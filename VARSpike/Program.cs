using System;
using System.Collections.Generic;
using System.Dynamic;
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
        

        static void Main(string[] args)
        {
            
            using (Reporter.HtmlOutput("VARSpike.html"))
            {
                
                //var repMP = new MarketPriceRepository();
                //var prices = new Series(repMP.GetPrices().Select(x => x.Value));
                var prices = new Series(Data.BrentCrude2013);

                //Reporter.Write("Prices", prices);

                var cr = Domain.ClassicReturnSeries(prices);
                //Reporter.Write("ClassicReturns", cr);
                //var crVAR = new ValueAtRisk(new Normal(cr.Mean(), cr.StandardDeviation()), Domain.StandardConfidenceLevels, 0) ;
                //crVAR.Compute();
                //Reporter.Write("VaR-ClassicReturns", crVAR);

                var lr = Domain.LogReturnSeries(prices);
                //Reporter.Write("LogReturns", lr);
                //var lrVAR = new ValueAtRisk(new Normal(lr.Mean(), lr.StandardDeviation()), Domain.StandardConfidenceLevels, 0);
                //lrVAR.Compute();
                //Reporter.Write("VaR-LogReturns", lrVAR);


                
                var options1 = new MonteCarlo.Params()
                {
                    Name = "MonteCarlo-using-ClassicReturns",

                    ReturnsType = ReturnType.Classic,
                    ReturnsDist = new Normal(cr.Mean(), cr.StandardDeviation()),
                    InitialPrice = prices.Last(),
                    TimeHorizon = 10,
                    ConfidenceIntervals = Domain.StandardConfidenceLevels,

                    // Quality
                    Quality_IntraDaySteps = 8,
                    Quality_ScenarioCount = 1000,

                    // RandomWrapper = new RandomWrapper().InitRecord(1, 10*8*1000)

                };

                var m1 = new MonteCarlo(options1);
                m1.Compute();

                Reporter.Write(m1);
                

                var options2 = new MonteCarlo.Params()
                {
                    Name = "MonteCarlo-using-LogReturns",

                    ReturnsType = ReturnType.Classic,
                    ReturnsDist = new Normal(lr.Mean(), lr.StandardDeviation()),
                    InitialPrice = prices.Last(),
                    TimeHorizon = 10,
                    ConfidenceIntervals = Domain.StandardConfidenceLevels,

                    // Quality
                    Quality_IntraDaySteps = 8,
                    Quality_ScenarioCount = 1000,

                    //RandomWrapper = new RandomWrapper().InitRecord(1, 10*8*1000)

                };

                var m2 = new MonteCarlo(options2);
                m2.Compute();

                Reporter.Write(m2);
                

                Reporter.Write(new MatrixResult(MatrixDefinitionBySet3D<double, string, string>.Define(
                    (ci, method, type) =>
                    {
                        return "";
                    },
                    options1.ConfidenceIntervals,
                    new String[] { "ClassicReturn", "LogReturn" },
                    new String[] { "Percentile", "VaR" }
                    )));


            }
        }

       
    }

    public interface ISample
    {
        DateTime At { get; set; }
        double Value { get; set; }
    }

    public struct Sample : ISample
    {
        public DateTime At { get; set; }
        public double Value { get; set; }
    }

    /// <summary>
    /// TimeSeries - must be oldest to newest (effects returns)
    /// </summary>
    public class Series : List<double>, IReporter
    {
        public Series() { }
        public Series(IEnumerable<double> oldestToNewest) : base(oldestToNewest) {}
        public Series(IEnumerable<ISample> sample) : base(sample.OrderBy(x=>x.At).Select(x=>x.Value)){}
        public Series(int capacity): base(capacity) {}

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
                    {"Mean",  Statistics.Mean(this)},
                    {"StdDev", Statistics.StandardDeviation(this)},
                    {"Min/Med/Max", TextHelper.Format("{0}/{1}/{2}", Statistics.Minimum(this), Statistics.Median(this), Statistics.Maximum(this))}
                },
                new TableResult(this)
            };
        }
    }

    
    
}
