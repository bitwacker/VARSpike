using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
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
                sb.AppendFormat("{0,5}↔{1,5} | {2,5} {3}", TextHelper.ToCell(b.LowerBound), TextHelper.ToCell(b.UpperBound), b.Count, TextHelper.BarChartLine(b.Count, max, 40));
                sb.AppendLine();
            }
            return new StringResult(sb);

        }
    }

    public class MonteCarlo : Series, IReporter
    {
        public MonteCarlo(Normal returnsDist, double priceToday, int timeHorizon, int scenarioCount, int intraDaySteps, double ci)
        {
            this.returnsDist = returnsDist;
            this.priceToday = priceToday;
            this.timeHorizon = timeHorizon;
            this.scenarioCount = scenarioCount;
            this.intraDaySteps = intraDaySteps;
            this.ci = ci;
        }

        // Inputs
        private static readonly Normal stdNormal = new Normal(0,1);
        private readonly Normal returnsDist;
        private readonly Random random = new Random(1);
        private readonly double priceToday;
        private readonly int timeHorizon;
        private readonly int scenarioCount;
        private readonly int intraDaySteps;
        private readonly double ci;

        // Out
        public double ResultRanked { get; set; }
        public Histogram Histogram { get; set; }
        public double ResultVarCoVar { get; set; }

        public void Compute()
        {
            this.Clear();
            for (int s = 0; s < scenarioCount; s++)
            {
                var scenario = new Series();
                var curr = priceToday;
                for (int t = 0; t < timeHorizon; t++)
                {
                    for (int dt = 0; dt < intraDaySteps; dt++)
                    {
                        var nextReturn = GenerateStep(s, t, dt);
                        scenario.Add(nextReturn);

                        curr = curr + curr * nextReturn;    
                    }
                }
                this.Add(curr);
            }
            Sort();


            // Ranked Method
            Histogram = new Histogram(this, 20);
            var priceAtCI = QuantileFromRankedSeries(this, ci);
            ResultRanked = priceAtCI /* should be smaller */ - priceToday;

            // Var-CoVar Method
            ResultVarCoVar = ComputeCoVar();
        }

        private double ComputeCoVar()
        {
            return Domain.VAR(this.Average(), this.StandardDeviation(), ci, 1) - priceToday;
        }


        private double QuantileFromRankedSeries(IEnumerable<double> seriesSorted, double d)
        {
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
                    {"Normal", string.Format("m={0}, s={1}", returnsDist.Mean, returnsDist.StdDev)},
                    {"VaR-Rank (As Price delta)", ResultRanked},
                    {"VaR-VarCoVar (As Price delta)", ResultVarCoVar}
                },
                ReportHelper.ToReport(Histogram)
                //new TableResult(this)
            };
        }
    }
}
