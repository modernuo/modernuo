using System;
using System.Collections.Generic;
using System.IO;

namespace Server
{
    public static class ShrinkTable
    {
        public const int DefaultItemID = 0x1870; // Yellow virtue stone

        private static int[] m_Table;

        public static int Lookup(Mobile m) => Lookup(m.Body.BodyID, DefaultItemID);

        public static int Lookup(int body) => Lookup(body, DefaultItemID);

        public static int Lookup(Mobile m, int defaultValue) => Lookup(m.Body.BodyID, defaultValue);

        public static int Lookup(int body, int defaultValue)
        {
            m_Table ??= Load();
            var index = body >= 0 && body < m_Table.Length ? m_Table[body] : -1;
            if (index < 0)
            {
                return defaultValue;
            }

            var val = m_Table[body];
            return val == 0 ? defaultValue : val;
        }

        private static int[] Load()
        {
            var path = Path.Combine(Core.BaseDirectory, "Data/shrink.cfg");

            if (!File.Exists(path))
            {
                Console.WriteLine("No table found!");
                return Array.Empty<int>();
            }

            var list = new List<(int, int)>();
            var length = 0;

            using var ip = new StreamReader(path);
            string line;

            while ((line = ip.ReadLine()) != null)
            {
                line = line.Trim();

                if (line.Length == 0 || line.StartsWithOrdinal("#"))
                {
                    continue;
                }

                try
                {
                    var split = line.Split('\t');

                    if (split.Length >= 2)
                    {
                        var body = Utility.ToInt32(split[0]);
                        var item = Utility.ToInt32(split[1]);

                        if (body < 0)
                        {
                            continue;
                        }

                        list.Add((body, item));

                        length = Math.Max(body + 1, length);
                    }
                }
                catch
                {
                    // ignored
                }
            }

            var table = new int[length];
            foreach (var (body, item) in list)
            {
                table[body] = item;
            }

            return table;
        }
    }
}
