using System.Collections.Generic;
using System.Linq;

namespace VARSpike
{
    internal class IndexAnalysis
    {
        public void Calculate(string[] args)
        {
            var seg = new MarketSegment();
            seg.FromSQL(SmallDBHelper.BuildConnectionString("NimbusT"));

            foreach (var index in seg)
            {
                foreach (var year in index)
                {
                    Reporter.WriteLine("{0,-40} Y{1} => {2}", index.Name,  year.Year, year.Prices.ToStringSummay());
                }
            }
            Reporter.WriteLine("========================================");
            foreach (var index in seg)
            {
                foreach (var year in index)
                {
                    Reporter.WriteLine("{0,-40} Y{1} => {2}", index.Name, year.Year, year.Returns.ToStringSummay());
                }
            }

        }
    }

    public class MarketSegment : List<Index>, IReporter
    {
        public void FromSQL(string db)
        {
            var sqlIndexsToAnalyseAll = "SELECT [Index],  Count(*) FROM dbo.PricingIndex WHERE [Index] is not null GROUP BY [Index]  ORDER BY Count(*) DESC";
            var sqlIndexsToAnalyse = @"SELECT [Index],  Count(*) FROM dbo.PricingIndex i
	JOIN TradePhysical p on i.Trade=p.TradePhysicalId
 WHERE [Index] is not null GROUP BY [Index]  ORDER BY Count(*) DESC";
            var sqlIndexNames = "SELECT Name, MasterEntityId From mdm.MasterEntity WHERE MasterEntityId in ({0})";

            var indexes = SmallDBHelper.ExecuteQuery(db, r => new Index() { Id = r.GetInt64(0) }, sqlIndexsToAnalyse);
            foreach (var index in indexes)
            {
                index.Name = SmallDBHelper.ExecuteQueryScalar<string>(db, sqlIndexNames, index.Id);

                // Get Prices
                var sqlPrices = "SELECT Cast(Price as float), Date, Type FROM [MarketPrice]  WHERE [Index]={0} AND [Type]={1} /* Mediam */";
                var allPrices = SmallDBHelper.ExecuteQuery(db,
                    r => new Sample(r.GetDateTime(1), r.GetDouble(0)),
                    sqlPrices, index.Id, 100070 /* Mediam */);

                if (!allPrices.Any()) continue;

                // Split into years
                var min = allPrices.Min(x => x.At);
                var max = allPrices.Max(x => x.At);

                for (int year = min.Year; year < max.Year; year++)
                {
                    var prices = allPrices.Where(x => x.At.Year == year).Cast<ISample>();
                    if (prices.Any())
                    {
                        var y = new IndexYear()
                        {
                            Year = year,
                            Prices = new TimeSeries(prices)
                            {
                                Name = "Prices",
                                Format = "$#,##0.00"
                            },
                        };
                        y.Returns = Domain.LogReturnSeries(y.Prices);
                        index.Add(y);    
                    }
                    
                }
                this.Add(index);
            }

            //// Correlations
            //var yearTarget = 2013;
            //foreach (var a in indexes)
            //{
            //    foreach (var b in indexes)
            //    {
            //        if (a[yearTarget] == null && b[yearTarget] == null) continue;

            //        var commonDataSet = GetCommonResult.Find(a[yearTarget].Returns, b[yearTarget].Returns);
            //    }
            //}
          



        }

        public class GetCommonResult
        {
            public DateRange Range { get; set; }
            public TimeSeries CommonA { get; set; }
            public TimeSeries CommonB { get; set; }


            public static GetCommonResult Find(TimeSeries a, TimeSeries b)
            {
                var rA = GetRange(a);
                var rB = GetRange(b);

                var intersect = DateRange.Intersection(rA, rB);
                if (intersect == null) return null;

                var res = new GetCommonResult();
                res.CommonA = new TimeSeries();
                res.CommonB = new TimeSeries();

                //foreach (var d in intersect)
                //{
                //    var dA = a[d];
                //    var dB = b[d];
                    
                //}
                return null;
            }

            public static DateRange GetRange(IEnumerable<ISample> a)
            {
                return DateRange.Combine(a.Select(x=>x.At));
            }
        }


        public IResult ToReport()
        {
            return new TableResult(this);
        }
    }

    public class Index : List<IndexYear>
    {
        public IndexYear this[int year]
        {
            get { return this.FirstOrDefault(x => x.Year == year); }
        }

        public long Id { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return string.Format("{0}  Count: {1}", Name, this.Count());
        }
    }

    public class IndexYear
    {
        public int Year { get; set; }
        public TimeSeries Prices { get; set; }
        public ISeries Returns { get; set; }
    }
}