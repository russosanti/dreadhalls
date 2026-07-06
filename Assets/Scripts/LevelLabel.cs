using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class LevelLabel : MonoBehaviour
{
    // Levels completed label
    void Start()
    {
        GetComponent<TMP_Text>().text = $"Levels completed: {GameSession.LevelsCompleted}";
    }
}
