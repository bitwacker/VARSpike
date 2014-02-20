using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.Distributions;

namespace VARSpike
{
    public static class TextHelper
    {
        public static string ToTable(IEnumerable<double> series)
        {
            var sb = new StringBuilder();
            int cc = 0;
            foreach (var d in series)
            {
                sb.Append(ToCell(d));

                cc++;
                if (cc % 10 == 0)
                {
                    sb.AppendLine();
                }
                else
                {
                    sb.Append(" | ");
                }
                
            }
            return sb.ToString();
        }

        public static string ToTable(IEnumerable series)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine();
            int cc = 0;
            foreach (var d in series)
            {
                sb.Append(ToCell(d));

                cc++;
                if (cc % 10 == 0)
                {
                    sb.AppendLine();
                }
                else
                {
                    sb.Append(" | ");
                }

            }
            return sb.ToString();
        }

        public static string ToCell(double d)
        {
            return d.ToString("0.000000").PadLeft(11);
        }

        public  static string ToCell(object d)
        {
            var normal = d as Normal;
            if (normal != null)
            {
                return string.Format("{0}={1} {2}={3}", Domain.Symbol.Mu, ToCell(normal.Mean), Domain.Symbol.Sigma, ToCell(normal.StdDev));
            }
            return d.ToString().PadLeft(11);
        }

        public static string BarChartLine(double count, double max, int size, char on='#', char off=' ')
        {
            var countOn = (int) (count / max * (double) size);
            var countOff = size - countOn;

            var sb = new StringBuilder();
            for (int cc = 0; cc < countOn; cc++) sb.Append(on);
            for (int cc = 0; cc < countOff; cc++) sb.Append(off);
            return sb.ToString();
        }
    }
}