using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using MathNet.Numerics.LinearAlgebra.Generic.Solvers.Status;
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
                fileWriter = new StreamWriter(fileName, false);
                Reporter.ImplementationFileHTML = fileWriter;
                
                WriteHeader();
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
                Reporter.ImplementationFileHTML = null;

                fileWriter.WriteLine("</body>");
                fileWriter.WriteLine("</html>");
                fileWriter.Flush();

                fileWriter.Dispose();
                

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

        public StringResult(string s, string htmlString)
        {
            String = s;
            HtmlString = htmlString;
        }

        public StringResult(StringBuilder sb)
        {
            String = sb.ToString();
        }

        public string String { get; set; }

        public string HtmlString { get; set; }

        public override string ToString()
        {
            return String;
        }


        public override string ToHTML()
        {
            return string.Format("<pre>{0}</pre>", HtmlString ?? String);
        }
    }


    public class PropertyListResult : List<Tuple<string, object>>, IResult
    {
        public PropertyListResult Add(string name, object val)
        {
            base.Add(new Tuple<string, object>(name, val));
            return this;
        }


        public void Output(ReportFormat format, TextWriter writer)
        {
            switch (format)
            {
                case (ReportFormat.Console):
                case (ReportFormat.Text): 
            
                    foreach (var item in this)
                    {
                        writer.WriteLine("{0,15}: {1} ", item.Item1, TextHelper.ToCell(item.Item2, format));
                    }
            
                    break;
                case (ReportFormat.Html):
                    writer.WriteLine("<ul>");
                    foreach (var item in this)
                    {
                        writer.WriteLine("<li><div class='head'>{0,15}</div> <div class='cell'>{1}</div></li> ", item.Item1, TextHelper.ToCell(item.Item2, format));
                    }
                    writer.WriteLine("</ul>");
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


    public class MatrixResult : IResult
    {
        public MatrixResult()
        {
        }

        public MatrixResult(MatrixDefinition matrix)
        {
            Matrix = matrix;
        }

        public MatrixDefinition Matrix { get; set; }

        public void Output(ReportFormat format, TextWriter writer)
        {
            
            writer.WriteLine("<table class='matrix'>");

            // Horz Headers
            writer.WriteLine("<tr>");
            writer.WriteLine("<td class='null' colspan='{0}'></td>", Matrix.Size.Count-1);
            for (int cc = 0; cc < Matrix.Size[0]; cc++)
            {
                writer.Write("<th>");
                writer.Write(Matrix.GetHeading(0, cc));
                writer.Write("</th>");    
            }
            writer.WriteLine("</tr>");

            foreach (var row in GetRowSpecs())
            {
                writer.WriteLine("<tr>");

                // Headers
                for (int rr = 0; rr < row.Count; rr++)
                {
                    writer.Write("<th>");
                    writer.Write(Matrix.GetHeading(rr+1, row[rr]));
                    writer.Write("</th>");
                }

                // Data Over- Dim0
                for (int cc = 0; cc < Matrix.Size[0]; cc++)
                {
                    var pos = new Vector<int>(row);
                    pos.Insert(0, cc);
                    writer.Write("<td>");
                    writer.Write(Matrix.GetCell(pos));
                    writer.Write("</td>");
                }


                writer.WriteLine("</tr>");
            }

            writer.WriteLine("</table>");
            
        }

      

        private IEnumerable<Vector<int>> GetRowSpecs()
        {
            
            var innerCounts = new List<int>(Matrix.Size);
            innerCounts.RemoveAt(0);
            return TreeProduct(innerCounts);
            
            
        }

        public static List<Vector<int>> TreeProduct(List<int> kidLengths)
        {
            if (kidLengths.Count == 1)
            {
                var node = new List<Vector<int>>();
                for (int cc = 0; cc < kidLengths[0]; cc++)
                {
                    node.Add(new Vector<int>() { cc });
                }
                return node;
            }
            else
            {
                var myResults = new List<Vector<int>>();

                var myCount = kidLengths.First();
                var innerCounts = new List<int>(kidLengths);
                innerCounts.RemoveAt(0);
                
                for (int cc = 0; cc < myCount; cc++)
                {
                    var innerSet = TreeProduct(innerCounts);
                    foreach (var inner in innerSet)
                    {
                        inner.Insert(0, cc);
                        myResults.Add(inner);
                    }
                }
                return myResults;
            }
        }

        private int RowCount(MatrixDefinition matrix)
        {
            return matrix.Size.Skip(1).Max();
        }
    }


}
