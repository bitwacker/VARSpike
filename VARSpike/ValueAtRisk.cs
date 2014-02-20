using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using MathNet.Numerics.Distributions;
using NUnit.Framework;

namespace VARSpike
{
    public class ValueAtRisk : IReporter
    {
        public ValueAtRisk(Normal distribution, List<double> confidenceIntervals, double initialPrice)
        {
            this.confidenceIntervals = confidenceIntervals;
            Distribution = distribution;
            InitialPrice = initialPrice;
        }

        // Input
        public double InitialPrice { get; private set; }
        public Normal Distribution { get; private set; }
        public List<double> confidenceIntervals { get; private set; }

        // Output
        public List<Tuple<double, double>> Results { get; private set; }


        public void Compute()
        {
            Results = new List<Tuple<double, double>>();
            foreach (var confidenceInterval in confidenceIntervals)
            {
                var res =  Calculate(Distribution.Mean, Distribution.StdDev, confidenceInterval, 1) - InitialPrice;
                Results.Add(new Tuple<double, double>(confidenceInterval, res));
            }
        }

        public static double Calculate(double mean, double stddev, double ci, double deltaTime)
        {
            return mean * deltaTime + stddev * Domain.NormalConfidenceIntervalNegOnly(ci) * Math.Sqrt(deltaTime);
        }


        public IResult ToReport()
        {
            return new CompountResult()
            {
                new PropertyListResult()
                {
                    {"Dist", Distribution}
                },
                new TableResult(Results.Select(x => x.Item1)),
                new TableResult(Results.Select(x => x.Item2))
            };
        }
    }
}