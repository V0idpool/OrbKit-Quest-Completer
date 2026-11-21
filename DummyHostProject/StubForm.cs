namespace Stub_Host
{
    public partial class StubForm : Form
    {
        public StubForm() : this("Game Session") { }

        public StubForm(string gameName)
        {
            InitializeComponent();
            Text = gameName;
            infoLbl.Text = $"Running {gameName}...";
        }
    }
}
