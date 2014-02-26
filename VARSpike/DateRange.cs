using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace VARSpike
{
    public class DateRange : IEnumerable<DateTime>
    {
        private DateTime start;
        private DateTime end;

        //allows for model binding
        public DateRange() { }

        public DateRange(DateTime start, DateTime end)
        {
            this.start = start;
            this.end = end;
        }

        public DateRange(DateTime? start, DateTime? end)
        {
            if (start == null || end == null)
            {
                this.start = new DateTime();
                this.end = new DateTime();
            }
            else
            {
                this.start = start.Value;
                this.end = end.Value;
            }
        }

        public DateRange(DateTime date)
        {
            this.start = date;
            this.end = date;
        }

        public DateRange(DateRange copy)
        {
            start = copy.start;
            end = copy.end;
        }

        public DateTime Start
        {
            get { return start; }
            set
            {
                if (end != DateTime.MinValue && value > end)
                {
                    // swap
                    start = end;
                    end = value;
                }
                else
                {
                    start = value;
                }
            }
        }
        public DateTime End
        {
            get { return end; }
            set
            {
                if (start != DateTime.MinValue && value < start)
                {
                    // swap
                    end = start;
                    start = value;
                }
                else
                {
                    end = value;
                }
            }
        }

        public bool IsNull
        {
            get { return start == DateTime.MinValue && end == DateTime.MinValue; }
        }

        public TimeSpan Duration
        {
            get { return End - Start; }
        }

        public int Length
        {
            get { return (int)Duration.TotalDays; }
        }

        public IEnumerator<DateTime> GetEnumerator()
        {
            var curr = start;
            while (curr <= end)
            {
                yield return curr;
                curr = curr.AddDays(1);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static DateRange Combine(IEnumerable<DateRange> ranges)
        {
            if (ranges == null) return null;
            ranges = ranges.Where(x => x != null);
            if (!ranges.Any()) return null;

            var result = new DateRange(ranges.First());

            foreach (var item in ranges.Skip(1))
            {
                if (item == null) continue;
                if (item.Start < result.Start) result.Start = item.Start;
                if (item.End > result.End) result.End = item.End;
            }
            return result;
        }

        public static DateRange Intersection(DateRange a, DateRange b)
        {
            if (a.Start > b.Start)
            {
                var x = a;
                a = b;
                b = x;
            }

            if (a.End < b.Start)
            {
                // No intersection
                return null;
            }

            if (a.End > b.End)
            {
                return new DateRange(b.Start, b.End);
            }
            else
            {
                return new DateRange(b.Start, a.End);
            }
        }

        static DateTime MinBusinessDate = new DateTime(1980, 1, 1);


        public static DateRange Combine(IEnumerable<DateTime> select)
        {
            if (!select.Any()) return null;
            var min = select.Where(x => x != DateTime.MinValue && x > MinBusinessDate).Min();
            var max = select.Where(x => x != DateTime.MaxValue).Max();
            return new DateRange(min, max);
        }

        public override string ToString()
        {
            if (start == DateTime.MinValue && end == DateTime.MinValue) return null;
            if (start == DateTime.MinValue) return "Invalid Range";
            if (end == DateTime.MinValue) return string.Format("from {0:dd-MMM-yyyy}", start);
            var days = (int)(end - start).TotalDays + 1;
            return string.Format("{0:dd-MMM-yyyy} to {1:dd-MMM-yyyy} ({2:0} {3})", start, end, days, days == 1 ? "day" : "days");
        }

        public bool Contains(DateTime date)
        {
            return start <= date && date <= End;
        }

        public bool Contains(DateRange range)
        {
            return start <= range.start && end >= range.end;
        }


        public int IsIn(DateTime date)
        {
            if (date < start) return -1;
            if (date > end) return 1;
            return 0;
        }

        public static DateRange From<T>(IEnumerable<T> data, Func<T, DateTime?> selector)
        {
            var min = data.Min(selector);
            var max = data.Max(selector);
            if (min == DateTime.MinValue) return null;
            return new DateRange(min, max);
        }


        public DateTime this[int idx]
        {
            get
            {
                return Start.AddDays(idx);
            }
        }
    }
}