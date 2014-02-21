using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Generic;
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
            using (Reporter.HtmlOutput("CalculationCorrelation.html"))
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

                var corrMatrix = DenseMatrix.Create(sources.Count, sources.Count,
                    (r, c) => Correlation.Pearson(sources[r].Returns, sources[c].Returns));
                Reporter.Write("Correlation Matrix", new MathMatrixResults(corrMatrix));

                var stdDevMatrix = DenseMatrix.Create(1, sources.Count, (r, c) => sources[c].Returns.StandardDeviation() * Math.Sqrt(sources[c].Returns.Count));
                Reporter.Write("StdDev Matrix", new MathMatrixResults(stdDevMatrix));

                var weightings = DenseMatrix.OfArray(new double[,] {{0.1, 0.2, 0.3, 0.4}});
                Reporter.Write("Weightings Matrix", new MathMatrixResults(weightings));

                var port = PortfolioVariance(weightings, stdDevMatrix, corrMatrix);
                Reporter.WriteLine("PortfolioVariance: {0}", TextHelper.ToCell(port));

            }
        }

        private double PortfolioVariance(Matrix<double> weightings, Matrix<double> stdDevMatrix, Matrix<double> corrMatrix)
        {
            var ws =  DenseMatrix.Create(1, weightings.ColumnCount, (r,c) => weightings[0, c] * stdDevMatrix[0, c]);
            var wsT = ws.Transpose();
            return  (ws * corrMatrix * wsT)[0,0];
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