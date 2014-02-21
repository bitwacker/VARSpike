using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;

namespace VARSpike
{

    public interface ISample
    {
        DateTime At { get; set; }
        double Value { get; set; }
    }

    public struct Sample : ISample
    {
        public DateTime At { get; set; }
        public double Value { get; set; }
    }

    public enum UnitOfMeasure
    {
        ReturnClassic,
        ReturnLog,
        Price
    }

    /// <summary>
    /// TimeSeries - must be oldest to newest (effects returns)
    /// </summary>
    public class Series : List<double>, IReporter
    {
        public Series() { }
        public Series(IEnumerable<double> oldestToNewest) : base(oldestToNewest) {}
        public Series(IEnumerable<ISample> sample) : base((IEnumerable<double>) sample.OrderBy(x=>x.At).Select(x=>x.Value)){}
        public Series(int capacity): base(capacity) {}

        public string Name { get; set; }

        public UnitOfMeasure UnitOfMeasure {  get; set; }
        public string UnitOfMeasureDescription { get; set; }

        public Normal NormalDistribution
        {
            get
            {
                return new Normal(this.Mean(), this.StandardDeviation());
            }
        }

        public override string ToString()
        {
            //return string.Format("Size: {0}; Avg: {1}; Std.Dev: {2}; (Min/Median/Max: {3}/{4}/{5})", Count, Statistics.Mean(this), Statistics.StandardDeviation(this), Statistics.Minimum(this), Statistics.Median(this), Statistics.Maximum(this));
            return ToReport().ToString();
        }

        public virtual IResult ToReport()
        {
            return new CompountResult()
            {
                new PropertyListResult()
                {
                    {"Name", Name},
                    {"UnitOfMeasure", UnitOfMeasure},
                    {"UnitOfMeasure Desc", UnitOfMeasureDescription},
                    {"Size", Count},
                    {"Mean",  Statistics.Mean(this)},
                    {"StdDev", Statistics.StandardDeviation(this)},
                    {"Min/Med/Max", TextHelper.Format("{0} / {1} / {2}", Statistics.Minimum(this), Statistics.Median(this), Statistics.Maximum(this))}
                },
                new TableResult(this)
            };
        }
    }

    public class TimeSeries : List<ISample>, IEnumerable<double>, IReporter
    {
        public IResult ToReport()
        {
            throw new NotImplementedException();
        }

        public IEnumerator<double> GetEnumerator()
        {
            return System.Linq.Enumerable.Select((List<ISample>)this , x => x.Value).GetEnumerator();
        }
    }
}