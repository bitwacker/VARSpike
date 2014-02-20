using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace VARSpike
{
    public enum ReportFormat
    {
        Console,
        Text,
        Html
    }

    public static class Reporter
    {
        static Reporter()
        {
            // Default
            ImplementationConsole = x => Console.WriteLine(x.ToString());
        }

        public static Action<IResult> ImplementationConsole { get; set; }
        public static Action<IResult> ImplementationFileText { get; set; }
        public static Action<IResult> ImplementationFileHTML { get; set; }

        public static void Write(IResult result)
        {
            if (ImplementationConsole != null) ImplementationConsole(result);
            if (ImplementationFileText != null) ImplementationFileText(result);
            if (ImplementationFileHTML != null) ImplementationFileHTML(result);
        }

        public static void Write(string heading, params IResult[] args)
        {
            Write(new HeadingResult(heading));
            foreach (var arg in args)
            {
                Write(arg);
            }
        }

        public static void Write(string heading, params IReporter[] args)
        {
            Write(new HeadingResult(heading));
            foreach (var arg in args)
            {
                Write(arg.ToReport());
            }
        }

        public static void WriteLine(string format, params object[] args)
        {
            if (format == null) return;
            Write(new StringResult(string.Format(format, args)));
        }

        private class HtmlOut : IDisposable
        {
            private StreamWriter fileWriter;
            private Action<IResult> old;

            public HtmlOut(string fileName)
            {
                old = Reporter.ImplementationFileHTML;
                Reporter.ImplementationFileHTML = this.ImplementationFileHTML;

                fileWriter = new StreamWriter(File.OpenWrite(fileName));
                WriteHeader();
            }

            private void ImplementationFileHTML(IResult obj)
            {
                fileWriter.WriteLine(obj.ToHTML());
            }

            private void WriteHeader()
            {
                fileWriter.WriteLine("<html>");
                fileWriter.WriteLine("<head>");
                fileWriter.WriteLine("<link href='result-report.css' rel='stylesheet' />");
                fileWriter.WriteLine("</head>");
                fileWriter.WriteLine("<body>");
            }

            public void Dispose()
            {
                fileWriter.WriteLine("</body>");
                fileWriter.WriteLine("</html>");

                fileWriter.Dispose();
                Reporter.ImplementationFileHTML = old;
            }
        }

        public static IDisposable HtmlOutput(string fileName)
        {
            return new HtmlOut(fileName);
        }
    }

    public interface IReporter
    {
        IResult ToReport();
    }

    public interface  IResult
    {
        string ToHTML();
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

        public string ToHTML()
        {
            return string.Format("<span>{0}</span>", String);
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
                sb.AppendFormat("{0,15}: {1} ",item.Item1, TextHelper.ToCell(item.Item2));
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public string ToHTML()
        {
            var sb = new StringBuilder();
            sb.Append("<ul>");
            foreach (var item in this)
            {
                sb.AppendFormat("<li><div class='head'>{0,15}</div> <div class='cell'>{1}</div></li> ", item.Item1, TextHelper.ToCell(item.Item2));
                sb.AppendLine();
            }
            sb.Append("</ul>");
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
            if (nums != null) return TextHelper.ToTable(nums) + Environment.NewLine;
            return TextHelper.ToTable(source) + Environment.NewLine;
        }

        public string ToHTML()
        {
            return ToTable(source);
        }

        public static string ToTable(IEnumerable data)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<table class='dataTable'>");
            int cc = 0;
            sb.AppendLine("<tr>");
            bool open = false;
            foreach (var d in data)
            {
                
                sb.Append("<td>");
                sb.Append(TextHelper.ToCell(d));
                sb.Append("</td>");
                open = true;

                cc++;
                if (cc % 10 == 0)
                {
                    sb.AppendLine("</tr>");
                    sb.AppendLine("<tr>");
                    open = false;
                }
            
            }
            if (open) sb.AppendLine("</tr>");
            sb.AppendLine("</table>");
            return sb.ToString();
        }
    }

    public class HeadingResult : IResult
    {
        private string heading;

        public HeadingResult(string heading)
        {
            this.heading = heading;
        }

        public override string ToString()
        {
            return heading + Environment.NewLine + "====================" + Environment.NewLine;
        }

        public string ToHTML()
        {
            return string.Format("<h3>{0}</h3>", heading);
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
                
            }
            return sb.ToString();
        }

        public string ToHTML()
        {
            var sb = new StringBuilder();
            foreach (var item in this)
            {
                sb.Append(item.ToHTML());

            }
            return sb.ToString();
        }
    }
}