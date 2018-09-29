using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DropDownloader
{
    class singlefunctions
    {
    }
    static public class ProgramInfo
    {
        static public string AssemblyGuid
        {
            get
            {
                object[] attributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), false);
                if (attributes.Length == 0)
                {
                    return String.Empty;
                }
                return ((System.Runtime.InteropServices.GuidAttribute)attributes[0]).Value;
            }
        }
    }
    static public class SingleInstance
    {
        public static readonly int WM_SHOWFIRSTINSTANCE =
            WinApi.RegisterWindowMessage("WM_SHOWFIRSTINSTANCE|{0}", ProgramInfo.AssemblyGuid);
        static Mutex mutex;
        static public bool Start()
        {
            bool onlyInstance = false;
            // string mutexName = String.Format("Local\\{0}", ProgramInfo.AssemblyGuid);

            // if you want your app to be limited to a single instance
            // across ALL SESSIONS (multiple users & terminal services), then use the following line instead:
            string mutexName = String.Format("Global\\{0}", ProgramInfo.AssemblyGuid);

            mutex = new Mutex(true, mutexName, out onlyInstance);
            return onlyInstance;
        }
        static public void ShowFirstInstance()
        {
            WinApi.PostMessage(
                (IntPtr)WinApi.HWND_BROADCAST,
                WM_SHOWFIRSTINSTANCE,
                IntPtr.Zero,
                IntPtr.Zero);
        }
        static public void Stop()
        {
            mutex.ReleaseMutex();
        }
    }
    static public class WinApi
    {
        [DllImport("user32")]
        public static extern int RegisterWindowMessage(string message);

        public static int RegisterWindowMessage(string format, params object[] args)
        {
            string message = String.Format(format, args);
            return RegisterWindowMessage(message);
        }

        public const int HWND_BROADCAST = 0xffff;
        public const int SW_SHOWNORMAL = 1;

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);

        [DllImportAttribute("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        public static void ShowToFront(IntPtr window)
        {
            ShowWindow(window, SW_SHOWNORMAL);
            SetForegroundWindow(window);
        }
        public  static void ApplyFolderIcon(string targetFolderPath, string iconFilePath)
        {
            var iniPath = Path.Combine(targetFolderPath, "desktop.ini");
            if (File.Exists(iniPath))
            {
                //remove hidden and system attributes to make ini file writable
                File.SetAttributes(
                   iniPath,
                   File.GetAttributes(iniPath) &
                   ~(FileAttributes.Hidden | FileAttributes.System));
            }

            //create new ini file with the required contents
            var iniContents = new StringBuilder()
                .AppendLine("[.ShellClassInfo]")
                .AppendLine($"IconResource={iconFilePath},0")
                .AppendLine($"IconFile={iconFilePath}")
                .AppendLine("IconIndex=0")
                .ToString();
            File.WriteAllText(iniPath, iniContents);

            //hide the ini file and set it as system
            File.SetAttributes(
               iniPath,
               File.GetAttributes(iniPath) | FileAttributes.Hidden | FileAttributes.System);
            //set the folder as system
            File.SetAttributes(
                targetFolderPath,
                File.GetAttributes(targetFolderPath) | FileAttributes.System);
        }
        public static void ListDirectory(TreeView treeView, string path)
        {
            try {
                treeView.Nodes.Clear();

                var stack = new Stack<TreeNode>();
                var rootDirectory = new DirectoryInfo(path);
                var node = new TreeNode(rootDirectory.Name) { Tag = rootDirectory };
                stack.Push(node);

                while (stack.Count > 0)
                {
                    var currentNode = stack.Pop();
                    var directoryInfo = (DirectoryInfo)currentNode.Tag;
                    for (int i = 0; i < directoryInfo.GetDirectories().Length; i++)
                    {

                        var directory = directoryInfo.GetDirectories()[i];
                        var childDirectoryNode = new TreeNode(directory.Name) { Tag = directory };
                        if (i < directoryInfo.GetDirectories().Length - 1)
                        {
                            childDirectoryNode.BackColor = Color.LightGreen;
                        }
                        else childDirectoryNode.BackColor = Color.LightYellow;
                        if (f1.filessync) childDirectoryNode.BackColor = Color.LightGreen;
                        currentNode.Nodes.Add(childDirectoryNode);
                        stack.Push(childDirectoryNode);
                    }
                    for (int i = 0; i < directoryInfo.GetFiles().Length; i++)
                    {

                        var file = directoryInfo.GetFiles()[i];
                        if (file.Name == "desktop.ini") continue;
                        TreeNode tr = new TreeNode(file.Name);

                        if (i < directoryInfo.GetFiles().Length - 1)
                            tr.BackColor = Color.LightGreen;
                        else tr.BackColor = Color.LightYellow;
                        currentNode.Nodes.Add(tr);
                        if (f1.filessync) tr.BackColor = Color.LightGreen;

                        if (i == directoryInfo.GetFiles().Length - 1)
                        {
                            if (tr.Parent.BackColor == Color.LightGreen)
                                tr.BackColor = Color.LightGreen;
                        }

                    }
                }
                node.LastNode.BackColor = Color.LightGreen;
                treeView.Nodes.Add(node);
            }
            catch
            {

            }
            }
        
    }
}
