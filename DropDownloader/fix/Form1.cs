using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace fix
{
    public partial class Form1 : Form
    {
        private static readonly string StartupKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private static readonly string StartupValue = "MyApplicationName";
        public Form1()
        {
            InitializeComponent();
           // SetStartup();
            Hide();
            Visible = false;
            Console.WriteLine("MEU NOME " + Process.GetCurrentProcess().ProcessName);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.Load += new EventHandler(Form1_Load);
        }

        void Form1_Load(object sender, EventArgs e)
        {
            this.Size = new Size(0, 0);
        }

        private static void SetStartup()
        {
            //Set the application to run at startup
            RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupKey, true);
            key.SetValue(StartupValue, Application.ExecutablePath.ToString());
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (Process.GetProcessesByName("Pro Arte Sync").Length > 0)
            {
                Console.WriteLine("Nao to aqui");
            }
            else
            {
                try
                {
                    Process.Start("Pro Arte Sync.exe");
                }
                catch
                {

                }
            }
        }
    }
}
