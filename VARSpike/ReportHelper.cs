using System.Text;
using MathNet.Numerics.Statistics;

namespace VARSpike
{
    public static class ReportHelper
    {
        public static IResult ToReport(Histogram hist)
        {
            double max = 0;
            for (int cc = 0; cc < hist.BucketCount; cc++)
            {
                var b = hist[cc];
                if (b.Count > max) max = b.Count;
            }

            var sb = new StringBuilder();
            var html = new StringBuilder();
            for (int cc = 0; cc < hist.BucketCount; cc++)
            {
                var b = hist[cc];


                sb.AppendFormat("{0,5} {4} {1,5} | {2,5} {3}", 
                    TextHelper.ToCell(b.LowerBound), 
                    TextHelper.ToCell(b.UpperBound), b.Count, 
                    TextHelper.BarChartLine(b.Count, max, 40),
                    TextHelper.Symbol.ArrowDouble);
                sb.AppendLine();

                html.AppendFormat("{0,5} {4} {1,5} | {2,5} {3}",
                    TextHelper.ToCell(b.LowerBound),
                    TextHelper.ToCell(b.UpperBound), b.Count,
                    TextHelper.BarChartLine(b.Count, max, 40),
                    TextHelper.Symbol.ArrowDouble_Html);
                html.AppendLine();

                
            }
            return new StringResult(sb.ToString(), html.ToString());

        }
    }
}