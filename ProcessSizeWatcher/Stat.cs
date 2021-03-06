﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessSizeWatcher
{
    public class Stat
    {
        public long TickStamp { get; set; }
        public int ProcessId { get; set; }
        public long PrivateBytes { get; set; }
        public long VirtualBytes { get; set; }
        public string ProcessName { get; set; }
        public long WorkingSet { get; set; }
        public DateTime Time { get; set; }

        public static Stat[] ParseStatsFromFile(string fileName)
        {

            string[] lines = File.ReadAllLines(fileName);

            List<Stat> stats = new List<Stat>();
            foreach (string line in lines)
            {
                string[] bits = line.Split(',').Select(n => n.Trim()).ToArray();

                Stat stat = new Stat
                {
                    TickStamp = long.Parse(bits[0]),
                    Time = DateTime.ParseExact(bits[1], "MM-dd-yyyy HH:mm:ss", CultureInfo.InvariantCulture),
                    ProcessName = bits[2],
                    ProcessId = int.Parse(bits[3]),
                    PrivateBytes = long.Parse(bits[4]),
                    VirtualBytes = long.Parse(bits[5]),
                    WorkingSet = long.Parse(bits[6]),
                };

                stats.Add(stat);
            }

            return stats.ToArray();
        }
    }
}
