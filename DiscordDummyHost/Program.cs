using DiscordDummyHost;
using System.Drawing;
using System.Windows.Forms;

namespace Discord_Dummy_Host;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }
}