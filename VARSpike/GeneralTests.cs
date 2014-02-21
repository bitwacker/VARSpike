using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Double;
using NUnit.Framework;
using VARSpike.Data;
using VARSpike.FileFormats;

namespace VARSpike
{
    [TestFixture]
    public class GeneralTests
    {
        [Test]
        public void ConfidenceLevels()
        {
            Assert.AreEqual(MathHelper.RoundDecimal(Domain.NormalConfidenceIntervalNegOnly(0.95), 2), -1.96);
        }

        [Test]
        public void ExcelPrecision()
        {
            //   12345678901234567
            // 0.248668584157093
            // 0.24866858415709278
            Assert.AreEqual(0.248668584157093, ExcelHelper.ToExcelPrecision(0.24866858415709278));
        }

        [Test]
        public void MatrixTests()
        {
            var set = new List<int>()
            {
                3,
                2,
                4
            };


            var res = UIMatrixResult.TreeProduct(set);
            foreach (var pair in res)
            {
                Console.WriteLine(pair);
            }


        }

        [Test]
        public void Matrix()
        {
            var m = DenseMatrix.Create(4, 3, (r, c) => r * 3 + (c+1));
            Console.WriteLine(m);

            var r1 = DenseMatrix.OfArray(new double[,]
            {
                {2,2,2,2},
            });
            Console.WriteLine(r1 * m);
        }

        [Test]
        public void TestCSV_Series()
        {
            var res = FileDataHelper.Load_RiskLearn_SetA();
        }

        [Test]
        public void TestCSV_Direct()
        {
            using (var file = new StreamReader("./Data/Data---RiskLearn--Analytical-VaR.csv"))
            {
                var xxx = FileImportCSV.Parse<Sample>(file, 
                    new FileImportCSV.ColumnParser<Sample>()
                    {
                        ColumnName = "Dates",
                        ParseColumn = (data, cell) => data.At = DateTime.Parse(cell)
                    },
                    new FileImportCSV.ColumnParser<Sample>()
                    {
                        ColumnName = "IBM",
                        ParseColumn = (data, cell) => data.Value = double.Parse(cell)
                    }
                );
                
                Assert.AreEqual(251, xxx.Count());
            }
            
        }
    }
}
