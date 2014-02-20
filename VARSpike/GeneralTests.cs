using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

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


            var res = MatrixResult.TreeProduct(set);
            foreach (var pair in res)
            {
                Console.WriteLine(pair);
            }


        }
    }
}
