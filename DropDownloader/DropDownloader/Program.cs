using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Windows.Forms;

namespace DropDownloader
{
    static class Program
    {
        static bool first = false;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
             
                if (!SingleInstance.Start())
                {
                    SingleInstance.ShowFirstInstance();
                    return;
                }
                if (!first)
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                }

                if (!f1.logado && !first)
                {
                    Console.WriteLine("firstEnter");

                    f1 mainForm = new f1();
                    if (!mainForm.checkUpdates())
                        Application.Run(mainForm);
                    first = true;
                }
                SingleInstance.Stop();
            }
            catch (Exception e)
            {
                MessageBox.Show("Erro=  "+e.ToString());
                //killapp();
            }

        }
        public static void killapp()
        {/*
            try
            {
                Process.Start(Application.ExecutablePath);
                Process.GetCurrentProcess().Kill();
            }
            catch
            { }*/
        }
    }
}

