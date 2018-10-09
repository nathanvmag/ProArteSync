using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
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
            f1 mainForm = null;

            try
            {
                WinApi.RefreshTrayArea();

                if (!SingleInstance.Start())
                {
                    SingleInstance.ShowFirstInstance();
                    return;
                }
                if (!first)
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.ThreadException += new ThreadExceptionEventHandler( ThreadHandler);

                    // Set the unhandled exception mode to force all Windows Forms errors to go through
                    // our handler.
                  Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                    
                    // Add the event handler for handling non-UI thread exceptions to the event. 
                    AppDomain.CurrentDomain.UnhandledException +=
                       new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

                }

                if (!f1.logado && !first)
                {
                    mainForm = new f1();
                    
                    Console.WriteLine("firstEnter");

                    if (!mainForm.checkUpdates())
                        Application.Run(mainForm);
                    first = true;
                }
                SingleInstance.Stop();



            }
            catch(Exception ex)
            {

                Process.GetCurrentProcess().Kill();
                if (mainForm!=null)
                mainForm.Close();
                Application.ExitThread();
                Application.Exit();
            }
        }
 
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            //DialogResult dr = MessageBox.Show("Error " + e.ExceptionObject.ToString(), "Erro", MessageBoxButtons.OK);

            Console.WriteLine("tive esse erro "+e.ExceptionObject.ToString());
            Process.GetCurrentProcess().Kill();
            Application.ExitThread();
            Application.Exit();
        }
         
        private static void ThreadHandler(object sender, ThreadExceptionEventArgs e)
        {
           // DialogResult dr = MessageBox.Show("Error " + e.Exception.ToString(), "Erro", MessageBoxButtons.OK);

            Console.WriteLine("tive esse erro "+e.Exception.ToString());
            Process.GetCurrentProcess().Kill();
            Application.ExitThread();
            Application.Exit();
        }
    }
}
    

