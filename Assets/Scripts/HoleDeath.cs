using UnityEngine;
using UnityEngine.SceneManagement;

public class HoleDeath : MonoBehaviour
{
    [SerializeField] private float deathY = -3f;
    private bool gameOver = false;

    // Update is called once per frame
    void Update()
    {
        if (!gameOver && transform.position.y < deathY)
        {
            gameOver = true;
            SceneManager.LoadScene("GameOver");
        }
    }
}
