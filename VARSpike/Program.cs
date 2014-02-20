using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra.Generic.Solvers.Status;
using MathNet.Numerics.Statistics;

namespace VARSpike
{
    class Program
    {
        private static StreamWriter FileWriter;

        static void Main(string[] args)
        {
            Reporter.Implementation = ConsoleWriter;
            using (FileWriter = new StreamWriter(File.OpenWrite("VARSpike.txt")))
            {
                //var repMP = new MarketPriceRepository();
                //var prices = new Series(repMP.GetPrices().Select(x => x.Value));
                var prices = new Series(Data.BrentCrude2013);

                Reporter.Write("Prices", prices);



                var cr = Domain.ClassicReturnSeries(prices);
                Reporter.Write("ClassicReturns", cr);
                var crVAR = new ValueAtRisk(new Normal(cr.Mean(), cr.StandardDeviation()), Domain.StandardConfidenceLevels, 0) ;
                crVAR.Compute();
                Reporter.Write("VaR-ClassicReturns", crVAR);

                var lr = Domain.LogReturnSeries(prices);
                Reporter.Write("LogReturns", lr);
                var lrVAR = new ValueAtRisk(new Normal(lr.Mean(), lr.StandardDeviation()), Domain.StandardConfidenceLevels, 0);
                lrVAR.Compute();
                Reporter.Write("VaR-LogReturns", lrVAR);
                

                //var m = new MonteCarlo(new Normal(cr.Mean(), cr.StandardDeviation()), prices.Last(), 10, 100, 1, .95);
                //m.Compute();
                //Reporter.Write("MonteCarlo", m);

                //m = new MonteCarlo(new Normal(cr.Mean(), cr.StandardDeviation()), prices.Last(), 10, 10000, 32, .95);
                //m.Compute();
                //Reporter.Write("MonteCarlo", m);

                using (new CodeTimerConsole("MonteCarlo-CR"))
                {
                    var m = new MonteCarlo(new Normal(cr.Mean(), cr.StandardDeviation()), prices.Last(), 30, 1000, 8, Domain.StandardConfidenceLevels, ReturnType.Classic);
                    m.Compute();
                    Reporter.Write("MonteCarlo", m);    
                }

                using (new CodeTimerConsole("MonteCarlo-LR"))
                {
                    var m = new MonteCarlo(new Normal(lr.Mean(), lr.StandardDeviation()), prices.Last(), 30, 1000, 8, Domain.StandardConfidenceLevels, ReturnType.Log);
                    m.Compute();
                    Reporter.Write("MonteCarlo", m);
                }
                
                


                //m = new MonteCarlo(new Normal(cr.Mean(), cr.StandardDeviation()), prices.Last(), 60, 50000, 8, .95);
                //m.Compute();
                //Reporter.Write("MonteCarlo", m);    
            }
        }

        private static void ConsoleWriter(IResult res)
        {
            var txt = res.ToString();
            Console.WriteLine(txt);
            if (FileWriter != null)
            {
                FileWriter.WriteLine(txt);
            }
        }
    }


    public class Series : List<double>, IReporter
    {
        public Series()
        {
        }

        public Series(IEnumerable<double> collection)
            : base(collection)
        {
        }

        public Series(int capacity)
            : base(capacity)
        {
        }

        public override string ToString()
        {
            //return string.Format("Size: {0}; Avg: {1}; Std.Dev: {2}; (Min/Median/Max: {3}/{4}/{5})", Count, Statistics.Mean(this), Statistics.StandardDeviation(this), Statistics.Minimum(this), Statistics.Median(this), Statistics.Maximum(this));
            return ToReport().ToString();
        }

        public virtual IResult ToReport()
        {
            return new CompountResult()
            {
                new PropertyListResult()
                {
                    {"Size", Count},
                    {"Mean", Statistics.Mean(this)},
                    {"StdDev", Statistics.StandardDeviation(this)},
                    {"Min/Med/Max", string.Format("{0}/{1}/{2}",Statistics.Minimum(this), Statistics.Median(this), Statistics.Maximum(this))}
                },
                new TableResult(this)
            };
        }
    }

    public interface ISample
    {
        double Value { get; set; }
    }

    public class MarketPrice : ISample
    {
        public DateTime Date { get; set; }
        public double Value { get; set; }
    }

    public class MarketPriceRepository
    {
        public List<MarketPrice> GetPrices()
        {
            return SmallDBHelper.ExecuteQuery("Data Source=localhost;Initial Catalog=NimbusT;Integrated Security=SSPI;",
                r =>
                {
                    return new MarketPrice()
                    {
                        Date = r.GetDateTime(0),
                        Value = (double)r.GetDecimal(1)
                    };
                },
                @"SELECT Date, Price  FROM MarketPrice WHERE 
	[Index]=30004 -- Brent DTD
	AND [Type]=100068 -- Closing
	AND [Source]=3 -- Bulldog
	AND [Date]>='2013-1-1' AND [Date]<='2013-12-31'");
        }
    }
}
