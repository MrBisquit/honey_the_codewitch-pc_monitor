using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace PCMonitor
{
    public static class Utils
    {
        // Modified but mostly copied from https://stackoverflow.com/a/41617624
        public static void RestartElevated()
        {
            if (IsRunAsAdmin()) return;


            ProcessStartInfo proc = new ProcessStartInfo();
            proc.UseShellExecute = true;
            proc.WorkingDirectory = Environment.CurrentDirectory;
            proc.FileName = Assembly.GetEntryAssembly().CodeBase;

            foreach (string arg in Environment.GetCommandLineArgs())
            {
                proc.Arguments += String.Format("\"{0}\" ", arg);
            }

            proc.Verb = "runas";
            Process.Start(proc);

            // Kill the current application

            Environment.Exit(0);
        }

        public static bool IsRunAsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(id);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
