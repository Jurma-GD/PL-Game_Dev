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
            case 0: return 600f; // Easy   — 10 min
            case 2: return 120f; // Hard   —  2 min
            default: return 300f; // Normal —  5 min
        }
    }
}
