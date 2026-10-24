namespace VanguardLauncher;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (Bootstrap.TrySelfInstall(args))
            return;
        Application.Run(new MainForm(args));
    }
}
