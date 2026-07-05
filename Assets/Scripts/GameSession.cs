using UnityEngine;

public class GameSession
{
    public static int LevelsCompleted {get; private set; }

    public static void NextLevel()
    {
        LevelsCompleted++;
    }
    
    public static void Reset()
    {
        LevelsCompleted = 0;
    }
}
