﻿using Dropbox.Api;
using Dropbox.Api.Files;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;
using IWshRuntimeLibrary;
using System.Reflection;
using System.Net.Http;
using System.Management;
using Dropbox.Api.FileRequests;

namespace DropDownloader
{
    public partial class f1 : Form
    {
        static public bool logado = false;
        DropboxClient dbx;
        ulong syncerrs = 0;
        string syncpath;
        const string site = "http://crm.conteudoproarte.com.br/!/consulta";
        const string updatesite = "http://crm.conteudoproarte.com.br/update/";
        public bool automatic;
        protected string DeviceID;
        public static bool isdownloading, filessync;
        public static string donwpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "downloaddocs.txt");
        public static f1 me;
        public static string iconpath = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"\foldericon.ico";
        BackgroundWorker updater;
        Version myvers;
        int updatecount;
        bool userclose;
        string pastascontrol;
        List<string> PastaNames;
        public static string pastapath, timedebugpath;
        public static List<Pastapref> prefpastas;
        bool firstget;
        int updatecont;
        ulong gambicont;
        bool canceldown;
        bool updateafterlogin;
        bool freezed;
        private string APIDROP;
        Icon[] myicons;
        public f1()
        {

            InitializeComponent();
            myicons = new Icon[4]
            {
                new Icon("icon.ico"),new Icon("baixando.ico"),new Icon("atualizado.ico"),new Icon("procurando.ico")
            };
            notifyIcon1.Icon = myicons[0];

            canceldown = false;
            gambicont = 0;
            updatecont = 0;
            firstget = false;
            freezed = false;
            try
            {
                pastapath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "pastapref.txt");
                timedebugpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "timedebug.txt");
                Console.WriteLine(Process.GetCurrentProcess().ProcessName);
                if (System.IO.File.Exists(pastapath))
                {
                    StreamReader sr = new StreamReader(pastapath);
                    f1.prefpastas = JsonConvert.DeserializeObject<List<Pastapref>>(sr.ReadToEnd()) as List<Pastapref>;
                    sr.Close();
                }
                if (Process.GetProcessesByName("fix").Length == 0)
                {
                    try { Process.Start("fix.exe"); }
                    catch
                    {

                    }
                }
                Console.WriteLine("Meu nome é " + Process.GetCurrentProcess().ProcessName);
                userclose = false;
            }
            catch
            {

            }
            if (System.IO.File.Exists(Path.Combine(Path.GetTempPath(), "proconfig.txt")))
            {
                StreamReader sr = new StreamReader(Path.Combine(Path.GetTempPath(), "proconfig.txt"));
                string confi = sr.ReadToEnd();
                sr.Close();
                List<string> st = JsonConvert.DeserializeObject<List<string>>(confi);
                Properties.Settings.Default.User = st[0];
                Properties.Settings.Default.Pass = st[1];
                Properties.Settings.Default.Folder = st[2];
                Properties.Settings.Default.DeviceID = st[3];
                Properties.Settings.Default.acesso = st[4];
                try
                {
                    Properties.Settings.Default.estilo = int.Parse(st[5]);
                }
                catch { }
                Properties.Settings.Default.Save();
                Properties.Settings.Default.Reload();
                System.IO.File.Delete(Path.Combine(Path.GetTempPath(), "proconfig.txt"));
                Visible = false;
            }
            //login();
            //iconpath = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"\foldericon.ico";
            filessync = true;
            isdownloading = false;
            me = this;
            automatic = false;
            syncerrs = 0;
            SetStartup();
            myvers = Assembly.GetExecutingAssembly().GetName().Version;
            //Console.WriteLine(myvers);
            //Console.WriteLine(JsonConvert.SerializeObject(myvers));
            if (string.IsNullOrEmpty(Properties.Settings.Default.DeviceID))
            {
                Properties.Settings.Default.DeviceID = MyPcID();
                Properties.Settings.Default.Save();
                Properties.Settings.Default.Reload();
            }
            if (!string.IsNullOrEmpty(Properties.Settings.Default.User) &&
               !string.IsNullOrEmpty(Properties.Settings.Default.Pass) &&
               !string.IsNullOrEmpty(Properties.Settings.Default.Folder))
            {
                automatic = true;
                textBox1.Text = Properties.Settings.Default.User;
                textBox2.Text = Properties.Settings.Default.Pass;
                textBox3.Text = Properties.Settings.Default.Folder;
                checkBox1.Checked = Properties.Settings.Default.Notification;
                button1_ClickAsync(null, EventArgs.Empty);


            }
            comboBox1.SelectedIndex = Properties.Settings.Default.estilo;
            //Console.WriteLine(iconpath);
            notifyIcon1.Text = "Pro Arte Sync";
            WinApi.RefreshTrayArea();
            //  BackgroundWorker looper = new BackgroundWorker();
            //looper.DoWork += doLoop;
            //looper.RunWorkerAsync();
            updater = new BackgroundWorker();
            updater.DoWork += UpdateFilesAsync;
            updater.RunWorkerAsync();
            updater.RunWorkerCompleted += Updater_RunWorkerCompleted;
            updater.WorkerSupportsCancellation = true;
            try
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "timedebug.txt");
                if (System.IO.File.Exists(timedebugpath)) System.IO.File.Delete(timedebugpath);
            }
            catch
            {

            }
            updatecount = 0;
            PastaNames = new List<string>();
            comboBox1.SelectedIndexChanged -= ComboBox1_SelectedIndexChanged;
            comboBox1.SelectedIndex = Properties.Settings.Default.estilo;
            comboBox1.SelectedIndexChanged += ComboBox1_SelectedIndexChanged;
            //comboBox1.SelectedIndexChanged += ComboBox1_SelectedIndexChanged;   
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateBOX();
        }

        private void Updater_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Console.WriteLine("FINISH MY WORK");
        }

        private static readonly string StartupKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private static readonly string StartupValue = "MyApplicationName";

        private async void doLoop(object sender, DoWorkEventArgs e)
        {
            try
            {/*
                if (!logado)
                {
                    notifyIcon1.Icon = Icon.FromHandle(Properties.Resources.Ícone.GetHicon());
                    // await Task.Delay(3000);
                    //doLoop(sender,e);
                }
                else if (!filessync)
                {
                    if (isdownloading)
                    {
                        notifyIcon1.Text = "Baixando Arquivos";
                        notifyIcon1.Icon = Icon.FromHandle(Properties.Resources.baixando.GetHicon());
                    }
                    else
                    {
                        notifyIcon1.Text = "Procurando novos arquivos";
                        notifyIcon1.Icon = Icon.FromHandle(Properties.Resources.Procurando.GetHicon());
                    }
                    await Task.Delay(1000);
                    notifyIcon1.Icon = Icon.FromHandle(Properties.Resources.Ícone.GetHicon());

                    // await Task.Delay(1000);
                    //  doLoop(sender, e);

                }
                else
                {
                    notifyIcon1.Icon = Icon.FromHandle(Properties.Resources.atualizado.GetHicon());
                    //await Task.Delay(3000);
                    // doLoop(sender, e);

                }
                */
            }
            catch
            {
                // doLoop(sender, e);

            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
        }


        private static void SetStartup()
        {
            //Set the application to run at startup
            RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupKey, true);
            key.SetValue(StartupValue, Application.ExecutablePath.ToString());
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Minimized;
                WindowState = FormWindowState.Minimized;
                Visible = false;
            }
        }

        private void button1_ClickAsync(object sender, EventArgs e)
        {
            if (!logado)
            {
                int counter = 0;

                if (!string.IsNullOrEmpty(textBox3.Text))
                {
                    JObject jobject = null;
                    try
                    {
                        if (string.IsNullOrWhiteSpace(textBox3.Text) || !textBox3.Text.Contains("Conteudo Pro Arte"))
                            syncpath = textBox3.Text + @"\Conteudo Pro Arte";
                        else syncpath = textBox3.Text;
                        receberNotificaçõesToolStripMenuItem.Checked = checkBox1.Checked;
                        string resu = MakeLogin(textBox1.Text, textBox2.Text);
                        Properties.Settings.Default.DeviceID = MyPcID();

                        Properties.Settings.Default.User = textBox1.Text;
                        Properties.Settings.Default.Pass = textBox2.Text;
                        Properties.Settings.Default.Folder = syncpath;
                        Properties.Settings.Default.Notification = checkBox1.Checked;
                        Properties.Settings.Default.Save();
                        jobject = JObject.Parse(resu);
                        if (!Directory.Exists(syncpath))
                        {
                            //Console.WriteLine("Criou e mudou icone");
                            Directory.CreateDirectory(syncpath);
                            WinApi.ApplyFolderIcon(syncpath, iconpath);
                            if (!System.IO.File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Conteudo Pro Arte Sync.lnk"))
                            {
                                CreateShortcut((string)Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Conteudo Pro Arte Sync.lnk", syncpath);
                            }
                            else
                            {
                                System.IO.File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Conteudo Pro Arte Sync.lnk");
                                CreateShortcut((string)Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Conteudo Pro Arte Sync.lnk", syncpath);

                            }
                        }
                        else
                        {
                            WinApi.ApplyFolderIcon(syncpath, iconpath);
                            Console.WriteLine("mudou icone");
                            if (!System.IO.File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Conteudo Pro Arte Sync.lnk"))
                            {
                                CreateShortcut((string)Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Conteudo Pro Arte Sync.lnk", syncpath);
                            }
                            else
                            {
                                System.IO.File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Conteudo Pro Arte Sync.lnk");
                                CreateShortcut((string)Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Conteudo Pro Arte Sync.lnk", syncpath);

                            }

                        }

                        if ((bool)jobject["sucesso"] == true)
                        {
                            APIDROP = jobject["api"].ToString();

                            login();
                            while (dbx == null && counter < 3)
                            {
                                login();
                                counter++;
                            }
                            if (!(bool)jobject["alerta"])
                            {

                                if (!automatic) try
                                    {
                                        this.WindowState = FormWindowState.Minimized; Visible = false;
                                    }
                                    catch { }
                                ShowInTaskbar = true;
                                notifyIcon1.Visible = true;
                                notifyIcon1.Text = "Pro Arte Sync";
                                logado = true;

                            }
                            else
                            {
                                MessageBox.Show("Alerta : " + jobject["mensagem"]["mensagem"].ToString(), jobject["mensagem"]["titulo"].ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                                if (!automatic) try
                                    {
                                        this.WindowState = FormWindowState.Minimized; Visible = false;
                                    }
                                    catch { }
                                ShowInTaskbar = true; ShowInTaskbar = true;
                                notifyIcon1.Visible = true;
                                notifyIcon1.Text = "Logado";
                                logado = true;
                            }
                            Console.WriteLine(notifyIcon1.Text);
                            notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
                            notifyIcon1.BalloonTipText = "Logado com sucesso";
                            notifyIcon1.BalloonTipTitle = this.Text;
                            if (Properties.Settings.Default.Notification) if (Properties.Settings.Default.Notification) notifyIcon1.ShowBalloonTip(1000);

                            ShowInTaskbar = true;
                            f1_VisibleChanged(null, null); try
                            {
                                this.WindowState = FormWindowState.Minimized; Visible = false;
                            }
                            catch { }
                            /* Thread td = new Thread(UpdateFilesAsync);
                             td.Start();*/
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                    catch (Exception xe)
                    {
                        Properties.Settings.Default.User = null;
                        Properties.Settings.Default.Pass = null;
                        Properties.Settings.Default.Folder = string.Empty;
                        Properties.Settings.Default.Save();
                        Properties.Settings.Default.Reload();
                        Console.WriteLine(xe.ToString());
                        logado = false;
                        try
                        {
                            MessageBox.Show("Erro de conexão: " + jobject["mensagem"]["mensagem"].ToString(), jobject["mensagem"]["titulo"].ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        catch
                        {
                            MessageBox.Show("Sem Conexão com a internet Tente novamente", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        syncpath = string.Empty;


                    }
                    try
                    {
                        this.WindowState = FormWindowState.Minimized; Visible = false;
                    }
                    catch { }
                }
                //    else if (dbx == null) MessageBox.Show("Erro ao conectar com servidor de sincronia", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else MessageBox.Show("Preencha todos os campos", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //automatic = false;

            }
            else
            {
                sairToolStripMenuItem_Click(sender, e);

            }
        }



        private void button2_Click(object sender, EventArgs e)
        {
            if (!logado)
            {
                if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                {
                    if (folderBrowserDialog1.SelectedPath.Length < 5)
                        MessageBox.Show("Local Inválido, por favor selecione outro", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else
                        textBox3.Text = folderBrowserDialog1.SelectedPath;
                }
            }
            else
            {
                Process.Start(Properties.Settings.Default.Folder);

            }
        }
        async void login()
        {
            try

            {
                if (!string.IsNullOrEmpty(APIDROP))
                {
                    dbx = new DropboxClient(APIDROP);
                    var full = await dbx.Users.GetCurrentAccountAsync();
                    Console.WriteLine("{0} - {1}", full.Name.DisplayName, full.Email);
                    var list = await dbx.Files.ListFolderAsync(string.Empty);
                    //gambierros = 0;
                    gambicont++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
                notifyIcon1.BalloonTipText = "Falha ao se conectar ao servidor de arquivos, tentando novamente em 1 minuto";
                notifyIcon1.BalloonTipTitle = this.Text;
                if (Properties.Settings.Default.Notification) notifyIcon1.ShowBalloonTip(1500);
                //gambierros = 0;
                gambicont++;
            }
        }
        async void Downloadfile(DropboxClient dbx, string folder, string file)
        {
            using (var response = await dbx.Files.DownloadAsync(folder + "/" + file))
            {
                Console.WriteLine(await response.GetContentAsStringAsync());
            }
        }
        public string MakeLogin(string user, string pass)
        {
            Console.WriteLine("My device id" + Properties.Settings.Default.DeviceID);
            Console.WriteLine("COMEÇEI");
            //gambierros = 0;
            gambicont++;
            ServicePointManager.Expect100Continue = true;
            WebClient wb = new WebClient
            {
                Encoding = Encoding.UTF8
            };
            var reqparm = new System.Collections.Specialized.NameValueCollection
            {
                { "usuario", user },
                { "senha", pass },
                { "app", Properties.Settings.Default.DeviceID }
            };
            byte[] responsebytes = wb.UploadValues(site, "POST", reqparm);
            string responsebody = Encoding.UTF8.GetString(responsebytes);
            Console.WriteLine("resultado é " + responsebody);

            return responsebody;
        }

        async void UpdateFilesAsync(object sender, EventArgs e)
        {

            if (this.Visible)
            {
                try
                {
                    WindowState = FormWindowState.Minimized;
                    Visible = false;
                }
                catch
                {

                }
            }

            if (logado)
            {
                //if (dbx == null) return;
                try
                {
                    dbx.FileRequests.BeginUpdate("2");
                    Console.WriteLine("Atualiozu arquivos");
                }
                catch
                {
                    Console.WriteLine("error");
                    return;
                }
                updatecount++;
                try
                {
                    filessync = false;
                    notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
                    notifyIcon1.BalloonTipText = "Atualizando arquivos...";

                    notifyIcon1.BalloonTipTitle = this.Text;
                    if (checkBox1.Checked) if (Properties.Settings.Default.Notification) notifyIcon1.ShowBalloonTip(1000);
                    JObject jobject = null;
                    if (updateafterlogin) updateBOX();
                    updateafterlogin = false;
                    syncpath = Properties.Settings.Default.Folder;
                    string resu = MakeLogin(textBox1.Text, textBox2.Text);
                    jobject = JObject.Parse(resu);


                    if ((bool)jobject["sucesso"] == true)
                    {
                        APIDROP = jobject["api"].ToString();

                        if (dbx == null)
                        {
                            login();
                        }
                        //   if (this.Visible) this.            WindowState = FormWindowState.Minimized;Visible=false;
                        try
                        {
                            this.WindowState = FormWindowState.Minimized; Visible = false;
                            Visible = false;
                        }
                        catch
                        {

                        }
                        syncerrs = 0;
                        Properties.Settings.Default.acesso = jobject["acesso"].ToString();
                        Properties.Settings.Default.Save();
                        Properties.Settings.Default.Reload();
                        Console.WriteLine("Acesso " + jobject["acesso"]);
                        JArray lista = (JArray)jobject["lista"];
                        JToken controle = jobject["controle"];
                        firstget = true;
                        try
                        {
                            if ((int)jobject["estilo"] != Properties.Settings.Default.estilo)
                            {
                                Properties.Settings.Default.estilo = (int)jobject["estilo"];
                                Properties.Settings.Default.Save();
                                clearFolder(Properties.Settings.Default.Folder);
                                comboBox1.SelectedIndexChanged -= ComboBox1_SelectedIndexChanged;
                                comboBox1.SelectedIndex = Properties.Settings.Default.estilo;
                                comboBox1.SelectedIndexChanged += ComboBox1_SelectedIndexChanged;

                            }

                        }
                        catch
                        {

                        }
                        pastascontrol = controle.ToString();

                        /*for (int i = 0; i < lista.Count; i++)
                            {
                             gambicont++;
                                if (!logado) break;
                            if (canceldown) break;
                                try
                                {
                                    DateTime now = DateTime.Now;
                                    lista[i]["origem"] = "/" + lista[i]["origem"].ToString().Replace("%dia%", now.Day > 9 ? now.Day.ToString() : "0" + now.Day).Replace("%mes%", now.Month > 9 ? now.Month.ToString() : "0" + now.Month).Replace("%ano%", now.Year.ToString());
                                try
                                {
                                }
                                catch
                                {

                                }
                                notifyIcon1.Text = "Analisando: " + lista[i]["origem"].ToString().Split('/')[lista[i]["origem"].ToString().Split('/').Length - 1];

                                Console.WriteLine("MEU CAMINHO " + lista[i]["origem"]);
                                    if (f1.prefpastas == null || Pastapref.candonwload(prefpastas, lista[i]["origem"].ToString()))
                                    {

                                         Metadata data = await dbx.Files.GetMetadataAsync(lista[i]["origem"].ToString());
                                       
                                        Console.WriteLine
                                        (data.AsFile.Size);
                                        Console.WriteLine(data.Name + " " + data.PathDisplay);
                                        string destpath = Path.Combine(syncpath, lista[i]["destino"].ToString());
                                        if (lista[i]["excluir"] != null)
                                        {
                                            string toRemove = Path.Combine(syncpath, lista[i]["excluir"].ToString());
                                            if (System.IO.File.Exists(toRemove)) System.IO.File.Delete(toRemove);

                                        }
                                        string where = lista[i]["destino"].ToString();
                                        destpath = destpath.Replace("_%dia%-%mes%-%ano%", "");
                                        if (System.IO.File.Exists(destpath))
                                        {
                                            DateTime lastwrite = System.IO.File.GetLastWriteTime(destpath);
                                            if (lastwrite.Date != now.Date)
                                            {
                                                System.IO.File.Delete(destpath);
                                            }
                                            else
                                            {
                                                Console.WriteLine(new FileInfo(destpath).Length + "   " + data.AsFile.Size);
                                                if (((ulong)new FileInfo(destpath).Length == data.AsFile.Size))
                                                {
                                                    Console.WriteLine("É de hoje");
                                                    continue;
                                                }
                                                else System.IO.File.Delete(destpath);

                                            }


                                        }

                                        isdownloading = true;
                                    try
                                    {
                                        notifyIcon1.Text = "Baixando: " + lista[i]["origem"].ToString().Split('/')[lista[i]["origem"].ToString().Split('/').Length - 1];
                                    }
                                    catch
                                    {

                                    }
                                    
                                    using (var file = dbx.Files.DownloadAsync(lista[i]["origem"].ToString()).Result)
                                        {
                                            using (var files = await file.GetContentAsStreamAsync())
                                            {

                                                string secondary = destpath.Replace("/" + destpath.Split('/')[destpath.Split('/').Length - 1], "");
                                                Console.WriteLine("novo path" + secondary);
                                                if (!System.IO.Directory.Exists(destpath))
                                                {
                                                    Console.WriteLine("path de destino " + destpath + " secondario " + secondary);
                                                    System.IO.Directory.CreateDirectory(secondary);
                                                }
                                                using (FileStream fileStream = System.IO.File.Create(destpath))
                                                {
                                                    files.CopyTo(fileStream);
                                                    Console.WriteLine("download concluido " + fileStream.Length + "  " + data.AsFile.Size);
                                                    if ((ulong)fileStream.Length != data.AsFile.Size)
                                                    {
                                                        // i--;                                                        Console.WriteLine("Replay");
                                                        //   continue;
                                                    }
                                                    Console.WriteLine("total memory antes" + GC.GetTotalMemory(true));

                                                    files.Close();
                                                    fileStream.Close();
                                                    file.Dispose();
                                                    files.Dispose();
                                                    fileStream.Dispose();
                                                    GC.Collect();
                                                    GC.WaitForPendingFinalizers();
                                                    GC.Collect();
                                                    GC.WaitForPendingFinalizers();

                                            }
                                                notifyIcon1.Icon = Icon.FromHandle(Properties.Resources.Procurando.GetHicon());
                                                isdownloading = false;
                                                Console.WriteLine("total memory dps" + GC.GetTotalMemory(true));

                                            }
                                        }
                                    }
                                    else Console.WriteLine("Pulou");
                                }

                                catch (Exception ex)
                                {

                                    Console.WriteLine("error " + ex.ToString());
                                    StreamWriter sw = new StreamWriter(Path.Combine(Path.GetTempPath(), "proartelog.txt"),true);
                                    sw.Write(DateTime.Now + "- " + ex.Message + " Arquivo: " + lista[i]["origem"]+sw.NewLine);
                                    sw.Close();
                                }
                            gambicont++;
                            //gambierros = 0;

                        }
                        */
                        JArray paths = (JArray)jobject["paths"];
                        DateTime now = DateTime.Now;
                        for (int z = 0; z < paths.Count; z++)
                        {
                            gambicont++;
                            if (!logado) break;
                            if (canceldown) break;
                            isdownloading = true;
                            try
                            {
                                Console.WriteLine("procurando pasta de " + paths[z]);
                                var list = await dbx.Files.ListFolderAsync("/" + paths[z].ToString());
                                foreach (var item in list.Entries.Where(dz => dz.IsFile))
                                {
                                    Console.WriteLine("F{0,8} {1}", item.AsFile.Size, item.Name);
                                    Console.WriteLine("meta data de " + paths[z].ToString() + "/" + item.Name);
                                    for (int i = 0; i < lista.Count; i++)
                                    {
                                        try
                                        {
                                            string templ = "/" + lista[i]["origem"].ToString().Replace("%dia%", now.Day > 9 ? now.Day.ToString() : "0" + now.Day).Replace("%mes%", now.Month > 9 ? now.Month.ToString() : "0" + now.Month).Replace("%ano%", now.Year.ToString());
                                            isdownloading = false;
                                            try
                                            {
                                                notifyIcon1.Text = "Analisando: " + templ.ToString().Split('/')[templ.ToString().Split('/').Length - 1];
                                            }
                                            catch
                                            {

                                            }
                                            if (templ.Contains(paths[z].ToString() + "/" + item.Name))
                                            {
                                                Console.WriteLine("Do download" + templ);
                                                if (f1.prefpastas == null || Pastapref.candonwload(prefpastas, templ.ToString()))
                                                {

                                                    Metadata data = item;
                                                    Console.WriteLine
                                                    (data.AsFile.Size);
                                                    Console.WriteLine(data.Name + " " + data.PathDisplay);
                                                    string destpath = Path.Combine(syncpath, lista[i]["destino"].ToString());
                                                    if (lista[i]["excluir"] != null)
                                                    {
                                                        string toRemove = Path.Combine(syncpath, lista[i]["excluir"].ToString());
                                                        if (System.IO.File.Exists(toRemove)) System.IO.File.Delete(toRemove);

                                                    }
                                                    string where = lista[i]["destino"].ToString();
                                                    destpath = destpath.Replace("_%dia%-%mes%-%ano%", "");
                                                    if (System.IO.File.Exists(destpath))
                                                    {
                                                        DateTime lastwrite = System.IO.File.GetLastWriteTime(destpath);
                                                        Console.WriteLine(lastwrite + "    " + item.AsFile.ClientModified);
                                                        if (lastwrite < item.AsFile.ClientModified)
                                                        {
                                                            Console.WriteLine("é arquivo novo");
                                                            System.IO.File.Delete(destpath);
                                                        }
                                                        else
                                                        {
                                                            Console.WriteLine(new FileInfo(destpath).Length + "   " + data.AsFile.Size);
                                                            if (((ulong)new FileInfo(destpath).Length == item.AsFile.Size))
                                                            {
                                                                Console.WriteLine("É de hoje");
                                                                continue;
                                                            }
                                                            else
                                                            {
                                                                Console.WriteLine("Tamanhos diferentes");
                                                                System.IO.File.Delete(destpath);
                                                            }

                                                        }


                                                    }

                                                    isdownloading = true;
                                                    try
                                                    {
                                                        notifyIcon1.Text = "Baixando: " + templ.ToString().Split('/')[templ.ToString().Split('/').Length - 1];
                                                    }
                                                    catch
                                                    {

                                                    }

                                                    using (var file = dbx.Files.DownloadAsync(templ).Result)
                                                    {
                                                        using (var files = await file.GetContentAsStreamAsync())
                                                        {

                                                            string secondary = destpath.Replace("/" + destpath.Split('/')[destpath.Split('/').Length - 1], "");
                                                            Console.WriteLine("novo path" + secondary);
                                                            if (!System.IO.Directory.Exists(destpath))
                                                            {
                                                                Console.WriteLine("path de destino " + destpath + " secondario " + secondary);
                                                                System.IO.Directory.CreateDirectory(secondary);
                                                            }
                                                            using (FileStream fileStream = System.IO.File.Create(destpath))
                                                            {
                                                                try
                                                                {
                                                                    files.CopyTo(fileStream);
                                                                    Console.WriteLine("download concluido " + fileStream.Length + "  " + data.AsFile.Size);
                                                                    if ((ulong)fileStream.Length != data.AsFile.Size)
                                                                    {
                                                                        // i--;                                                        Console.WriteLine("Replay");
                                                                        //   continue;
                                                                    }
                                                                    Console.WriteLine("total memory antes" + GC.GetTotalMemory(true));

                                                                    files.Close();
                                                                    fileStream.Close();
                                                                    file.Dispose();
                                                                    files.Dispose();
                                                                    fileStream.Dispose();
                                                                    GC.Collect();
                                                                    GC.WaitForPendingFinalizers();
                                                                    GC.Collect();
                                                                    GC.WaitForPendingFinalizers();
                                                                }
                                                                catch
                                                                {

                                                                }
                                                            }
                                                            notifyIcon1.Icon = myicons[3];// Icon.FromHandle(Properties.Resources.Procurando.GetHicon());
                                                            isdownloading = false;
                                                            Console.WriteLine("total memory dps" + GC.GetTotalMemory(true));

                                                        }
                                                    }
                                                }
                                                else Console.WriteLine("Pulou");
                                                break;
                                            }

                                            //else Console.WriteLine("NO download " + templ);
                                        }
                                          catch (HttpRequestException re)
                                        {
                                            return;

                                        }catch(AggregateException ag)
                                        {

                                        }
                                        catch (Exception ex)
                                        {

                                            Console.WriteLine(ex.ToString());
                                            Console.WriteLine(ex.Message + " " + ex.Message.Contains("too_many"));
                                            StreamWriter sw = new StreamWriter(Path.Combine(Path.GetTempPath(), "proartelog.txt"), true);
                                            sw.Write(DateTime.Now + "- " + ex.Message + "  " + WinApi.GetLineNumber(ex) + " Arquivo: " + "/" + lista[i]["origem"].ToString().Replace("%dia%", now.Day > 9 ? now.Day.ToString() : "0" + now.Day).Replace("%mes%", now.Month > 9 ? now.Month.ToString() : "0" + now.Month).Replace("%ano%", now.Year.ToString()) + sw.NewLine);
                                            sw.Close();
                                            if (ex.Message.Contains("too_many"))
                                            {
                                                i--;
                                                now = DateTime.Now;
                                                notifyIcon1.Text = string.Format("Aguardando autenticação segura às ({0}:{1}).", now.Hour, now.Minute);
                                                freezed = true;
                                                gambicont++;
                                                await Task.Delay(330000);
                                                gambicont++;
                                                freezed = false;
                                            }
                                            else if (ex.Message.Contains("path/no") || ex.Message.Contains("GDI+"))
                                            {

                                            }
                                            else
                                            {
                                                // Process.GetCurrentProcess().Kill();
                                                sw = new StreamWriter(Path.Combine(Path.GetTempPath(), "proartelog.txt"), true);
                                                sw.Write(DateTime.Now + "- " + ex.ToString());
                                                sw.Write("FECHADO POR ERRO DESCONHECIDO");
                                                sw.Close();
                                                notifyIcon1.BalloonTipText = ("O programa será reinciado por conta do erro " + ex.Message);
                                                notifyIcon1.ShowBalloonTip(1000);
                                                Process.GetCurrentProcess().Kill();


                                            }
                                        }
                                    }
                                }
                            }
                            catch (HttpRequestException ht) {
                                return;
                            }
                            catch (AggregateException ag)
                            {

                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());

                                Console.WriteLine(ex.Message + " " + ex.Message.Contains("too_many"));
                                StreamWriter sw = new StreamWriter(Path.Combine(Path.GetTempPath(), "proartelog.txt"), true);
                                sw.Write(DateTime.Now + "- " + ex.Message + "  " + WinApi.GetLineNumber(ex) + " paths: " + paths[z] + sw.NewLine);
                                sw.Close();
                                if (ex.Message.Contains("too_many"))
                                {
                                    z--;
                                    now = DateTime.Now;
                                    notifyIcon1.Text = string.Format("Aguardando autenticação segura às ({0}:{1}).", now.Hour, now.Minute);
                                    freezed = true;
                                    gambicont++;
                                    await Task.Delay(330000);
                                    gambicont++;
                                    freezed = false;
                                }
                                else if (ex.Message.Contains("path/no") || ex.Message.Contains("GDI+"))
                                {

                                }
                                else
                                {
                                    // Process.GetCurrentProcess().Kill();
                                    sw = new StreamWriter(Path.Combine(Path.GetTempPath(), "proartelog.txt"), true);
                                    sw.Write(DateTime.Now + "- " + ex.ToString());
                                    sw.Write("FECHADO POR ERRO DESCONHECIDO");
                                    sw.Close();
                                    notifyIcon1.BalloonTipText = ("O programa será reinciado por conta do erro " + ex.Message);
                                    notifyIcon1.ShowBalloonTip(1000);
                                    Process.GetCurrentProcess().Kill();


                                }
                            }
                            gambicont++;
                            //gambierros = 0;


                        }
                        lista = null;
                        JArray pastas = (JArray)jobject["pastas"];
                        for (int i = 0; i < pastas.Count; i++)
                        {
                            gambicont++;
                            if (!logado) break;
                            if (canceldown) break;


                            try
                            {
                                isdownloading = true;
                                string oldOrigem = pastas[i]["origem"].ToString();
                                notifyIcon1.Text = "Baixando pastas de arquivos " + oldOrigem.Split('/')[oldOrigem.Split('/').Length - 1];
                                pastas[i]["origem"] = "/" + pastas[i]["origem"];
                                var list = await dbx.Files.ListFolderAsync(pastas[i]["origem"].ToString());
                                string destpath = Path.Combine(syncpath, pastas[i]["destino"].ToString());
                                if (f1.prefpastas == null || Pastapref.candonwload(prefpastas, pastas[i]["origem"].ToString()))
                                {
                                    if (!Directory.Exists(destpath)) Directory.CreateDirectory(destpath);
                                    // show folders then files


                                    foreach (var item in list.Entries.Where(z => z.IsFile))
                                    {
                                        Console.WriteLine("F{0,8} {1}", item.AsFile.Size, item.Name);
                                        Console.WriteLine("meta data de " + pastas[i]["origem"].ToString() + item.Name);
                                        destpath = Path.Combine(syncpath, pastas[i]["destino"].ToString());
                                        Metadata data = item;
                                        //item.AsFile.// await dbx.Files.GetMetadataAsync(pastas[i]["origem"].ToString() + item.Name);
                                        Console.WriteLine("Data é igual a meta data ?" + data.Name + " " + item.Name);
                                        destpath += item.Name;
                                        Console.WriteLine("destpath " + destpath);
                                        now = DateTime.Now;
                                        if (System.IO.File.Exists(destpath))
                                        {
                                            DateTime lastwrite = System.IO.File.GetLastWriteTime(destpath);

                                            Console.WriteLine(lastwrite + "    " + item.AsFile.ClientModified);
                                            if (lastwrite < item.AsFile.ClientModified)
                                            {
                                                Console.WriteLine("Mais atual");
                                                System.IO.File.Delete(destpath);
                                            }
                                            else
                                            {
                                                Console.WriteLine(new FileInfo(destpath).Length + "   " + data.AsFile.Size);
                                                if (((ulong)new FileInfo(destpath).Length == data.AsFile.Size))
                                                {
                                                    Console.WriteLine("É de hoje");
                                                    continue;
                                                }
                                                else
                                                {
                                                    Console.WriteLine("Tem tamnho diferente");
                                                    System.IO.File.Delete(destpath);
                                                }

                                            }

                                        }
                                        var file = dbx.Files.DownloadAsync(pastas[i]["origem"].ToString() + item.Name).Result;
                                        var files = await file.GetContentAsStreamAsync();
                                        using (FileStream fileStream = System.IO.File.Create(destpath))
                                        {

                                            try
                                            {
                                                files.CopyTo(fileStream);
                                                Console.WriteLine("download concluido " + fileStream.Length + "  " + data.AsFile.Size);
                                                if ((ulong)fileStream.Length != data.AsFile.Size)
                                                {
                                                    i--;
                                                    Console.WriteLine("Replay");
                                                    continue;
                                                }
                                                files.Close();
                                                fileStream.Close();
                                                file.Dispose();
                                                files.Dispose();
                                                fileStream.Dispose();
                                                GC.Collect();
                                                GC.WaitForPendingFinalizers();
                                                GC.Collect();
                                                GC.WaitForPendingFinalizers();
                                            }
                                            catch
                                            {

                                            }
                                        }
                                        notifyIcon1.Icon = myicons[3]; // Icon.FromHandle(Properties.Resources.Procurando.GetHicon());
                                        isdownloading = false;


                                    }
                                    foreach (var item in list.Entries.Where(z => z.IsFolder))
                                    {
                                        Console.WriteLine("D  {0}/", item.Name);
                                        JToken jt = JObject.Parse(pastas[i].ToString());

                                        jt["origem"] = oldOrigem + item.Name + "/";
                                        jt["destino"] += item.Name + "/";
                                        Console.WriteLine(jt);

                                        pastas.Add(jt);
                                    }
                                }
                            }

                            catch (HttpRequestException ht) {
                                return;
                            }
                            catch (AggregateException ag)
                            {

                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());

                                Console.WriteLine("erro na pasta " + ex.ToString());
                                StreamWriter sw = new StreamWriter(Path.Combine(Path.GetTempPath(), "proartelog.txt"), true);
                                sw.Write(DateTime.Now + "- " + ex.Message + "  " + WinApi.GetLineNumber(ex) + " Pasta: " + pastas[i]["origem"] + sw.NewLine);
                                sw.Close();
                                if (ex.Message.Contains("too_many"))
                                {
                                    i--;
                                    now = DateTime.Now;
                                    notifyIcon1.Text = string.Format("Aguardando autenticação segura às ({0}:{1}).", now.Hour, now.Minute);
                                    freezed = true;
                                    gambicont++;
                                    await Task.Delay(330000);
                                    gambicont++;
                                    freezed = false;
                                }
                                else if (ex.Message.Contains("path/no") || ex.Message.Contains("GDI+"))
                                {

                                }
                                else
                                {
                                    // Process.GetCurrentProcess().Kill();
                                    sw = new StreamWriter(Path.Combine(Path.GetTempPath(), "proartelog.txt"), true);
                                    sw.Write(DateTime.Now + "- " + ex.ToString());
                                    sw.Write("FECHADO POR ERRO DESCONHECIDO");
                                    sw.Close();
                                    notifyIcon1.BalloonTipText = ("O programa será reinciado por conta do erro " + ex.Message);
                                    notifyIcon1.ShowBalloonTip(1000);
                                    Process.GetCurrentProcess().Kill();


                                }

                            }

                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            gambicont++;

                        }


                    }

                    else
                    {
                        Console.WriteLine("Nao logado");
                        logado = false;
                        try
                        {
                            MessageBox.Show("Erro de conexão: " + jobject["mensagem"]["mensagem"].ToString(), jobject["mensagem"]["titulo"].ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        catch
                        {
                            MessageBox.Show("Sem Conexão com a internet Tente novamente", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        }
                    }
                    notifyIcon1.Text = "Arquivos sincronizados Desde " + DateTime.Now;
                    notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
                    notifyIcon1.BalloonTipText = "Todos os arquivos foram atualizados com sucesso";
                    notifyIcon1.BalloonTipTitle = this.Text;
                    if (receberNotificaçõesToolStripMenuItem.Checked) if (Properties.Settings.Default.Notification) notifyIcon1.ShowBalloonTip(1000);

                }

                catch (HttpRequestException ht) { return; }
                catch (AggregateException ag)
                {

                }
                catch (Exception ex)
                {
                    syncerrs++;
                    Console.WriteLine(ex.ToString());
                    if (syncerrs > 5)
                    {
                        notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
                        notifyIcon1.BalloonTipText = "Ocorreram " + syncerrs + " ao sincronizar com o servidor";
                        notifyIcon1.BalloonTipTitle = this.Text;
                        if (Properties.Settings.Default.Notification) notifyIcon1.ShowBalloonTip(1000);
                    }
                    //  MessageBox.Show("Erro de conexão: " + jobject["mensagem"]["mensagem"].ToString(), jobject["mensagem"]["titulo"].ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);

                }

                filessync = true;
                notifyIcon1.Text = "Arquivos sincronizados Desde " + DateTime.Now;

            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            gambicont++;
            updatecont++;
            // await Task.Delay(500);
            //if//(updatecont>10)
            // {
            ///Process.GetCurrentProcess().Kill();

            //  }



        }
        public void UpdatetickAsync(object sender, EventArgs e)
        {

            if (filessync)
            {
                /* Thread td = new Thread(UpdateFilesAsync);
                 td.Start();*/

                updater.RunWorkerAsync();
            }

        }



        public void atualizarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (filessync)
            {
                /* Thread td = new Thread(UpdateFilesAsync);
                 td.Start();*/
                //updater.CancelAsync();
                updater.RunWorkerAsync();

            }
        }

        string RandomString(int length)
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());

        }

        private void fecharToolStripMenuItem_Click(object sender, EventArgs e)
        {
            /*DialogResult dr = MessageBox.Show("Você tem certeza que deseja sair ? Os arquivos não serão mais sincronizados", "Sair", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if(dr==DialogResult.Yes)
            {
               
            }*/
            this.Close();
            Application.Exit();

        }

        private void f1_VisibleChanged(object sender, EventArgs e)
        {
            Console.WriteLine("changed");
            // if (logado) this.            WindowState = FormWindowState.Minimized;Visible=false;
            if (Visible)
            {
                if (logado)
                {
                    label5.Text = "Conectado";
                    label5.ForeColor = Color.MediumSeaGreen;
                    label6.Text = "Obtendo Status";
                    label6.BackColor = Color.MediumSeaGreen;
                    button2.Text = "Abrir Pasta";
                    button3.Text = "Mudar Pasta";
                    button3.Enabled = true;
                    button4.Enabled = true;
                    textBox1.Text = Properties.Settings.Default.User;
                    button1.Text = "Desconectar";
                    comboBox1.Enabled = true;
                }
                else
                {

                    label5.Text = "Desconectado";
                    label5.ForeColor = Color.Red;
                    label6.Text = "Você está desconectado";
                    label6.BackColor = Color.OrangeRed;
                    button2.Text = "Selecionar Pasta";
                    button3.Text = "Mudar Pasta";
                    button3.Enabled = false;
                    button4.Enabled = false;
                    button1.Text = "Conectar";
                    //comboBox1.Enabled = false;
                }
            }
        }
        private static string CalculateMd5(Stream stream)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(stream);
                var base64String = Convert.ToBase64String(hash);
                return base64String;
            }
        }

        private void sairToolStripMenuItem_Click(object sender, EventArgs e)
        {
            logado = false;
            this.Visible = true;
            this.WindowState = FormWindowState.Normal;
            Properties.Settings.Default.User = null;
            Properties.Settings.Default.Pass = null;
            Properties.Settings.Default.DeviceID = null;

            Properties.Settings.Default.Folder = string.Empty;
            Properties.Settings.Default.Save();
            Properties.Settings.Default.Reload();
            notifyIcon1.Text = "Deslogado";
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            if (System.IO.File.Exists(pastapath)) System.IO.File.Delete(pastapath);
            automatic = false;
            f1_VisibleChanged(null, null);
        }

        private void receberNotificaçõesToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            checkBox1.Checked = receberNotificaçõesToolStripMenuItem.Checked;
            Properties.Settings.Default.Notification = receberNotificaçõesToolStripMenuItem.Checked;
            Properties.Settings.Default.Save();
        }

        private void abrirToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }


        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == SingleInstance.WM_SHOWFIRSTINSTANCE)
            {
                ShowWindow();
            }
            base.WndProc(ref message);
        }
        public void ShowWindow()
        {
            // Insert code here to make your form show itself.
            if (logado)
            {
                try
                {
                    //uf.StartPosition = FormStartPosition.CenterScreen;
                    this.Visible = true;
                    //  uf.Visible = true;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());

                }

            }
            else
            {
                WinApi.ShowToFront(this.Handle);

            }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // ShowWindow();
        }

        private void definirPreferenciasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(string.Format("http://crm.conteudoproarte.com.br?usuario={0}&chave={1}&aplicacao={2}", Properties.Settings.Default.User, Properties.Settings.Default.acesso, Properties.Settings.Default.DeviceID));

        }

        private void statusDeDownloadToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
        public bool checkUpdates()
        {
            WebClient http = new WebClient();
            feedback fb = new feedback();

            try
            {
                string ver = http.DownloadString(updatesite + "ver.txt");
                Console.WriteLine("Updated Version = " + ver);
                Version vr = JsonConvert.DeserializeObject<Version>(ver);
                Console.WriteLine("Updated  = " + vr);
                if (vr > myvers)
                {
                    Console.WriteLine("Tem que updatar");
                    DialogResult updatequestion = MessageBox.Show("Existe uma nova versão disponivel para baixar deseja continuar ? " + Environment.NewLine + " Versão atual: " + myvers + Environment.NewLine + " Versão para Download: " + vr, "Atualização", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (updatequestion == DialogResult.Yes)
                    {

                        List<string> opts = new List<string>();
                        opts.Add(Properties.Settings.Default.User);
                        opts.Add(Properties.Settings.Default.Pass);
                        opts.Add(Properties.Settings.Default.Folder);
                        opts.Add(Properties.Settings.Default.DeviceID);
                        opts.Add(Properties.Settings.Default.acesso);
                        opts.Add(Properties.Settings.Default.estilo.ToString());



                        fb.StartPosition = FormStartPosition.CenterScreen;
                        fb.Visible = true;
                        if (System.IO.File.Exists(Path.Combine(System.IO.Path.GetTempPath(), "updateproarte.msi"))) System.IO.File.Delete(Path.Combine(System.IO.Path.GetTempPath(), "updateproarte.msi"));
                        http.DownloadFile(new Uri(updatesite + "Instal Pro Arte Sync.msi"), Path.Combine(System.IO.Path.GetTempPath(), "updateproarte.msi"));
                        fb.Close();
                        StreamWriter st = new StreamWriter(Path.Combine(Path.GetTempPath(), "proconfig.txt"));
                        st.Write(JsonConvert.SerializeObject(opts));
                        st.Close();
                        Process.Start(Path.Combine(System.IO.Path.GetTempPath(), "updateproarte.msi"));

                        userclose = false;
                        return true;
                    }
                    return false;

                }
                return false;

            }
            catch
            {
                //if (fb != null) fb.Close();
                return false;
            }
        }

        string MyPcID()
        {
            string cpuInfo = string.Empty;
            ManagementClass mc = new ManagementClass("win32_processor");
            ManagementObjectCollection moc = mc.GetInstances();

            foreach (ManagementObject mo in moc)
            {
                if (cpuInfo == "")
                {
                    //Get only the first CPU's ID
                    cpuInfo = mo.Properties["processorID"].Value.ToString();
                    break;
                }
            }
            return cpuInfo;
        }

        private void procurarAtualizaçoesDoSistemaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!checkUpdates()) MessageBox.Show("Seu programa esta na versão mais atualizada", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            checkUpdates();
        }

        private void receberNotificaçõesToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void f1_FormClosing(object sender, FormClosingEventArgs e)
        {

            DialogResult dr = MessageBox.Show("Voce tem certeza que deseja fechar ?", "Fechar", MessageBoxButtons.YesNo);

            if (dr == DialogResult.Yes)
            {
                while (Process.GetProcessesByName("fix").Length > 0)
                {
                    //  e.Cancel = true;

                    try
                    {
                        Process.GetProcessesByName("fix")[0].Kill();
                    }
                    catch
                    {

                    }
                }
                if (!string.IsNullOrEmpty(Properties.Settings.Default.User))
                {

                    List<string> opts = new List<string>
                    {
                        Properties.Settings.Default.User,
                        Properties.Settings.Default.Pass,
                        Properties.Settings.Default.Folder,
                        Properties.Settings.Default.DeviceID,
                        Properties.Settings.Default.acesso,
                        Properties.Settings.Default.estilo.ToString()
                    };
                    StreamWriter st = new StreamWriter(Path.Combine(Path.GetTempPath(), "proconfig.txt"));
                    st.Write(JsonConvert.SerializeObject(opts));
                    st.Close();
                }
            }
            else e.Cancel = true;


        }

        private void definirPreferenciasNovoToolStripMenuItem_Click(object sender, EventArgs e)
        {



        }

        private void button2_VisibleChanged(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
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
                    UpdatetickAsync(sender, e);
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            pastaspref pf = new pastaspref(pastascontrol)
            {
                StartPosition = FormStartPosition.CenterScreen,
                Visible = true

            };
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void timer3_Tick(object sender, EventArgs e)
        {

            if (this.Visible)
            {
                if (logado)
                {
                    button4.Enabled = firstget;
                    if (f1.filessync)
                    {
                        label6.Text = "Todos arquivos Sincronizados";
                        label6.BackColor = Color.MediumSeaGreen;
                    }
                    else if (f1.isdownloading)

                    {
                        label6.Text = "Baixando arquivos";
                        label6.BackColor = Color.OrangeRed;
                    }
                    else
                    {
                        label6.Text = "Procurando arquivos";
                        label6.BackColor = Color.CornflowerBlue;
                    }
                }
                else
                {
                    label6.Text = "Desconectado";
                    label6.BackColor = Color.OrangeRed;
                }
            }
            //doLoop(null, null);
        }

        private void escolherConteúdoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pastaspref pf = new pastaspref(pastascontrol)
            {
                StartPosition = FormStartPosition.CenterScreen,
                Visible = true

            };
        }

        private void abrirProgramaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Visible = true;

        }

        private void procurarNovosArquivosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (filessync)
                {
                    /* Thread td = new Thread(UpdateFilesAsync);
                     td.Start();*/
                    // updater.CancelAsync();
                    updater.RunWorkerAsync();

                }
                Console.WriteLine("jdjsakdjsa");
            }
            catch
            {
                Console.WriteLine("errouuu");
            }
        }

        private async void timer4_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!logado)
                {
                    notifyIcon1.Icon = myicons[0];
                    await Task.Delay(1000);
                    gambicont++;
                    //doLoop(sender,e);
                }
                else if (!filessync)
                {
                    if (isdownloading)
                    {
                        //notifyIcon1.Text = "Baixando Arquivos";
                        notifyIcon1.Icon = myicons[1];
                    }
                    else
                    {
                        //notifyIcon1.Text = "Procurando novos arquivos";
                        notifyIcon1.Icon = myicons[3];
                    }
                    await Task.Delay(1000);
                    notifyIcon1.Icon = myicons[0];

                    await Task.Delay(1000);
                    //  doLoop(sender, e);
                    gambicont++;

                }
                else
                {
                    notifyIcon1.Icon = myicons[2];
                    await Task.Delay(3000);
                    gambicont++;
                    // doLoop(sender, e);

                }
            }
            catch
            {
                // doLoop(sender, e);

            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            gambicont++;

        }
        ulong oldgambi, gambierros;

        private void updateBOX()
        {
            WebClient http = new WebClient();
            try
            {
                if (logado)
                {
                    string r = http.DownloadString(String.Format("http://crm.conteudoproarte.com.br/!/estilos/?acao=estilo&cliente={0}&senha={1}&estilo={2}",
                        Properties.Settings.Default.User,
                        Properties.Settings.Default.Pass,
                        comboBox1.SelectedIndex));

                    JObject resu = JObject.Parse(r);
                    Console.WriteLine(resu);
                    if ((bool)resu["success"] && !updateafterlogin)
                    {
                        if (!updateafterlogin) MessageBox.Show(resu["msg"].ToString(), resu["titulo"].ToString(), MessageBoxButtons.OK, MessageBoxIcon.None);
                        Properties.Settings.Default.estilo = comboBox1.SelectedIndex;
                        Properties.Settings.Default.Save();
                        updater.CancelAsync();
                        updater.Dispose();
                        canceldown = true;
                        clearFolder(Properties.Settings.Default.Folder);
                        prefpastas = null;
                        if (System.IO.File.Exists(pastapath)) System.IO.File.Delete(pastapath);
                        // updater.RunWorkerAsync();
                        Process.GetCurrentProcess().Kill();
                    }
                    else
                    {
                        int crtl = comboBox1.SelectedIndex == 0 ? 1 : 0;
                        if (!updateafterlogin) MessageBox.Show(resu["msg"].ToString(), resu["titulo"].ToString(), MessageBoxButtons.OK, MessageBoxIcon.None);
                        //comboBox1.SelectedIndex = crtl;
                    }
                }
                else
                {
                    updateafterlogin = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("error meu " + ex);
                if (!updateafterlogin)
                {
                    int crtl = comboBox1.SelectedIndex == 0 ? 1 : 0;
                    MessageBox.Show("Error", "Falha ao se conectar ao servidor", MessageBoxButtons.OK, MessageBoxIcon.None);
                }  //comboBox1.SelectedIndex = crtl;
            }
        }

        private void timer5_Tick(object sender, EventArgs e)
        {
            if (System.IO.File.Exists(timedebugpath)) System.IO.File.Delete(timedebugpath);
            StreamWriter sw = new StreamWriter(timedebugpath);
            sw.Write(JsonConvert.SerializeObject(new Random().Next(0, 999999999)));
            sw.Close();
            if (logado)
            {
                if (oldgambi != gambicont)
                {
                    oldgambi = gambicont;
                    gambierros = 0;
                }
                else
                {
                    gambierros++;
                }
                if (gambierros > 20 && !filessync && !freezed)
                {
                    throw new Exception("Erro de travar e resetar");
                    //Process.GetCurrentProcess().Kill();
                }
            }
            Console.WriteLine("Date time tick oldgambi {0}x{2} e gambierro {1} ", oldgambi, gambierros, gambicont);

            //contextMenuStrip1.Show();
            // contextMenuStrip1.            WindowState = FormWindowState.Minimized;            WindowState = FormWindowState.Minimized;Visible=false;
        }

        public static void CreateShortcut(string shortcutAddress, string target)
        {
            object shDesktop = (object)"Desktop";
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
            shortcut.Description = "Conteudo Pro Arte Sync";
            shortcut.TargetPath = target;
            shortcut.IconLocation = iconpath;
            shortcut.Save();
        }
        private void clearFolder(string FolderName)
        {
            System.IO.DirectoryInfo di = new DirectoryInfo(FolderName);

            foreach (FileInfo file in di.EnumerateFiles())
            {
                try
                {
                    file.Delete();
                }
                catch
                {

                }
            }
            foreach (DirectoryInfo dir in di.EnumerateDirectories())
            {
                try { dir.Delete(true); }
                catch
                {

                }
            }
        }

    }

}


