using Stub_Host;
using System.Drawing;
using System.Windows.Forms;

namespace Stub_Host;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        string gameName = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
            ? args[0]
            : "Game Session";
        Application.Run(new StubForm(gameName));
    }
}