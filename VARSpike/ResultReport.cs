﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Runtime.ExceptionServices;
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
            writer.WriteLine("<td class='null' colspan='1'></td>");
            for (int cc = 0; cc < Matrix.Size[0]; cc++)
            {
                writer.Write("<th>");
                writer.Write(Matrix.GetHeading(0, cc));
                writer.Write("</th>");    
            }
            writer.WriteLine("</tr>");

            for (int cc = 0; cc < Matrix.Size[1]; cc++)
            {
                writer.WriteLine("<tr>");
                // Head
                writer.Write("<th>");
                writer.Write(Matrix.GetHeading(1, cc));
                writer.Write("</th>");
                writer.WriteLine();

                // Data
                for (int xx = 0; xx < Matrix.Size[0]; xx++)
                {
                    var pos = new Vector<int>() {xx, cc};
                    writer.Write("<td>");
                    writer.Write(Matrix.GetCell(pos));
                    writer.Write("</td>");
                }
                writer.WriteLine();
                writer.WriteLine("</tr>");
            }

            writer.WriteLine("</table>");
            
        }
    }


    public class Vector<T> : List<T>
    {
        public Vector()
        {
        }

        public Vector(int capacity) : base(capacity)
        {
        }

        public Vector(IEnumerable<T> collection) : base(collection)
        {
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("(");
            var first = true;
            foreach (var item in this)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(", ");
                }
                sb.Append(item);
            }
            sb.Append(")");
            return sb.ToString();
        }
    }

  

    public class MatrixDefinition
    {
        public Vector<int> Size { get; set; }


        public Func<int, int, string> GetHeading { get; set; }
        public Func<Vector<int>, string> GetCell { get; set; }
    }

    public class MatrixDefinitionBySet : MatrixDefinition
    {
        protected MatrixDefinitionBySet()
        {
            GetHeading = GetHeadingBySet;
            GetCell = GetCellBySet;
        }

        public List<List<object>> Sets { get; protected set; }

        public Func<Vector<object>, string> RenderCell { get; set; }

        private string GetCellBySet(Vector<int> cell)
        {
            try
            {
                var element = new Vector<object>();
                var setIdx = 0;
                foreach (int i in cell)
                {
                    element.Add(Sets[setIdx][i]);
                    setIdx++;
                }
                return RenderCell(element);
            }
            catch (Exception ex)
            {
                return "[ERR] " + cell+ ex;
            }
            
        }

        private string GetHeadingBySet(int setIdx, int itemIdx)
        {
            return TextHelper.ToCell(Sets[setIdx][itemIdx]);
        }

        public static MatrixDefinitionBySet Define(Func<Vector<object>, string> renderCell , params IEnumerable[] sets)
        {
            var result = new List<List<object>>();
            foreach (var set in sets)
            {
                var l = new List<object>();
                foreach (var item in set)
                {
                    l.Add(item);
                }
                result.Add(l);
            }
            return new MatrixDefinitionBySet()
            {
                Sets = result,
                Size = new Vector<int>(result.Select(x => x.Count())),
                RenderCell = renderCell
            };
        }
    }

    public class MatrixDefinitionBySet2D<T1, T2> : MatrixDefinitionBySet
    {
        public static MatrixDefinitionBySet Define(Func<T1, T2, string> renderCell, IEnumerable<T1> set1, IEnumerable<T2> set2)
        {
            var result = new List<List<object>>();
            result.Add(new List<object>(set1.Cast<object>()));
            result.Add(new List<object>(set2.Cast<object>()));

            return new MatrixDefinitionBySet2D<T1, T2>()
            {
                Sets = result,
                Size = new Vector<int>(result.Select(x => x.Count())),
                RenderCell = (vector) => renderCell((T1) vector[0], (T2) vector[1])
            };
        }
    }

    public class MatrixDefinitionBySet3D<T1, T2, T3> : MatrixDefinitionBySet
    {
        public static MatrixDefinitionBySet Define(Func<T1, T2, T3, string> renderCell, IEnumerable<T1> set1, IEnumerable<T2> set2, IEnumerable<T3> set3)
        {
            var result = new List<List<object>>();
            result.Add(new List<object>(set1.Cast<object>()));
            result.Add(new List<object>(set2.Cast<object>()));
            result.Add(new List<object>(set3.Cast<object>()));

            return new MatrixDefinitionBySet3D<T1, T2, T3>()
            {
                Sets = result,
                Size = new Vector<int>(result.Select(x => x.Count())),
                RenderCell = (vector) => renderCell((T1)vector[0], (T2)vector[1], (T3)vector[2])
            };
        }
    }
}
