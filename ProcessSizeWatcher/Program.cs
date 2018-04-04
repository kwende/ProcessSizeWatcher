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
using MathNet.Numerics;

namespace ProcessSizeWatcher
{
    class Program
    {
        static void GetStatsForAllProcesses()
        {
            for (; ; )
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
                        NonPagedMemory = n.NonpagedSystemMemorySize64,
                        PagedMemory = n.PagedSystemMemorySize64
                    }).ToArray();

                DateTime now = DateTime.Now;
                Console.Clear();
                Console.WriteLine("PerProcess: ");
                foreach (var result in results)
                {
                    string line = $"{now.Ticks}, {now.ToString("MM-dd-yyyy HH:mm:ss")}, {result.Name}, {result.Id}, {result.PrivateBytes}, {result.VirtualSize}, {result.WorkingSet}, {result.PagedMemory}, {result.NonPagedMemory}{Environment.NewLine}";
                    Console.Write("\t" + line);
                    File.AppendAllText("perProcessResults.csv", line);
                }

                Interop.MEMORYSTATUSEX memoryStatus = new Interop.MEMORYSTATUSEX();
                if (Interop.GlobalMemoryStatusEx(memoryStatus))
                {
                    string globalLine =
    $"{now.Ticks}, {now.ToString("MM-dd-yyyy HH:mm:ss")}, {memoryStatus.dwMemoryLoad}, {memoryStatus.ullAvailExtendedVirtual}, {memoryStatus.ullAvailPageFile}, {memoryStatus.ullAvailPhys}, {memoryStatus.ullAvailVirtual}, {memoryStatus.ullTotalPageFile}, {memoryStatus.ullTotalPhys}, {memoryStatus.ullTotalVirtual}{Environment.NewLine}";
                    Console.WriteLine("SystemWide: ");
                    Console.WriteLine("\t" + globalLine);
                    File.AppendAllText("systemWideResults.csv", globalLine);
                }



                Thread.Sleep(1000 * 60);
            }
        }

        static void GetStatsForProcess(string processName)
        {

        }

        static void AnalyzeStats2(string resultsPath)
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


            List<int> activeListOfProcessIds = new List<int>();
            for (int c = 1; c < tickStamps.Count; c++)
            {
                long currentTickStamp = tickStamps[c];
            }
        }

        static void AnalyzeStatsFile(string resultsPath)
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

            processIdsOfInterest.Add(allStats.Where(n => n.ProcessName == "Ocuvera.Video.PerformanceTestApp").Select(n => n.ProcessId).First());

            const string MemoryDirectory = "C:/users/brush/desktop/memory/";
            const double MinSlope = 0;// 10 * 1024;

            if (Directory.Exists(MemoryDirectory))
            {
                Directory.Delete(MemoryDirectory, true);
            }
            Directory.CreateDirectory(MemoryDirectory);

            int numberFound = 0;
            foreach (int processId in processIdsOfInterest)
            {
                string processName = allStats.Where(n => n.ProcessId == processId).First().ProcessName;
                DateTime[] times = allStats.Where(n => n.ProcessId == processId).Select(n => n.Time).ToArray();
                double[] workingSetValues = allStats.Where(n => n.ProcessId == processId).Select(n => (double)n.WorkingSet).ToArray();
                double[] privatByteValues = allStats.Where(n => n.ProcessId == processId).Select(n => (double)n.PrivateBytes).ToArray();
                double[] virtualByteValues = allStats.Where(n => n.ProcessId == processId).Select(n => (double)n.VirtualBytes).ToArray();
                double[] x = new double[virtualByteValues.Length];
                for (int c = 1; c <= x.Length; c++) x[c - 1] = c;

                double workingSetSlope = Fit.Line(x, workingSetValues).Item2;
                double privatBytesSlope = Fit.Line(x, privatByteValues).Item2;
                double virtualByteSlope = Fit.Line(x, virtualByteValues).Item2;

                Console.WriteLine($"{processName}: {virtualByteSlope / 1024} K/min.");

                if (workingSetSlope > MinSlope || privatBytesSlope > MinSlope || virtualByteSlope > MinSlope)
                {
                    using (StreamWriter fout = new StreamWriter($"{MemoryDirectory}{processName}_{processId}.csv"))
                    {
                        fout.WriteLine("Time,Working Set,PrivateBytes,VirtualBytes");
                        for (int c = 0; c < workingSetValues.Length; c++)
                        {
                            fout.WriteLine($"{times[c].ToString("MM-dd-yyyy HH:mm:ss")},{workingSetValues[c]}, {privatByteValues[c]}, {virtualByteValues[c]}");
                        }
                    }

                    numberFound++;
                }
            }

            Console.WriteLine($"Found {numberFound} matching processes.");
            Console.ReadLine();

            return;
        }

        static void LaunchAndGetStatsForOneProcess(string processFilePath)
        {
            Process proc = Process.Start(processFilePath); 

            for (; ; )
            {
                proc.Refresh(); 
                DateTime now = DateTime.Now;
                Console.WriteLine("PerProcess: ");
                string line = $"{now.Ticks}, {now.ToString("MM-dd-yyyy HH:mm:ss")}, {proc.ProcessName}, {proc.Id}, {proc.PrivateMemorySize64}, {proc.VirtualMemorySize64}, {proc.WorkingSet64}, {proc.PagedSystemMemorySize64}, {proc.NonpagedSystemMemorySize64}{Environment.NewLine}";

                string fileName = Path.GetFileNameWithoutExtension(processFilePath); 
                File.AppendAllText($"{fileName}_MemoryResults.csv", line);
                Console.WriteLine(line); 
                Thread.Sleep(1000 * 5);
            }
        }

        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0].EndsWith(".csv"))
            {
                AnalyzeStatsFile(args[0]);
            }
            else if(args.Length > 0 && args[0].EndsWith(".exe"))
            {
                LaunchAndGetStatsForOneProcess(args[0]); 
            }
            else
            {
                GetStatsForAllProcesses();
            }
        }
    }
}
