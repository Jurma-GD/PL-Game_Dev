/// <summary>
/// Persists settings between scenes (menu → game).
/// </summary>
public static class GameSettings
{
    public static float Volume = 1f;
    public static int MazeSeed = 12345;
    public static int Difficulty = 1; // 0 = Easy, 1 = Normal, 2 = Hard

    // Reshuffle intervals per difficulty (seconds)
    public static float GetReshuffleInterval()
    {
        switch (Difficulty)
        {
            case 0: return 420f; // Easy   —  7 min
            case 2: return 120f; // Hard   —  2 min
            default: return 210f; // Normal — 3:30
        }
    }
}
