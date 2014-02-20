using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using MathNet.Numerics.Distributions;
using NUnit.Framework;

namespace VARSpike
{
    public class Interpretation
    {
        public string Name { get; set; }
        public Func<double, object> Transform { get; set; }
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
                new MatrixResult(MatrixDefinitionBySet2D<double, string>.Define(
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