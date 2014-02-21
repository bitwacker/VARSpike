using System;
using System.Dynamic;
using System.IO;
using System.Net.NetworkInformation;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using MathNet.Numerics.LinearAlgebra.Generic.Solvers.Status;

namespace VARSpike
{
    public class Program
    {
        static void Main(string[] args)
        {
            //new CalculationMonteCarloTimeHorizon().Calculate(args);
            new CalculationCorrelation().Calculate(args);

        }
    }
}
