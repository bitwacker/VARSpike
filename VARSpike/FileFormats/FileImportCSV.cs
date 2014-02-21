using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;

namespace VARSpike.FileFormats
{
    public static class FileImportCSV
    {
       public static IEnumerable<string[]> GetRows(TextReader reader)
       {
           string line = null;
           while ((line = reader.ReadLine()) != null)
           {
               if (string.IsNullOrWhiteSpace(line)) continue;
               

               yield return ParseLine(line);
           }
       }

        private static string[] ParseLine(string line)
        {
            //TODO: This should read forward to find comma-escaping
            return line.Split(',');
        }

        public class ColumnParser<T>
        {
            public string ColumnName { get; set; }
            public Action<T, string> ParseColumn { get; set; }
        }

        public static IEnumerable<TRow> Parse<TRow>(TextReader reader, params ColumnParser<TRow>[] parsers) where TRow:new()
        {
            string[] first = null;
            ColumnParser<TRow>[] orderedParsers = null;
            foreach (var row in GetRows(reader))
            {
                if (first == null)
                {
                    first = row;
                    orderedParsers =
                        first.Select(x => parsers.FirstOrDefault(y => string.Equals(x, y.ColumnName, StringComparison.InvariantCultureIgnoreCase)))
                            .ToArray();

                }
                else
                {
                    var empty = new TRow();
                    for (int cc = 0; cc < row.Length; cc++)
                    {
                        if (orderedParsers[cc] != null)
                        {
                            orderedParsers[cc].ParseColumn(empty, row[cc]);    
                        }
                        
                    }
                    
                    yield return empty;
                }
            }
        }


    }
}
