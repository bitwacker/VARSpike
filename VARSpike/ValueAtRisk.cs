using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;
using NUnit.Framework;

namespace VARSpike
{
    public class Interpretation
    {
        public string Name { get; set; }
        public Func<double, object> Transform { get; set; }
    }

    public class ValueAtRiskHistoric : IReporter
    {
        public ValueAtRiskHistoric(List<double> confidenceIntervals, Series priceHistory, Series returnHistory)
        {
            ConfidenceIntervals = confidenceIntervals;
            PriceHistory = priceHistory;
            ReturnHistory = returnHistory;
        }

        public Series PriceHistory { get; protected set; }

        public Series ReturnHistory { get; protected set; }
        public List<double> ConfidenceIntervals { get; private set; }

        // Output
        public List<Tuple<double, double>> PriceResults { get; private set; }
        public List<Tuple<double, double>> ReturnResults { get; private set; }


        public void Compute()
        {
            PriceResults = new List<Tuple<double, double>>();
            ReturnResults = new List<Tuple<double, double>>();
            foreach (var confidenceInterval in ConfidenceIntervals)
            {
                var res = Statistics.Percentile(PriceHistory, (int)((1-confidenceInterval) * 100));
                PriceResults.Add(new Tuple<double, double>(confidenceInterval, res));

                var retres = Statistics.Percentile(ReturnHistory, (int)((1 - confidenceInterval) * 100));
                ReturnResults.Add(new Tuple<double, double>(confidenceInterval, retres));
            }
        }

        public IResult ToReport()
        {
            
            return new CompountResult()
            {
                new UIMatrixResult(UiMatrixDefinitionBySet2D<double, string>.Define(
                    (ci, r) =>
                    {
                        if (r == "Percentile") return TextHelper.ToCell(PriceResults[ConfidenceIntervals.IndexOf(ci)].Item2);

                        if (r == "Returns") return TextHelper.ToCell(ReturnResults[ConfidenceIntervals.IndexOf(ci)].Item2);

                        if (r == "Return Diff")
                        {
                            var latestPricce = PriceHistory.Last();
                            var diff = Domain.LogReturnInv(ReturnResults[ConfidenceIntervals.IndexOf(ci)].Item2, latestPricce) - PriceHistory.Last();
                            return TextHelper.ToCell(diff);
                        }

                        return TextHelper.ToCell(PriceResults[ConfidenceIntervals.IndexOf(ci)].Item2 - PriceHistory.Last());
                    },
                    ConfidenceIntervals,
                    new String[] {"Percentile", "Price Diff", "Returns", "Return Diff"}
                    ))
            };
        }
    }

    public class ValueAtRisk : IReporter
    {
        public ValueAtRisk(Normal distribution, List<double> confidenceIntervals)
        {
            this.ConfidenceIntervals = confidenceIntervals;
            Distribution = distribution;
            DeltaTime = 1;
        }

        public ValueAtRisk(Normal distribution, List<double> confidenceIntervals, double deltaTime)
        {
            Distribution = distribution;
            ConfidenceIntervals = confidenceIntervals;
            DeltaTime = deltaTime;
        }

        // Input
        public double DeltaTime { get; set; }
        public List<Interpretation> Interpretations { get; set; } 
        
        public Normal Distribution { get; private set; }
        public List<double> ConfidenceIntervals { get; private set; }

        // Output
        public List<Tuple<double, double>> Results { get; private set; }
        

        public void Compute()
        {
            Results = new List<Tuple<double, double>>();
            foreach (var confidenceInterval in ConfidenceIntervals)
            {
                var res =  Calculate(Distribution.Mean, Distribution.StdDev, confidenceInterval, DeltaTime);
                Results.Add(new Tuple<double, double>(confidenceInterval, res));
            }
        }

        public static double Calculate(double mean, double stddev, double ci, double deltaTime)
        {
            return mean * deltaTime + stddev * Domain.NormalConfidenceIntervalNegOnly(ci) * Math.Sqrt(deltaTime);
        }


        public IResult ToReport()
        {
            var resultRows = new List<string>();
            resultRows.Add("VaR");
            if (Interpretations != null)
            {
                Interpretations.ForEach(c=>resultRows.Add(c.Name));
            }
            return new CompountResult()
            {
                new PropertyListResult()
                {
                    {"Dist", Distribution},
                    {"DeltaTime", DeltaTime},
                },
                new UIMatrixResult(UiMatrixDefinitionBySet2D<double, string>.Define(
                    (ci, r) =>
                    {
                        if (r == "VaR") return TextHelper.ToCell(Results[ConfidenceIntervals.IndexOf(ci)].Item2);

                        var match = Interpretations.Find(x => x.Name == r);
                        if (match != null)
                        {
                            return TextHelper.ToCell(match.Transform(Results[ConfidenceIntervals.IndexOf(ci)].Item2));    
                        }

                        return "N/A";
                    },
                    ConfidenceIntervals,
                    resultRows
                    ))
            };
        }
    }
}