using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using MathNet.Numerics.LinearAlgebra.Generic.Solvers.Status;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace VARSpike
{
    public class Program
    {
        static void Main(string[] args)
        {
            //new CalculationMonteCarloTimeHorizon().Calculate(args);
            //new CalculationCorrelation().Calculate(args);
            new IndexAnalysis().Calculate(args);
        }
    }

   
}
