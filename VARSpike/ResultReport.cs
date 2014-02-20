using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Mime;
using System.Text;

namespace VARSpike
{
    public static class Reporter
    {
        static Reporter()
        {
            // Default
            Implementation = x => Console.WriteLine(x.ToString());
        }

        public static Action<IResult> Implementation { get; set; }

        public static void Write(IResult result)
        {
            Implementation(result);
        }

        public static void Write(string heading, params IResult[] args)
        {
            Console.WriteLine("{0}", heading);
            Console.WriteLine("------------------------");
            foreach (var arg in args)
            {
                Write(arg);
            }
        }

        public static void Write(string heading, params IReporter[] args)
        {
            Console.WriteLine("{0}", heading);
            Console.WriteLine("------------------------");
            foreach (var arg in args)
            {
                Write(arg.ToReport());
            }
        }
    }

    public interface IReporter
    {
        IResult ToReport();
    }

    public interface  IResult
    {

    }

    public class StringResult : IResult
    {
        public StringResult()
        {
        }

        public StringResult(string s)
        {
            String = s;
        }

        public StringResult(StringBuilder sb)
        {
            String = sb.ToString();
        }

        public string String { get; set; }

        public override string ToString()
        {
            return String;
        }
    }


    public class PropertyListResult : List<Tuple<string, object>>, IResult
    {
        public PropertyListResult Add(string name, object val)
        {
            base.Add(new Tuple<string, object>(name, val));
            return this;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var item in this)
            {
                sb.AppendFormat("{0,10}: {1} ",item.Item1, TextHelper.ToCell(item.Item2));
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }

    public class TableResult : IResult
    {
        private IEnumerable source;

        public TableResult(IEnumerable source)
        {
            this.source = source;
        }

        public override string ToString()
        {
            var nums = source as IEnumerable<double>;
            if (nums != null) return TextHelper.ToTable(nums);
            return TextHelper.ToTable(source);
        }
    }

    public class CompountResult : List<IResult>, IResult
    {
        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var item in this)
            {
                sb.Append(item.ToString());
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}