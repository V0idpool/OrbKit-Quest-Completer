using System.Xml.Linq;

namespace DiscordDummyHost
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            infoLbl = new Label();
            SuspendLayout();
            // 
            // infoLbl
            // 
            infoLbl.AutoSize = false;
            infoLbl.Dock = DockStyle.Fill;
            infoLbl.Location = new Point(0, 0);
            infoLbl.Name = "infoLbl";
            infoLbl.Padding = new Padding(8, 0, 8, 0);
            infoLbl.Size = new Size(304, 80);
            infoLbl.TabIndex = 0;
            infoLbl.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(24, 24, 30);
            ClientSize = new Size(304, 80);
            Controls.Add(infoLbl);
            Font = new Font("Segoe UI", 9F);
            ForeColor = Color.FromArgb(230, 230, 235);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "OrbKit Stub";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label infoLbl;
    }
}
