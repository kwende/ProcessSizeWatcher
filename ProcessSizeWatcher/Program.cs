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
        static void GetStats()
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
                        Name = n.ProcessName,
                        WorkingSet = n.WorkingSet64,
                    }).ToArray();

                DateTime now = DateTime.Now;
                Console.Clear();
                foreach (var result in results)
                {
                    string line = $"{now.Ticks}, {result.Name}, {result.Id}, {result.PrivateBytes}, {result.VirtualSize}, {result.WorkingSet}{Environment.NewLine}";
                    Console.Write(line);
                    File.AppendAllText("results.csv", line);
                }

                Thread.Sleep(1000 * 60);
            }
        }

        static void AnalyzeStats(string resultsPath)
        {
            Stat[] allStats = Stat.ParseStatsFromFile(resultsPath);

            // how many tickstamps exist? 
            List<long> tickStamps = new List<long>();
            for (int c = 0; c < allStats.Length; c++)
            {
                long tickStamp = allStats[c].TickStamp;
                if (!tickStamps.Contains(tickStamp))
                {
                    tickStamps.Add(tickStamp);
                }
            }
            int totalNumberOfTicks = tickStamps.Count;

            // find the initial list of process ids. 
            long firstTimeStamp = allStats[0].TickStamp;
            int[] initialList = allStats.Where(n => n.TickStamp == firstTimeStamp).Select(n => n.ProcessId).ToArray();

            // how long did each of these process ids stick around? if the entire length, remember it
            List<int> processIdsOfInterest = new List<int>();
            foreach (int processId in initialList)
            {
                int numberOfTimesThisProcessIdShowedUp = allStats.Count(n => n.ProcessId == processId);
                if (numberOfTimesThisProcessIdShowedUp == totalNumberOfTicks)
                {
                    processIdsOfInterest.Add(processId);
                }
            }

            //List<int> processIdsThatMightLeak = new List<int>();
            //foreach (int processIdOfInterest in processIdsOfInterest)
            //{
            //    Stat[] statsForThisProcessId = allStats.Where(n => n.ProcessId == processIdOfInterest).ToArray();
            //    long[] privateBytesOverTime = statsForThisProcessId.Select(n => n.PrivateBytes).ToArray();
            //    long[] workingSetOverTime = statsForThisProcessId.Select(n => n.WorkingSet).ToArray();
            //    long[] virtualBytesOverTime = statsForThisProcessId.Select(n => n.VirtualBytes).ToArray();

            //    int endIndex = privateBytesOverTime.Length - 1;

            //    // look every hour for an increase in any of these values, continue so long as so. 
            //    if (privateBytesOverTime[totalNumberOfTicks - 1] - privateBytesOverTime[0] > 0 &&
            //        workingSetOverTime[totalNumberOfTicks - 1] - workingSetOverTime[0] > 0 &&
            //        virtualBytesOverTime[totalNumberOfTicks - 1] - virtualBytesOverTime[0] > 0)
            //    {
            //        processIdsThatMightLeak.Add(processIdOfInterest);
            //    }
            //}

            //Console.WriteLine("These processes continued to increase...");

            //foreach (int processId in processIdsThatMightLeak)
            //{
            //    string processName = allStats.Where(n => n.ProcessId == processId).First().ProcessName;
            //    Console.WriteLine();
            //    long[] workingSetValues = allStats.Where(n => n.ProcessId == processId).Select(n => n.WorkingSet).ToArray();
            //    using (StreamWriter fout = new StreamWriter($"C:/users/brush/desktop/memory/{processName}.csv"))
            //    {
            //        foreach (long workingSetValue in workingSetValues)
            //        {
            //            fout.WriteLine($"{workingSetValue}");
            //        }
            //    }
            //}

            foreach (int processId in processIdsOfInterest)
            {
                string processName = allStats.Where(n => n.ProcessId == processId).First().ProcessName;
                Console.WriteLine();
                long[] workingSetValues = allStats.Where(n => n.ProcessId == processId).Select(n => n.WorkingSet).ToArray();
                long[] privatByteValues = allStats.Where(n => n.ProcessId == processId).Select(n => n.PrivateBytes).ToArray();
                long[] virtualByteValues = allStats.Where(n => n.ProcessId == processId).Select(n => n.VirtualBytes).ToArray();
                using (StreamWriter fout = new StreamWriter($"C:/users/brush/desktop/memory/{processName}.csv"))
                {
                    fout.WriteLine("Working Set,PrivateBytes,VirtualBytes");
                    for (int c = 0; c < workingSetValues.Length; c++)
                    {
                        fout.WriteLine($"{workingSetValues[c]}, {privatByteValues[c]}, {virtualByteValues[c]}");
                    }
                }
            }

            Console.ReadLine();

            return;
        }

        static void Main(string[] args)
        {
            AnalyzeStats("C:/users/brush/desktop/results.csv");
        }
    }
}
