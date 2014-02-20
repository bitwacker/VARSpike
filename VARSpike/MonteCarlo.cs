using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;
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
            for (int cc = 0; cc < hist.BucketCount; cc++)
            {
                var b = hist[cc];
                sb.AppendFormat("{0,5} ⇔ {1,5} | {2,5} {3}", TextHelper.ToCell(b.LowerBound), TextHelper.ToCell(b.UpperBound), b.Count, TextHelper.BarChartLine(b.Count, max, 40));
                sb.AppendLine();
            }
            return new StringResult(sb);

        }
    }

    public enum ReturnType
    {
        Classic,
        Log
    }

    public class MonteCarlo : Series, IReporter
    {
        public MonteCarlo(Normal returnsDist, double initialPrice, int timeHorizon, int scenarioCount, int intraDaySteps, List<double> ci, ReturnType returnType)
        {
            this.returnsDist = returnsDist;
            this.initialPrice = initialPrice;
            this.timeHorizon = timeHorizon;
            this.scenarioCount = scenarioCount;
            this.intraDaySteps = intraDaySteps;
            this.ci = ci;
            this.returnType = returnType;
        }

        // Inputs
        private static readonly Normal stdNormal = new Normal(0,1);
        private readonly Normal returnsDist;
        private readonly Random random = new Random(1);
        private readonly double initialPrice;
        private readonly int timeHorizon;
        private readonly int scenarioCount;
        private readonly int intraDaySteps;
        private readonly List<double> ci;
        private readonly ReturnType returnType;

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
            double count = seriesSorted.Count();
            var indx = (int)Math.Floor(count * (1-ci));
            return seriesSorted.Skip(indx).FirstOrDefault();
        }

        private double GenerateStep(int s, int t, int dt)
        {
            var deltaT = 1 / (double)intraDaySteps;
            var e = stdNormal.InverseCumulativeDistribution(random.NextDouble());  
            // var e = stdNormal.Sample();
            return returnsDist.Mean * deltaT + returnsDist.StdDev * e * Math.Sqrt(deltaT);


            // M, S, dt=1, p=RANDOM

            // m = normal.Mean
            // s = normal.StdDev
            // p = inside stdNormal.Sample()
            // XLS NORM.INV(p, m, s) => m + s*NORM.INV(0,1)*p
        }

        public override string ToString()
        {
            return ToReport().ToString();
        }

        public override IResult ToReport()
        {
            return new CompountResult()
            {
                new PropertyListResult()
                {
                    {"TimeHorizon", timeHorizon},
                    {"IntraDaySteps", intraDaySteps},
                    {"ScenarioCount", scenarioCount},
                    {"ReturnType", returnType},
                    {"Normal", TextHelper.ToCell(returnsDist)},
                   
                },
                ReportHelper.ToReport(Histogram),
                new HeadingResult("VaR-VarCoVar"),
                ResultVarCoVar.ToReport(),
                new HeadingResult("VaR-Ranked"),
                new TableResult(ci),
                new TableResult(ResultRanked),
                //new TableResult(this)
            };
        }
    }
}
