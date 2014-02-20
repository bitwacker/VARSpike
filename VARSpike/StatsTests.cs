using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace VARSpike
{
    [TestFixture]
    public class StatsTests
    {
        [Test]
        public void ConfidenceLevels()
        {
            Assert.AreEqual(MathHelper.RoundDecimal(Domain.NormalConfidenceIntervalNegOnly(0.95), 2), -1.96);
        }
    }
}
