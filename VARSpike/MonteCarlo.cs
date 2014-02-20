using System;
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

    

    public class MonteCarlo : Series, IReporter
    {
        public class Params : CalculationParams
        {
            public Params()
            {
                RandomWrapper = new RandomWrapper();
            }
           

            public Normal ReturnsDist { get;  set; }

            public double InitialPrice { get;  set; }

            public int TimeHorizon { get;  set; }

            public int Quality_ScenarioCount { get;  set; }

            public int Quality_IntraDaySteps { get;  set; }

            public List<double> ConfidenceIntervals { get;  set; }

            public ReturnType ReturnsType { get;  set; }

            public RandomWrapper RandomWrapper { get;  set; }
            
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

        public List<double> ResultRanked { get; set; }
        public ValueAtRisk ResultVarCoVar { get; set; }

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
            ResultRanked = ci.Select(x => QuantileFromRankedSeries(this, x) - initialPrice).ToList();
            
            // Var-CoVar Method
            ResultVarCoVar = new ValueAtRisk(new Normal(this.Mean(), this.StandardDeviation()), ci, initialPrice);
            ResultVarCoVar.Compute();

         
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
            var e = stdNormal.InverseCumulativeDistribution(GetRandom(s, t, dt));  
            // var e = stdNormal.Sample();
            return returnsDist.Mean * deltaT + returnsDist.StdDev * e * Math.Sqrt(deltaT);


            // M, S, dt=1, p=RANDOM

            // m = normal.Mean
            // s = normal.StdDev
            // p = inside stdNormal.Sample()
            // XLS NORM.INV(p, m, s) => m + s*NORM.INV(0,1)*p
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
                ReportHelper.ToReport(Histogram),
                new HeadingResult("VaR-VarCoVar"),
                ResultVarCoVar.ToReport(),
                new HeadingResult("VaR-Ranked"),
                new TableResult(ci),
                new TableResult(ResultRanked),
                new VerboseResult(WriteVerbose)
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
