using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessSizeWatcher
{
    class Program
    {
        static void Main(string[] args)
        {
            for (;;)
            {
                Process[] allProcs = Process.GetProcesses();
                var results =
                    allProcs.Select(n => new
                    {
                        Id = n.Id,
                        PrivateBytes = n.PrivateMemorySize64,
                        VirtualSize = n.VirtualMemorySize64,
                        Name = n.ProcessName
                    }).ToArray();

                DateTime now = DateTime.Now;
                Console.Clear();
                foreach (var result in results)
                {
                    string line = $"{now.Ticks}, {result.Name}, {result.Id}, {result.PrivateBytes}, {result.VirtualSize}{Environment.NewLine}";
                    Console.Write(line);
                    File.AppendAllText("results.csv", line);
                }

                Thread.Sleep(1000 * 60);
            }
        }
    }
}
