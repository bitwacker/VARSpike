using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using MathNet.Numerics.Random;

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
            ImplementationConsole = Console.Out;
        }

        public static TextWriter ImplementationConsole { get; set; }
        public static TextWriter ImplementationFileText { get; set; }
        public static TextWriter ImplementationFileHTML { get; set; }


        public static void Write(IResult result)
        {
            if (ImplementationConsole != null) result.Output(ReportFormat.Console, ImplementationConsole);
            if (ImplementationFileText != null) result.Output(ReportFormat.Text, ImplementationFileText);
            if (ImplementationFileHTML != null) result.Output(ReportFormat.Html, ImplementationFileHTML);
        }

        public static void Write(IReporter reporter)
        {
            Write(reporter.ToReport());
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
            

            public HtmlOut(string fileName)
            {
                fileWriter = new StreamWriter(File.OpenWrite(fileName));
                Reporter.ImplementationFileHTML = fileWriter;
                
                WriteHeader();
            }

            //private void ImplementationFileHTML(IResult obj)
            //{
            //    fileWriter.WriteLine(obj.ToHTML()
            //        .Replace("μ", "&#956;")
            //        .Replace("σ", "&#963;")
            //        .Replace("⇔", "&#8660;")

            //        );
            //}

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
                Reporter.ImplementationFileHTML = null;

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
        void Output(ReportFormat format, TextWriter writer);
    }

    public abstract class CommonResult : IResult
    {
        public void Output(ReportFormat format, TextWriter writer)
        {
            switch (format)
            {
                case(ReportFormat.Console):
                case (ReportFormat.Text): writer.Write(ToString());
                    break;
                case (ReportFormat.Html): writer.Write(ToHTML());
                    break;
            }
        }

        public abstract string ToHTML();
    }

    public class StringResult : CommonResult
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


        public override string ToHTML()
        {
            return string.Format("<pre>{0}</pre>", String);
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
        public void Output(ReportFormat format, TextWriter writer)
        {
            switch (format)
            {
                case (ReportFormat.Console):
                case (ReportFormat.Text): writer.Write(ToString());
                    break;
                case (ReportFormat.Html): writer.Write(ToHTML());
                    break;
            }
        }
    }

    public class TableResult : CommonResult
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

        public override string ToHTML()
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

    public class HeadingResult : CommonResult
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

        public override string ToHTML()
        {
            return string.Format("<h3>{0}</h3>", heading);
        }
    }

    public class CompountResult : List<IResult>, IResult
    {
        public void Output(ReportFormat format, TextWriter writer)
        {
            foreach (var item in this)
            {
                item.Output(format, writer);
            }
        }
    }


    public class VerboseResult :  IResult
    {
        private readonly Action<ReportFormat, TextWriter> writer;

        public VerboseResult(Action<ReportFormat, TextWriter> writer)
        {
            this.writer = writer;
        }

        public void Output(ReportFormat format, TextWriter output)
        {
            if (format != ReportFormat.Console)
            {
                this.writer(format, output);
            }
        }
    }
}