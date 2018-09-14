using Dropbox.Api;
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

namespace DropDownloader
{
    public partial class f1 : Form
    {
        static public bool logado = false;
        DropboxClient dbx;
        int syncerrs = 0;
        string syncpath;
        const string site = "http://crm.conteudoproarte.com.br/!/consulta";
        const string updatesite = "http://crm.conteudoproarte.com.br/update/";
        public bool automatic;
        protected string DeviceID;
        public static bool isdownloading, filessync;
        public static string donwpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "downloaddocs.txt");
        public static f1 me;
        public static string iconpath = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"\foldericon.ico";
        Userinfos uf;
        BackgroundWorker updater;
        Version myvers;
        int updatecount;
        bool userclose;
        List<string> PastaNames;
        public f1()
        {
            InitializeComponent();

            userclose = false;
            if (System.IO.File.Exists(Path.Combine(Path.GetTempPath(), "proconfig.txt")))
            {
                StreamReader sr = new StreamReader(Path.Combine(Path.GetTempPath(), "proconfig.txt"));
                string confi = sr.ReadToEnd();
                sr.Close();
                List<string> st = JsonConvert.DeserializeObject<List<string>>(confi);
                Properties.Settings.Default.User = st[0];
                Properties.Settings.Default.Pass= st[1];
                Properties.Settings.Default.Folder= st[2];
                Properties.Settings.Default.DeviceID= st[3];
                Properties.Settings.Default.acesso= st[4];
                Properties.Settings.Default.Save();
                Properties.Settings.Default.Reload();
                System.IO.File.Delete(Path.Combine(Path.GetTempPath(), "proconfig.txt"));

            }
            login();
            iconpath = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"\foldericon.ico";
            filessync = true;
            isdownloading = false;
            me = this;
            automatic = false;
            syncerrs = 0;
            SetStartup();
            myvers = Assembly.GetExecutingAssembly().GetName().Version;
            Console.WriteLine(myvers);
            Console.WriteLine(JsonConvert.SerializeObject(myvers));
            if (string.IsNullOrEmpty(Properties.Settings.Default.DeviceID))
            {
                Properties.Settings.Default.DeviceID = MyPcID();
                Properties.Settings.Default.Save();
                Properties.Settings.Default.Reload();
            }
            if(!string.IsNullOrEmpty(Properties.Settings.Default.User)&&
               !string.IsNullOrEmpty(Properties.Settings.Default.Pass) &&
               !string.IsNullOrEmpty(Properties.Settings.Default.Folder))
                {
                automatic = true;
                textBox1.Text= Properties.Settings.Default.User;
                    textBox2.Text= Properties.Settings.Default.Pass;
                    textBox3.Text= Properties.Settings.Default.Folder ;
                    checkBox1.Checked = Properties.Settings.Default.Notification;
                    button1_ClickAsync(null, EventArgs.Empty);
                
                    
            }
           
            Console.WriteLine(iconpath);
            notifyIcon1.Text = "Pro Arte Sync";
            BackgroundWorker looper = new BackgroundWorker();
            looper.DoWork += doLoop;
            looper.RunWorkerAsync();
            updater = new BackgroundWorker();
            updater.DoWork += UpdateFilesAsync;
            updater.WorkerSupportsCancellation = true;
            updater.RunWorkerAsync();
            updater.RunWorkerCompleted += Updater_RunWorkerCompleted;


            uf = new Userinfos(this);
            updatecount = 0;
            PastaNames = new List<string>();
            
        }

        private void Updater_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Console.WriteLine("FINISH MY WORK");
        }

        private static readonly string StartupKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private static readonly string StartupValue = "MyApplicationName";

        private async void doLoop(object sender, DoWorkEventArgs e)
        {            
                try
                {
                    if (!logado)
                    {
                        notifyIcon1.Icon = Icon.FromHandle(Properties.Resources.Ícone.GetHicon());
                        await Task.Delay(3000);
                        doLoop(sender,e);
                    }
                    else if (!filessync)
                    {
                        if (isdownloading)
                            notifyIcon1.Icon = Icon.FromHandle(Properties.Resources.baixando.GetHicon());
                        else notifyIcon1.Icon = Icon.FromHandle(Properties.Resources.Procurando.GetHicon());

                        await Task.Delay(1000);
                        notifyIcon1.Icon = Icon.FromHandle(Properties.Resources.Ícone.GetHicon());

                        await Task.Delay(1000);
                        doLoop(sender, e);

                }
                else
                    {
                        notifyIcon1.Icon = Icon.FromHandle(Properties.Resources.atualizado.GetHicon());
                        await Task.Delay(3000);
                        doLoop(sender, e);

                      }
            }
                catch
                {
                    doLoop(sender, e);

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
                Hide();
            }
        }

        private void button1_ClickAsync(object sender, EventArgs e)
        {
            int counter = 0;
            while (dbx == null && counter < 3)
            {
                login();
                counter++;
            }
            if (!string.IsNullOrEmpty(textBox3.Text) && dbx != null)
            {
                JObject jobject = null;
                try
                {
                    if (string.IsNullOrWhiteSpace( textBox3.Text)||!textBox3.Text.Contains("Conteudo Pro Arte"))
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
                    if(!Directory.Exists(syncpath))
                    {
                        Console.WriteLine("Criou e mudou icone");
                        Directory.CreateDirectory(syncpath);
                        WinApi.ApplyFolderIcon(syncpath, iconpath);
                        if (!System.IO.File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)+@"\Conteudo Pro Arte Sync.lnk"))
                        {
                            CreateShortcut((string)Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Conteudo Pro Arte Sync.lnk",syncpath);
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
                        if(!System.IO.File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Conteudo Pro Arte Sync.lnk"))
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
                        if (!(bool)jobject["alerta"])
                        {
                            if(!automatic)this.Hide();
                            ShowInTaskbar = true;                            
                            notifyIcon1.Visible = true;
                            notifyIcon1.Text = "Pro Arte Sync";
                            logado = true;

                        }
                        else
                        {
                            MessageBox.Show("Alerta : " + jobject["mensagem"]["mensagem"].ToString(), jobject["mensagem"]["titulo"].ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                            if(!automatic)this.Hide();
                            ShowInTaskbar = true;
                            notifyIcon1.Visible = true;
                            notifyIcon1.Text = "Logado";
                            notifyIcon1.Icon = Icon.FromHandle( Properties.Resources.atualizado.GetHicon());
                            logado = true;
                        }
                        Console.WriteLine(notifyIcon1.Text);
                        notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
                        notifyIcon1.BalloonTipText = "Logado com sucesso";
                        notifyIcon1.BalloonTipTitle = this.Text;
                        notifyIcon1.ShowBalloonTip(1000);

                       /* Thread td = new Thread(UpdateFilesAsync);
                        td.Start();*/
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                catch(Exception xe)
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

            }
            else if (dbx == null) MessageBox.Show("Erro ao conectar com servidor de sincronia", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else MessageBox.Show("Preencha todos os campos", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //automatic = false;
        }



        private void button2_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                if (folderBrowserDialog1.SelectedPath.Length < 5)
                    MessageBox.Show("Local Inválido, por favor selecione outro", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    textBox3.Text = folderBrowserDialog1.SelectedPath;
            }
        }
        async void login()
        {
            try
            {
                dbx = new DropboxClient("CiLrR88Cdh8AAAAAAABrC9xxm4llVWkCJJbB0yJ9HCSfsqsO1GyRH5qD3sZSpsUP");                
                    var full = await dbx.Users.GetCurrentAccountAsync();
                    Console.WriteLine("{0} - {1}", full.Name.DisplayName, full.Email);
                    var list = await dbx.Files.ListFolderAsync(string.Empty);                                 
            }
            catch
            {
               if (Visible) MessageBox.Show("Falha ao se conectar ao servidor do Dropbox", "Falha", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                {
                    notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
                    notifyIcon1.BalloonTipText = "Falha ao se conectar no Dropbox";
                    notifyIcon1.BalloonTipTitle = this.Text;
                    notifyIcon1.ShowBalloonTip(1000);
                }
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
            ServicePointManager.Expect100Continue = true;
            WebClient wb = new WebClient();
            wb.Encoding = Encoding.UTF8;
            var reqparm = new System.Collections.Specialized.NameValueCollection();
            reqparm.Add("usuario", user);
            reqparm.Add("senha", pass);
            reqparm.Add("app", Properties.Settings.Default.DeviceID);
            byte[] responsebytes = wb.UploadValues(site, "POST", reqparm);
            string responsebody = Encoding.UTF8.GetString(responsebytes);
            Console.WriteLine("resultado é " + responsebody);
            return responsebody;
        }

         async void UpdateFilesAsync(object sender, EventArgs e)
        {
            try
            {
                if (this.Visible) this.Hide();
                if (dbx == null)
                {
                    login();
                }
                if (logado)
                {
                    if (dbx == null) return;
                    updatecount++;
                    try
                    {
                        filessync = false;
                        notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
                        notifyIcon1.BalloonTipText = "Atualizando arquivos...";

                        notifyIcon1.BalloonTipTitle = this.Text;
                        if (checkBox1.Checked) notifyIcon1.ShowBalloonTip(1000);
                        JObject jobject = null;

                        syncpath = Properties.Settings.Default.Folder;
                        string resu = MakeLogin(textBox1.Text, textBox2.Text);
                        jobject = JObject.Parse(resu);
                        // JArray controle = (JArray)jobject["controle"];
                        /*for (int z = 0; z < controle.Count; z++)
                        {
                            if (!PastaNames.Contains(controle[z].ToString()))
                                PastaNames.Add(controle[z].ToString());
                        }
                        foreach (string s in PastaNames)
                            Console.WriteLine("Nome de pasta " + s);
                            */
                        if ((bool)jobject["sucesso"] == true)
                        {
                            if (this.Visible) this.Hide();
                            syncerrs = 0;
                            Properties.Settings.Default.acesso = jobject["acesso"].ToString();
                            Properties.Settings.Default.Save();
                            Properties.Settings.Default.Reload();
                            Console.WriteLine("Acesso " + jobject["acesso"]);
                            JArray lista = (JArray)jobject["lista"];
                            for (int i = 0; i < lista.Count; i++)
                            {
                                notifyIcon1.Text = "Atualizando arquivos " + (i + 1) + "/" + lista.Count;

                                if (!logado) break;
                                try
                                {
                                    DateTime now = DateTime.Now;
                                    lista[i]["origem"] = "/" + lista[i]["origem"].ToString().Replace("%dia%", now.Day > 9 ? now.Day.ToString() : "0" + now.Day).Replace("%mes%", now.Month > 9 ? now.Month.ToString() : "0" + now.Month).Replace("%ano%", now.Year.ToString());
                                    Console.WriteLine("MEU CAMINHO " + lista[i]["origem"]);
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
                                                    i--;
                                                    Console.WriteLine("Replay");
                                                    continue;
                                                }
                                                Console.WriteLine("total memory antes" + GC.GetTotalMemory(true));

                                                files.Close();
                                                fileStream.Close();
                                                file.Dispose();
                                                files.Dispose();
                                                fileStream.Dispose();

                                            }
                                            notifyIcon1.Icon = Icon.FromHandle(Properties.Resources.Procurando.GetHicon());
                                            isdownloading = false;
                                            Console.WriteLine("total memory dps" + GC.GetTotalMemory(true));


                                        }
                                    }
                                }
                                catch (Exception ex)
                                {

                                    Console.WriteLine("error " + ex.ToString());
                                }
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                            }
                            JArray pastas = (JArray)jobject["pastas"];
                            for (int i = 0; i < pastas.Count; i++)
                            {
                                if (!logado) break;

                                try
                                {
                                    isdownloading = true;
                                    string oldOrigem = pastas[i]["origem"].ToString();
                                    notifyIcon1.Text = "Baixando pastas de arquivos " + oldOrigem.Split('/')[oldOrigem.Split('/').Length - 1];
                                    pastas[i]["origem"] = "/" + pastas[i]["origem"];
                                    var list = await dbx.Files.ListFolderAsync(pastas[i]["origem"].ToString());
                                    string destpath = Path.Combine(syncpath, pastas[i]["destino"].ToString());

                                    if (!Directory.Exists(destpath)) Directory.CreateDirectory(destpath);
                                    // show folders then files


                                    foreach (var item in list.Entries.Where(z => z.IsFile))
                                    {
                                        Console.WriteLine("F{0,8} {1}", item.AsFile.Size, item.Name);
                                        Console.WriteLine("meta data de " + pastas[i]["origem"].ToString() + item.Name);
                                        destpath = Path.Combine(syncpath, pastas[i]["destino"].ToString());
                                        Metadata data = await dbx.Files.GetMetadataAsync(pastas[i]["origem"].ToString() + item.Name);

                                        destpath += item.Name;
                                        Console.WriteLine("destpath " + destpath);
                                        DateTime now = DateTime.Now;
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
                                        using (var file = dbx.Files.DownloadAsync(lista[i]["origem"].ToString()).Result)
                                        {
                                            using (var files = await file.GetContentAsStreamAsync())
                                            {
                                                using (FileStream fileStream = System.IO.File.Create(destpath))
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
                                                }
                                                notifyIcon1.Icon = Icon.FromHandle(Properties.Resources.Procurando.GetHicon());
                                                isdownloading = false;
                                                GC.Collect();
                                                GC.WaitForPendingFinalizers();
                                                GC.Collect();
                                                GC.WaitForPendingFinalizers();

                                            }
                                        }
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
                                catch
                                {

                                }

                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
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
                        if (receberNotificaçõesToolStripMenuItem.Checked) notifyIcon1.ShowBalloonTip(1000);

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
                            notifyIcon1.ShowBalloonTip(1000);
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

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            updater.CancelAsync();

        }
        public void updatetickAsync(object sender, EventArgs e)
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
            DialogResult dr = MessageBox.Show("Você tem certeza que deseja sair ? Os arquivos não serão mais sincronizados", "Sair", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if(dr==DialogResult.Yes)
            {
                userclose = true;
                this.Close();
                Application.Exit();

            }
        }

        private void f1_VisibleChanged(object sender, EventArgs e)
        {
            Console.WriteLine("changed");
            if (logado) this.Hide();
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
            notifyIcon1.Icon= Icon.FromHandle(Properties.Resources.Ícone.GetHicon());
            automatic = false;
        }

        private void receberNotificaçõesToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            checkBox1.Checked = receberNotificaçõesToolStripMenuItem.Checked;
            Properties.Settings.Default.Notification = receberNotificaçõesToolStripMenuItem.Checked;
            Properties.Settings.Default.Save();
        }

        private void abrirToolStripMenuItem_Click(object sender, EventArgs e)
        {   if (!Visible && !logado) Visible = true;
              else
            ShowWindow();

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
                    uf.StartPosition = FormStartPosition.CenterScreen;

                    uf.Visible = true;
                }
                catch (Exception e){
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
            Process.Start(string.Format("http://crm.conteudoproarte.com.br?usuario={0}&chave={1}&aplicacao={2}",Properties.Settings.Default.User,Properties.Settings.Default.acesso,Properties.Settings.Default.DeviceID));

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
                string ver =  http.DownloadString(updatesite + "ver.txt");
                Console.WriteLine("Updated Version = " + ver);
                Version vr = JsonConvert.DeserializeObject<Version>(ver);
                Console.WriteLine("Updated  = " + vr);
                if (vr >myvers)
                {
                    Console.WriteLine("Tem que updatar");
                    DialogResult updatequestion= MessageBox.Show("Existe uma nova versão disponivel para baixar deseja continuar ? "+Environment.NewLine+" Versão atual: "+myvers+ Environment.NewLine + " Versão para Download: " + vr,"Atualização",MessageBoxButtons.YesNo,MessageBoxIcon.Question);
                    if (updatequestion==DialogResult.Yes)
                    {

                        List<string> opts = new List<string>();
                        opts.Add(Properties.Settings.Default.User);
                        opts.Add(Properties.Settings.Default.Pass);
                        opts.Add(Properties.Settings.Default.Folder);
                        opts.Add(Properties.Settings.Default.DeviceID);
                        opts.Add(Properties.Settings.Default.acesso);
                       


                        fb.StartPosition = FormStartPosition.CenterScreen;
                        fb.Visible = true;
                        if (System.IO.File.Exists(Path.Combine(System.IO.Path.GetTempPath(), "updateproarte.msi")))System.IO.File.Delete(Path.Combine(System.IO.Path.GetTempPath(), "updateproarte.msi"));
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
            if (!checkUpdates()) MessageBox.Show("Seu programa esta na versão mais atualizada", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }

        private void receberNotificaçõesToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void f1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(!userclose)
            {
                e.Cancel = true;
                return;
            }
            DialogResult dr= MessageBox.Show("Voce tem certeza que deseja fechar ?", "Fechar", MessageBoxButtons.YesNo);

            if (dr == DialogResult.Yes)
                if (string.IsNullOrEmpty(Properties.Settings.Default.User))
                {
                    List<string> opts = new List<string>();
                    opts.Add(Properties.Settings.Default.User);
                    opts.Add(Properties.Settings.Default.Pass);
                    opts.Add(Properties.Settings.Default.Folder);
                    opts.Add(Properties.Settings.Default.DeviceID);
                    opts.Add(Properties.Settings.Default.acesso);
                    StreamWriter st = new StreamWriter(Path.Combine(Path.GetTempPath(), "proconfig.txt"));
                    st.Write(JsonConvert.SerializeObject(opts));
                    st.Close();
                }
                else e.Cancel= true;
        }

        private void definirPreferenciasNovoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pastaspref pf = new pastaspref(PastaNames)
            { StartPosition = FormStartPosition.CenterScreen, Visible = true

            };
            

        }

        public static void CreateShortcut(string shortcutAddress,string target)
        {
            object shDesktop = (object)"Desktop";
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
            shortcut.Description = "Conteudo Pro Arte Sync";
            shortcut.TargetPath = target;
            shortcut.IconLocation = iconpath;
            shortcut.Save();
        }
    }
    
}
    

