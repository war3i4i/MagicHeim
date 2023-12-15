namespace MagicHeim_Logger;

public static class Logger
{
    public static void Log(object obj, ConsoleColor color = ConsoleColor.DarkYellow)
    {
        if (Application.platform == RuntimePlatform.WindowsPlayer)
        {
            ConsoleManager.SetConsoleColor(ConsoleColor.Cyan);
            ConsoleManager.StandardOutStream.Write("[MagicHeim]");
            ConsoleManager.SetConsoleColor(color);
            ConsoleManager.StandardOutStream.Write($" {obj}\n");
            ConsoleManager.SetConsoleColor(ConsoleColor.White);
        }
        else
        {
            MonoBehaviour.print("[MagicHeim] " + obj);
        }
    }
}