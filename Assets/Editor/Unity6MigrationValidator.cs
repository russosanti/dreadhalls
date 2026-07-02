using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class Unity6MigrationValidator
{
    private const string SettingsFolder = "Assets/Settings";
    private const string PipelinePath = SettingsFolder + "/DreadhallsURP.asset";
    private const string RendererPath = SettingsFolder + "/DreadhallsRenderer.asset";

    [MenuItem("Tools/Migrate Project to URP")]
    public static void MigrateToUrp()
    {
        EnsureSettingsFolder();
        var pipeline = EnsurePipelineAssets();
        GraphicsSettings.defaultRenderPipeline = pipeline;
        QualitySettings.renderPipeline = pipeline;

        var upgradedMaterials = 0;
        foreach (var materialPath in AssetDatabase.FindAssets("t:Material")
                     .Select(AssetDatabase.GUIDToAssetPath)
                     .Where(IsProjectAsset))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null || material.shader == null)
            {
                continue;
            }

            var shaderName = material.shader.name;
            MaterialUpgrader upgrader = null;
            if (shaderName == "Standard" || shaderName == "Standard (Specular setup)")
            {
                MarkMaskTexturesAsLinear(material);
                upgrader = new StandardUpgrader(shaderName);
            }
            else if (shaderName.Contains("Particles", StringComparison.OrdinalIgnoreCase))
            {
                upgrader = new ParticleUpgrader(shaderName);
            }

            if (upgrader == null)
            {
                continue;
            }

            MaterialUpgrader.Upgrade(material, upgrader, MaterialUpgrader.UpgradeFlags.None);
            EditorUtility.SetDirty(material);
            upgradedMaterials++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Run();

        Debug.Log(
            $"URP migration passed: assigned {pipeline.name} and upgraded " +
            $"{upgradedMaterials} materials.");
    }

    [MenuItem("Tools/Validate Unity 6 Migration")]
    public static void Run()
    {
        var failures = new List<string>();
        var assetPaths = AssetDatabase.GetAllAssetPaths()
            .Where(path => path.StartsWith("Assets/", StringComparison.Ordinal))
            .ToArray();

        AssetDatabase.ForceReserializeAssets(
            assetPaths,
            ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata);

        foreach (var scenePath in EditorBuildSettings.scenes
                     .Where(scene => scene.enabled)
                     .Select(scene => scene.path))
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            FindMissingScripts(scene.GetRootGameObjects(), scenePath, failures);
            EditorSceneManager.SaveScene(scene);
        }

        foreach (var prefabPath in AssetDatabase.FindAssets("t:Prefab")
                     .Select(AssetDatabase.GUIDToAssetPath)
                     .Where(IsProjectAsset))
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                FindMissingScripts(new[] { prefabRoot }, prefabPath, failures);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        ValidateUrp(failures);
        AssetDatabase.SaveAssets();

        if (failures.Count > 0)
        {
            throw new BuildFailedException(
                "Unity 6 migration validation failed:\n" + string.Join("\n", failures));
        }

        Debug.Log(
            $"Unity 6 migration validation passed: " +
            $"{EditorBuildSettings.scenes.Count(scene => scene.enabled)} scenes and " +
            $"{AssetDatabase.FindAssets("t:Prefab").Length} prefabs.");
    }

    private static UniversalRenderPipelineAsset EnsurePipelineAssets()
    {
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        if (pipeline != null)
        {
            return pipeline;
        }

        var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
        renderer.name = "DreadhallsRenderer";
        ResourceReloader.ReloadAllNullIn(renderer, UniversalRenderPipelineAsset.packagePath);
        AssetDatabase.CreateAsset(renderer, RendererPath);

        pipeline = UniversalRenderPipelineAsset.Create(renderer);
        pipeline.name = "DreadhallsURP";
        pipeline.supportsHDR = false;
        pipeline.supportsCameraDepthTexture = false;
        pipeline.supportsCameraOpaqueTexture = false;
        AssetDatabase.CreateAsset(pipeline, PipelinePath);
        return pipeline;
    }

    private static void EnsureSettingsFolder()
    {
        if (!AssetDatabase.IsValidFolder(SettingsFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Settings");
        }
    }

    private static void MarkMaskTexturesAsLinear(Material material)
    {
        foreach (var propertyName in new[] { "_MetallicGlossMap", "_SpecGlossMap", "_OcclusionMap" })
        {
            var texture = material.GetTexture(propertyName);
            var texturePath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(texturePath))
            {
                continue;
            }

            if (AssetImporter.GetAtPath(texturePath) is TextureImporter importer &&
                importer.sRGBTexture)
            {
                importer.sRGBTexture = false;
                importer.SaveAndReimport();
            }
        }
    }

    private static void ValidateUrp(ICollection<string> failures)
    {
        if (GraphicsSettings.defaultRenderPipeline is not UniversalRenderPipelineAsset)
        {
            failures.Add("Graphics Settings does not reference a URP asset.");
        }

        foreach (var materialPath in AssetDatabase.FindAssets("t:Material")
                     .Select(AssetDatabase.GUIDToAssetPath)
                     .Where(IsProjectAsset))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null || material.shader == null)
            {
                failures.Add($"{materialPath}: material has no shader.");
                continue;
            }

            var shaderName = material.shader.name;
            if (shaderName == "Standard" ||
                shaderName == "Standard (Specular setup)" ||
                shaderName.StartsWith("Legacy Shaders/", StringComparison.Ordinal))
            {
                failures.Add($"{materialPath}: still uses Built-in shader {shaderName}.");
            }
        }
    }

    private static bool IsProjectAsset(string path)
    {
        return path.StartsWith("Assets/", StringComparison.Ordinal);
    }

    private static void FindMissingScripts(
        IEnumerable<GameObject> roots,
        string assetPath,
        ICollection<string> failures)
    {
        foreach (var root in roots)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                var missingCount =
                    GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);

                if (missingCount > 0)
                {
                    failures.Add(
                        $"{assetPath}: {GetHierarchyPath(transform)} has " +
                        $"{missingCount} missing script(s).");
                }
            }
        }
    }

    private static string GetHierarchyPath(Transform transform)
    {
        var path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }
}
