using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using MathNet.Numerics;
using MathNet.Numerics.Statistics;

namespace VARSpike
{
    public class FinancialSource
    {
        public string Name
        {
            get { return PriceHistory.Name; }
        }

        public Series PriceHistory { get; set; }

        public Series Returns { get; set; }
    }

    public class CalculationCorrelation
    {
        public void Calculate(string[] args)
        {
            var prices = new Series[]
            {
                Data.SharesApple,
                Data.SharesMicrosoft,
                Data.SharesGoogle,
                Data.SharesOracle,
            };

            var sources = prices.Select(x =>
            {
                return new FinancialSource()
                {
                    PriceHistory = x,
                    Returns = Domain.LogReturnSeries(x)
                };
            }).ToList();

            foreach (var pair in SetPermutation(sources))
            {
                var cor = Correlation.Pearson(pair.Item1.Returns, pair.Item2.Returns);

                Reporter.WriteLine("Correlation {0,10} <-> {1,10} = {2}", pair.Item1.Name, pair.Item2.Name,
                    TextHelper.ToCell(cor));

            }

            foreach (var pair in SetPermutation(sources))
            {
                
                var cov = Statistics.Covariance(pair.Item1.Returns, pair.Item2.Returns);
                Reporter.WriteLine("CoVariance  {0,10} <-> {1,10} = {2}", pair.Item1.Name, pair.Item2.Name,
                    TextHelper.ToCell(cov));
            }
        }

        private IEnumerable<Tuple<T,T>> SetPermutation<T>(IEnumerable<T> sets)
        {
            foreach (var a in sets)
            {
                foreach (var b in sets)
                {
                    yield return new Tuple<T, T>(a,b);
                }   
            }
        }
    }
}