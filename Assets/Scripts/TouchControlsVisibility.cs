using System.Runtime.InteropServices;
using UnityEngine;

public class TouchControlsVisibility : MonoBehaviour
{
    [SerializeField] private GameObject touchControls;
    [SerializeField] private bool forceShowForTesting;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int IsMobileBrowser();
#endif

    void Start()
    {
        bool shouldShowTouchControls;

#if UNITY_WEBGL && !UNITY_EDITOR
        shouldShowTouchControls = IsMobileBrowser() != 0;
#else
        shouldShowTouchControls = Application.isMobilePlatform;
#endif

        touchControls.SetActive(shouldShowTouchControls || forceShowForTesting);
    }
}
