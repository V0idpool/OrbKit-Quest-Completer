namespace DiscordDummyHost
{
    public partial class Form1 : Form
    {
        public static string exeName = Path.GetFileName(Environment.ProcessPath ?? "Game");
        public Form1()
        {
            InitializeComponent();
            infoLbl.Text = $"Quest Stub Running:\n{Form1.exeName}\n(Leave open until quest finishes)";
        }
    }
}
