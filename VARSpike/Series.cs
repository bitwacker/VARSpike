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

        public Sample(DateTime at, double value) : this()
        {
            At = at;
            Value = value;
        }

        public DateTime At { get; set; }
        public double Value { get; set; }
    }

    public enum UnitOfMeasure
    {
        ReturnClassic,
        ReturnLog,
        Price
    }

    public interface ISeries : IReporter, IEnumerable<double>
    {
        string Name { get; }

        UnitOfMeasure UnitOfMeasure { get; }
        string UnitOfMeasureDescription { get;  }

        IDistribution Distribution { get; }

        string ToStringSummay();

        string Format { get;  }


    }

    /// <summary>
    /// TimeSeries - must be oldest to newest (effects returns)
    /// </summary>
    public class Series : List<double>, ISeries
    {
        public Series() { }
        public Series(IEnumerable<double> oldestToNewest) : base(oldestToNewest) {}
        public Series(IEnumerable<ISample> sample) : base((IEnumerable<double>) sample.OrderBy(x=>x.At).Select(x=>x.Value)){}
        public Series(int capacity): base(capacity) {}

        public string Name { get; set; }

        public UnitOfMeasure UnitOfMeasure {  get; set; }
        public string UnitOfMeasureDescription { get; set; }

        public string Format { get; set; }

        public IDistribution Distribution
        {
            get
            {
                return new Normal(this.Mean(), this.StandardDeviation());
            }
        }

        public static string ToSummary(ISeries ser)
        {
            var fmt = ser.Format ?? "0.00000";
            return TextHelper.Format("n={4,4} [{3}] {0,8:" + fmt + "}/{1,8:" + fmt + "}/{2,8:" + fmt + "}", 
                Statistics.Minimum(ser), Statistics.Median(ser),
                Statistics.Maximum(ser), ser.Distribution, ser.Count()).ToString();
        }

        public string ToStringSummay()
        {
            return ToSummary(this);
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

    public class TimeSeries : List<ISample>, ISeries
    {
        public TimeSeries()
        {
        }

        public TimeSeries(int capacity) : base(capacity)
        {
        }

        public TimeSeries(IEnumerable<ISample> collection) : base(collection)
        {
        }

        public string ToStringSummay()
        {
            return Series.ToSummary(this);
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

        IEnumerator<double> IEnumerable<double>.GetEnumerator()
        {
            return System.Linq.Enumerable.Select((List<ISample>)this , x => x.Value).GetEnumerator();
        }

        public string Name { get;  set; }
        public UnitOfMeasure UnitOfMeasure { get;  set; }
        public string UnitOfMeasureDescription { get;  set; }
        public IDistribution Distribution
        {
            get
            {
                if (this.Count == 0) return null;
                return new Normal(this.Mean(), this.StandardDeviation());
            }
        }

        public string Format { get; set; }
    }
}