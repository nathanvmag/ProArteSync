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
                f1 mainForm = new f1();

                Console.WriteLine("firstEnter");

                    if (!mainForm.checkUpdates())
                        Application.Run(mainForm);
                    first = true;
                }
                SingleInstance.Stop();          
          

        }        
    }
}

