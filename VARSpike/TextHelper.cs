using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using MathNet.Numerics.Distributions;

namespace VARSpike
{
    public static class TextHelper
    {

        public static class Symbol
        {

            public const string Mu = "μ"; public const string Mu_Html = "&#956;";

            public const string Sigma = "σ"; public const string Sigma_Html = "&#963;";

            public const string ArrowDouble = "⇔"; public const string ArrowDouble_Html = "&#8660;";


        }

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

        public  static string ToCell(object d, ReportFormat fmt = ReportFormat.Console)
        {
            

            var normal = d as Normal;
            if (normal != null)
            {
                if (fmt == ReportFormat.Html)
                {
                    return string.Format("Norm[{0}={1}, {2}={3}]", Symbol.Mu_Html, ToCell(normal.Mean), Symbol.Sigma_Html, ToCell(normal.StdDev));
                }
                else
                {
                    return string.Format("Norm[{0}={1}, {2}={3}]", Symbol.Mu, ToCell(normal.Mean), Symbol.Sigma, ToCell(normal.StdDev));    
                }
                
            }
            if (d is double)
            {
                return ToCell((double) d);
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

        public static object Format(string format, params object[] args)
        {
            if (format == null) return null;
            var toCell = args.Select(x=>ToCell(x)).ToArray();
            return string.Format(format, toCell);
        }
    }
}