using System.Windows.Forms;

namespace OSCAutoClicker;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        string? warning = Localization.Initialize();
        if (warning is not null)
        {
            MessageBox.Show(warning, "OSC Auto Clicker", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        Application.Run(new MainForm());
    }
}
