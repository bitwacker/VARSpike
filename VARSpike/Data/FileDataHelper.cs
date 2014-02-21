using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using VARSpike.FileFormats;

namespace VARSpike.Data
{
    public static  class  FileDataHelper
    {
        public static List<Series> Load_RiskLearn_SetA()
        {
            using (var file = new StreamReader("./Data/Data---RiskLearn--Analytical-VaR.csv"))
            {
                return Load_CVS_FirstColDate_ThenAllOthersSeries(file);
            }
        }

        private static List<Series> Load_CVS_FirstColDate_ThenAllOthersSeries(StreamReader reader)
        {
            List<Series> results = null;

            string[] first = null;
            foreach (var row in FileImportCSV.GetRows(reader))
            {
                if (first == null)
                {
                    first = row;
                    results = first.Skip(1).Select(x => new Series()
                    {
                        Name = x
                    }).ToList();
                }
                else
                {
                    var date = DateTime.Parse(row[0]);
                    for (int cc = 1; cc < row.Length; cc++)
                    {
                        results[cc-1].Add(double.Parse(row[cc]));
                    }
                }
            }

            return results;
        }
    }
}
