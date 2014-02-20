using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra.Generic.Solvers.Status;
using MathNet.Numerics.Statistics;

namespace VARSpike
{
    class Program
    {
        public class MonteCarloResult
        {
            public MonteCarlo.Params Params { get; set; }

            public MonteCarlo MonteCarlo { get; set; }

        }

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


                var runPack = new List<MonteCarloResult>()
                {
                    new MonteCarloResult()
                    {
                        Params =  new MonteCarlo.Params()
                        {
                            Name = "t10 dt8 s1000",
                            ReturnsType = ReturnType.Log,
                            ReturnsDist = new Normal(ExcelHelper.ToExcelPrecision(lr.Mean()), ExcelHelper.ToExcelPrecision(lr.StandardDeviation())),
                            InitialPrice = prices.Last(),
                            TimeHorizon = 10,
                            ConfidenceIntervals = Domain.StandardConfidenceLevels,

                            // Quality
                            Quality_IntraDaySteps = 8,
                            Quality_ScenarioCount = 1000,
                        },
                    },
                    new MonteCarloResult()
                    {
                        Params = new MonteCarlo.Params()
                        {
                            Name = "t10 dt8 s1000",
                            ReturnsType = ReturnType.Classic,
                            ReturnsDist = new Normal(ExcelHelper.ToExcelPrecision(cr.Mean()), ExcelHelper.ToExcelPrecision(cr.StandardDeviation())),
                            InitialPrice = prices.Last(),
                            TimeHorizon = 10,
                            ConfidenceIntervals = Domain.StandardConfidenceLevels,

                            // Quality
                            Quality_IntraDaySteps = 8,
                            Quality_ScenarioCount = 1000
                        }
                    },

                    new MonteCarloResult()
                    {
                        Params =  new MonteCarlo.Params()
                        {
                            Name = "t10 dt8 s10000",
                            ReturnsType = ReturnType.Log,
                            ReturnsDist = new Normal(ExcelHelper.ToExcelPrecision(lr.Mean()), ExcelHelper.ToExcelPrecision(lr.StandardDeviation())),
                            InitialPrice = prices.Last(),
                            TimeHorizon = 10,
                            ConfidenceIntervals = Domain.StandardConfidenceLevels,

                            // Quality
                            Quality_IntraDaySteps = 8,
                            Quality_ScenarioCount = 10000,
                        },
                    },
                    new MonteCarloResult()
                    {
                        Params = new MonteCarlo.Params()
                        {
                            Name = "t10 dt8 s10000",
                            ReturnsType = ReturnType.Classic,
                            ReturnsDist = new Normal(ExcelHelper.ToExcelPrecision(cr.Mean()), ExcelHelper.ToExcelPrecision(cr.StandardDeviation())),
                            InitialPrice = prices.Last(),
                            TimeHorizon = 10,
                            ConfidenceIntervals = Domain.StandardConfidenceLevels,

                            // Quality
                            Quality_IntraDaySteps = 8,
                            Quality_ScenarioCount = 10000
                        }
                    },

                    new MonteCarloResult()
                    {
                        Params =  new MonteCarlo.Params()
                        {
                            Name = "t10 dt32 s100000",
                            ReturnsType = ReturnType.Log,
                            ReturnsDist = new Normal(ExcelHelper.ToExcelPrecision(lr.Mean()), ExcelHelper.ToExcelPrecision(lr.StandardDeviation())),
                            InitialPrice = prices.Last(),
                            TimeHorizon = 32,
                            ConfidenceIntervals = Domain.StandardConfidenceLevels,

                            // Quality
                            Quality_IntraDaySteps = 32,
                            Quality_ScenarioCount = 100000,
                        },
                    },
                    new MonteCarloResult()
                    {
                        Params = new MonteCarlo.Params()
                        {
                            Name = "t10 dt32 s100000",
                            ReturnsType = ReturnType.Classic,
                            ReturnsDist = new Normal(ExcelHelper.ToExcelPrecision(cr.Mean()), ExcelHelper.ToExcelPrecision(cr.StandardDeviation())),
                            InitialPrice = prices.Last(),
                            TimeHorizon = 10,
                            ConfidenceIntervals = Domain.StandardConfidenceLevels,

                            // Quality
                            Quality_IntraDaySteps = 32,
                            Quality_ScenarioCount = 100000
                        }
                    },
                };


                foreach (var item in runPack)
                {
                    using (var timer = new CodeTimerConsole(item.Params.Name))
                    {
                        item.MonteCarlo = new MonteCarlo(item.Params);
                        item.MonteCarlo.Compute();    
                    }
                }

                Reporter.Write(new HeadingResult("COMPARISON"));

                Reporter.Write(new MatrixResult(MatrixDefinitionBySet4D<double, string, ReturnType, VaRMethod>.Define(
                    (ci, name, method, type) =>
                    {
                        var calcWrap = runPack.FirstOrDefault(x=>x.Params.Name == name && x.Params.ReturnsType == method);
                        if (calcWrap == null) return "(null)";
                        var calc = calcWrap.MonteCarlo;
                        if (type == VaRMethod.Normal)
                        {
                            return TextHelper.ToCell(calc.ResultVarCoVar.Results[calc.Parameters.ConfidenceIntervals.IndexOf(ci)].Item2);
                        }
                        else // Percentile
                        {
                            return TextHelper.ToCell(calc.ResultPercentile[calc.Parameters.ConfidenceIntervals.IndexOf(ci)]);
                        }
                    },
                    Domain.StandardConfidenceLevels,
                    runPack.Select(x=>x.Params.Name).Distinct(),
                    new ReturnType[] { ReturnType.Classic, ReturnType.Log },
                    new VaRMethod[] { VaRMethod.Percentile, VaRMethod.Normal }
                    
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

        public Normal NormalDistribution
        {
            get
            {
                return new Normal(this.Mean(), this.StandardDeviation());
            }
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
                    {"Mean",  Statistics.Mean(this)},
                    {"StdDev", Statistics.StandardDeviation(this)},
                    {"Min/Med/Max", TextHelper.Format("{0}/{1}/{2}", Statistics.Minimum(this), Statistics.Median(this), Statistics.Maximum(this))}
                },
                new TableResult(this)
            };
        }
    }

    
    
}
