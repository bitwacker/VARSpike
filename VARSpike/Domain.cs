﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;

namespace VARSpike
{
    public static class Domain
    {
        public static Normal norm = new Normal(0, 1);
        public static List<double> StandardConfidenceLevels = new List<double>() {0.9, 0.95, 0.99};

        public static double ClassicReturn(double now, double prev)
        {
            return (now - prev) / prev;
        }

        public static double ClassicReturnInv(double classicReturn, double price)
        {
            return price + price * classicReturn;
        }

        public static double LogReturn(double now, double prev)
        {
            return Math.Log(now / prev, Constants.E);
        }

        public static Sample LogReturn(ISample now, ISample prev)
        {
            if (now.At < prev.At) throw new InvalidDataException();
            return new Sample(now.At, Math.Log(now.Value / prev.Value, Constants.E));
        }


        public static double LogReturnInv(double logReturn, double price)
        {
            return price * Math.Exp(logReturn);
        }
      

        public static double NormalConfidenceIntervalNegOnly(double ci)
        {
            // NORM.INV
           // return norm.InverseCumulativeDistribution((1 - ci)/2);
            return norm.InverseCumulativeDistribution((1 - ci));
        }

        public static Series ClassicReturnSeries(ISeries prices)
        {
            var result = new Series();
            var last = prices.First();
            foreach (var p in prices.Skip(1))
            {
                result.Add(ClassicReturn(p, last));
                last = p;
            }
            return result;
        }

        public static Series LogReturnSeries(ISeries prices)
        {
            var result = new Series()
            {
                UnitOfMeasure = UnitOfMeasure.ReturnLog,
            };
            var last = prices.First();
            foreach (var p in prices.Skip(1))
            {
                result.Add(LogReturn(p, last));
                last = p;
            }
            return result;
        }


        public static TimeSeries LogReturnSeries(TimeSeries prices)
        {
            var result = new TimeSeries()
            {
                UnitOfMeasure = UnitOfMeasure.ReturnLog,
            };
            var last = prices.First<ISample>();
            foreach (var p in prices.Skip<ISample>(1))
            {
                result.Add(LogReturn(p, last));
                last = p;
            }
            return result;
        }

        
    }

    public class MathHelper
    {
        public static double RoundDecimal(double num, int decimalPlaces)
        {
            return Math.Round(num, decimalPlaces);
            //return Math.Round(num * Math.Pow(10, decimalPlaces)) / Math.Pow(10, decimalPlaces);
        }
    }
}