using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DropDownloader
{
    public partial class Userinfos : Form
    {
        f1 meform;
        int syncount;
        public Userinfos(f1 me)
        {
            InitializeComponent();
            if (File.Exists(Application.ExecutablePath + @"\foldericon.ico"))
            {
                Console.WriteLine("EXISTE");
            }
            meform = me;
            label1.Text = "Logado na conta: " + Properties.Settings.Default.User;
            textBox3.Text = Properties.Settings.Default.Folder;
            syncount = 0;
            
        }

        private void button2_Click(object sender, EventArgs e)
        {

            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                if (folderBrowserDialog1.SelectedPath.Length < 5)
                    MessageBox.Show("Local Inválido, por favor selecione outro", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                {
                    textBox3.Text = folderBrowserDialog1.SelectedPath;
                    string syncpath;
                    if (string.IsNullOrWhiteSpace(textBox3.Text) || !textBox3.Text.Contains("Conteudo Pro Arte"))
                        syncpath = textBox3.Text + @"\Conteudo Pro Arte";
                    else syncpath = textBox3.Text;
                    if (!Directory.Exists(syncpath)) Directory.CreateDirectory(syncpath);
                    Properties.Settings.Default.Folder = syncpath;
                    Properties.Settings.Default.Save();
                    Properties.Settings.Default.Reload();
                    textBox3.Text = syncpath;
                    WinApi.ApplyFolderIcon(syncpath, f1.iconpath);

                    if (!System.IO.File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Conteudo Pro Arte Sync.lnk"))
                    {
                        f1.CreateShortcut((string)Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Conteudo Pro Arte Sync.lnk", syncpath);
                    }
                    else
                    {
                        System.IO.File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Conteudo Pro Arte Sync.lnk");
                        f1.CreateShortcut((string)Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Conteudo Pro Arte Sync.lnk", syncpath);

                    }
                    meform.updatetickAsync(sender, e);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            f1.logado = false;
            meform.Visible = true;
            meform.WindowState = FormWindowState.Normal;
            Properties.Settings.Default.User = null;
            Properties.Settings.Default.Pass = null;
            Properties.Settings.Default.Folder = string.Empty;
            Properties.Settings.Default.DeviceID = null;
            Properties.Settings.Default.Save();
            Properties.Settings.Default.Reload();
            meform.notifyIcon1.Text = "Deslogado";
            meform.textBox1.Text = "";
            meform.textBox2.Text = "";
            meform.textBox3.Text = "";
            meform.automatic = false;
            this.Close();
        }

        private void Userinfos_Load(object sender, EventArgs e)
        {

        }

        private void Userinfos_Resize(object sender, EventArgs e)
        {

            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
            }
        }

        private void Userinfos_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            Process.Start(Properties.Settings.Default.Folder);

        }

        private void button4_Click(object sender, EventArgs e)
        {
            Process.Start(string.Format("http://crm.conteudoproarte.com.br?usuario={0}&chave={1}&aplicacao={2}", Properties.Settings.Default.User, Properties.Settings.Default.acesso, Properties.Settings.Default.DeviceID));
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label1.Text = "Logado na conta: " + Properties.Settings.Default.User;

            
            if (this.Visible && syncount == 0)
            {
                if (f1.filessync)
                {
                    statuslb.Text = "Todos arquivos Sincronizados";
                    statuslb.ForeColor = Color.Green;
                }
                else if (f1.isdownloading)

                {
                    statuslb.Text = "Baixando arquivos";
                    statuslb.ForeColor = Color.Red;
                }
                else
                {
                    statuslb.Text = "Procurando arquivos";
                    statuslb.ForeColor = Color.Blue;
                }
            }
        }

        private void Userinfos_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible)
            {
                this.Update();
                WinApi.ListDirectory(treeView1, Properties.Settings.Default.Folder);

            }
        }
        
    }

    public class SucessDownload
    {
        public string path;
        public DateTime tempo;
        public SucessDownload(string p, DateTime t)
        {
            path = p;
            tempo = t;
        }
    }
    

    }
        

