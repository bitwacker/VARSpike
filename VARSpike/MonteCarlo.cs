using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Schema;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;

namespace VARSpike
{
    public class CalculationParams
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class ExcelHelper
    {
        public static double ToExcelPrecision(double d)
        {
            return MathHelper.RoundDecimal(d, 15);
        }
    }

    public enum VaRMethod
    {
        Percentile,
        VarCoVar
    }
    

    public class MonteCarlo : Series, IReporter
    {
        

        public class Params : CalculationParams
        {
            public Params()
            {
                RandomWrapper = new RandomWrapper();
                UseExcel = true;
            }
           

            public Normal ReturnsDist { get;  set; }

            public double InitialPrice { get;  set; }

            public int TimeHorizon { get;  set; }

            public int Quality_ScenarioCount { get;  set; }

            public int Quality_IntraDaySteps { get;  set; }

            public List<double> ConfidenceIntervals { get;  set; }

            public ReturnType ReturnsType { get;  set; }

            public RandomWrapper RandomWrapper { get;  set; }

            public bool UseExcel { get; set; }
            
        }

        public MonteCarlo(Params @params)
        {
            this.returnsDist = @params.ReturnsDist;
            this.initialPrice = @params.InitialPrice;
            this.timeHorizon = @params.TimeHorizon;
            this.scenarioCount = @params.Quality_ScenarioCount;
            this.intraDaySteps = @params.Quality_IntraDaySteps;
            this.ci = @params.ConfidenceIntervals;
            this.returnType = @params.ReturnsType;
            this.random = @params.RandomWrapper;
            Parameters = @params;
        }

        

        // Inputs
        private static readonly Normal stdNormal = new Normal(0,1);
        private readonly Normal returnsDist;
        private readonly RandomWrapper random;
        private readonly double initialPrice;
        private readonly int timeHorizon;
        private readonly int scenarioCount;
        private readonly int intraDaySteps;
        private readonly List<double> ci;
        private readonly ReturnType returnType;
        public Params Parameters { get; set; }

        // Out
        public Histogram Histogram { get; set; }

        public List<double> ResultPercentile { get; set; }
        public ValueAtRisk ResultVarCoVar { get; set; }
        //public ValueAtRisk ResultVarCoVarReturns { get; set; }
        //public List<double> ResultVarCoVarReturnsPrice { get; set; }

        public void Compute()
        {
         
            this.Clear();
            for (int s = 0; s < scenarioCount; s++)
            {
                var scenario = new Series();
                var curr = initialPrice;
                for (int t = 0; t < timeHorizon; t++)
                {
                    for (int dt = 0; dt < intraDaySteps; dt++)
                    {
                        var nextReturn = GenerateStep(s, t, dt);
                        scenario.Add(nextReturn);

                        if (returnType == ReturnType.Classic)
                        {
                            curr = curr + curr * nextReturn;        
                        }
                        else if (returnType == ReturnType.Log)
                        {
                            curr = curr * Math.Exp(nextReturn); 
                        }
                        
                    }
                }
                this.Add(curr);
            }
            Sort();


            // Ranked Method
            Histogram = new Histogram(this, 20);
            ResultPercentile = ci.Select(x => QuantileFromRankedSeries(this, x) - initialPrice).ToList();
            
            // Var-CoVar Method
            ResultVarCoVar = new ValueAtRisk(this.NormalDistribution, ci)
            {
                Interpretations = new List<Interpretation>()
                {
                    new Interpretation()
                    {
                        Name = "VaR delta(Price)",
                        Transform = (var) => var - initialPrice
                    }
                }
            };
            
            ResultVarCoVar.Compute();

            //var walkReturns = Domain.LogReturnSeries(this);
            //ResultVarCoVarReturns = new ValueAtRisk(walkReturns.NormalDistribution, ci, 0);
            //ResultVarCoVarReturns.Compute();

            //ResultVarCoVarReturnsPrice = ResultVarCoVarReturns.Results.Select(x => Domain.LogReturnInv(x.Item2, initialPrice) - initialPrice).ToList();

        }

        


        /// <summary>
        /// http://en.wikipedia.org/wiki/Percentile
        /// </summary>
        private double QuantileFromRankedSeries(IEnumerable<double> seriesSorted, double ci)
        {
            //var q = Statistics.QuantileCustom(seriesSorted, ci, QuantileDefinition.Excel);

            return Statistics.Percentile(seriesSorted, (int) ((1-ci) * 100));

            // A HACK Method:
            //double count = seriesSorted.Count();
            //var indx = (int)Math.Floor(count * (1-ci));
            //return seriesSorted.Skip(indx).FirstOrDefault();
        }

        private double GenerateStep(int s, int t, int dt)
        {
            var deltaT = 1 / (double)intraDaySteps;
            var rnd = ExcelHelper.ToExcelPrecision(GetRandom(s, t, dt));
            var e = ExcelHelper.ToExcelPrecision(stdNormal.InverseCumulativeDistribution(rnd));  
            
            var result = ExcelHelper.ToExcelPrecision(returnsDist.Mean * deltaT + returnsDist.StdDev * e * Math.Sqrt(deltaT));

            return result;
        }
        
        private double GetRandom(int s, int t, int dt)
        {
            if (random.Record)
            {
                return random.NextDouble(string.Format("s{0}t{1}dt{2}", s, t, dt));
            }
            else
            {
                return random.NextDouble();
            }
        }

        public override string ToString()
        {
            return ToReport().ToString();
        }

        public override IResult ToReport()
        {
            var resultMatrix = UiMatrixDefinitionBySet2D<double, string>.Define(
                (ci, s) =>
                {
                    switch (s)
                    {
                        case("Percentile") :
                            return TextHelper.ToCell(
                                 ResultPercentile[Parameters.ConfidenceIntervals.IndexOf(ci)]
                                 );

                        case ("VaR-OnPrice"):
                            return TextHelper.ToCell(
                                 ResultVarCoVar.Results[Parameters.ConfidenceIntervals.IndexOf(ci)].Item2 - initialPrice
                                 );
                            

                        //case ("VaR-Returns"):
                        //    return ResultVarCoVarReturns.Results[Parameters.ConfidenceIntervals.IndexOf(ci)].Item2.ToString();

                        //case ("VaR-ReturnsPrice"):
                        //    return ResultVarCoVarReturnsPrice[Parameters.ConfidenceIntervals.IndexOf(ci)].ToString();

                        default:
                            throw new Exception();
                    }
                    
                },
                Parameters.ConfidenceIntervals,
                new String[] { "Percentile", "VaR-OnPrice" }
                );

            return new CompountResult()
            {
                new HeadingResult(Parameters.Name ?? "MonteCarlo"),
                new PropertyListResult()
                {
                    {"TimeHorizon", timeHorizon},
                    {"IntraDaySteps", intraDaySteps},
                    {"ScenarioCount", scenarioCount},
                    {"ReturnType", returnType},
                    {"Normal", returnsDist},
                   
                },
                new HeadingResult("Scenarios (ordered random-walk) results"),
                base.ToReport(),
                ReportHelper.ToReport(Histogram),
                new VerboseResult(WriteVerbose),
                new HeadingResult("Results"),
                new UIMatrixResult()
                {
                    UiMatrix = resultMatrix
                },
            };
        }

        private void WriteVerbose(ReportFormat fmt, TextWriter outp)
        {
            if (random.PlayBack)
            {
                var filename = string.Format("{0}-randomseed.csv", this.Parameters.Name);
                outp.WriteLine("[VERBOSE] file: ./{0}", filename);

                if (random.Record)
                {
                    random.SaveFixed(filename);
                }
            }
            
        }
    }
}
