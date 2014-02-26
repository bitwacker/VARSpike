using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Generic;
using MathNet.Numerics.Statistics;
using VARSpike.Data;

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
                var prices = FileDataHelper.Load_RiskLearn_SetA().Take(5);

                var sources = prices.Select(x =>
                {
                    return new FinancialSource()
                    {
                        PriceHistory = x,
                        Returns = Domain.LogReturnSeries(x)
                    };
                }).ToList();

                foreach (var src in sources)
                {
                    Reporter.WriteLine("Series: {0,20} PriceCount: {1} ReturnCount: {2} giving {3}", src.Name, src.PriceHistory.Count, src.Returns.Count, src.Returns.Distribution);
                }

                var corrMatrix = DenseMatrix.Create(sources.Count, sources.Count,
                    (r, c) => Correlation.Pearson(sources[r].Returns, sources[c].Returns));
                Reporter.Write("Correlation Matrix", new MathMatrixResults(corrMatrix));

                var stdDevMatrix = DenseMatrix.Create(1, sources.Count, 
                    (r, c) => sources[c].Returns.StandardDeviation() * Math.Sqrt(sources[c].Returns.Count));
                Reporter.Write("StdDev Matrix", new MathMatrixResults(stdDevMatrix));

                var weightings = DenseMatrix.OfArray(new double[,] {{ 150000, 200000, 300000,  150000, 200000 }});
                Reporter.Write("Weightings Matrix", new MathMatrixResults(weightings));

                var port = PortfolioVariance(weightings, stdDevMatrix, corrMatrix);
                Reporter.WriteLine("PortfolioVariance: {0}", TextHelper.ToCell(port));

            }
        }

        private double PortfolioVariance(Matrix<double> weightings, Matrix<double> stdDevMatrix, Matrix<double> corrMatrix)
        {
            // p = w'T.cor.w
            var ws =  DenseMatrix.Create(1, weightings.ColumnCount, (r,c) => (stdDevMatrix[0, c]/Math.Sqrt(250)) * weightings[0,c] * 2.33);
            Reporter.Write("ws", new MathMatrixResults(ws));
            var wsT = ws.Transpose();
            Reporter.Write("wst", new MathMatrixResults(wsT));
            
            return  Math.Sqrt((ws * corrMatrix * wsT)[0,0]);
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