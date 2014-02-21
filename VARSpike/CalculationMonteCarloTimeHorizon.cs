using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;

namespace VARSpike
{
    public class CalculationMonteCarloTimeHorizon
    {
        public class MonteCarloResult
        {
            public MonteCarlo.Params Params { get; set; }

            public MonteCarlo MonteCarlo { get; set; }

        }
        
        public void Calculate(string[] args)
        {

            using (Reporter.HtmlOutput("CalculationMonteCarloTimeHorizon.html"))
            {
                
                //var repMP = new MarketPriceRepository();
                //var prices = new Series(repMP.GetPrices().Select(x => x.Value));
                var prices = new Series(Data.BrentCrude2013);

                Reporter.Write("Prices", prices);

                var lastPrice = prices.Last();

                var cr = Domain.ClassicReturnSeries(prices);
                Reporter.Write("ClassicReturns", cr);
                var crVAR = new ValueAtRisk(new Normal(cr.Mean(), cr.StandardDeviation()), Domain.StandardConfidenceLevels)
                {
                    Interpretations = new List<Interpretation>()
                    {
                        new Interpretation()
                        {
                            Name = "VaR Price",
                            Transform = (var) => Domain.ClassicReturnInv(var, lastPrice)
                        },
                        new Interpretation()
                        {
                            Name = "VaR delta(Price)",
                            Transform = (var) => Domain.ClassicReturnInv(var, lastPrice) - lastPrice
                        }
                    }
                };
                
              
                crVAR.Compute();
                Reporter.Write("VaR-ClassicReturns", crVAR);

                var lr = Domain.LogReturnSeries(prices);
                Reporter.Write("LogReturns", lr);
                var lrVAR = new ValueAtRisk(new Normal(lr.Mean(), lr.StandardDeviation()), Domain.StandardConfidenceLevels)
                {
                    Interpretations = new List<Interpretation>()
                    {
                        new Interpretation()
                        {
                            Name = "VaR Price",
                            Transform =(var) => Domain.LogReturnInv(var, lastPrice)
                        },
                        new Interpretation()
                        {
                            Name = "VaR delta(Price)",
                            Transform =(var) => Domain.LogReturnInv(var, lastPrice) - lastPrice
                        }
                    }
                };
                lrVAR.Compute();
                Reporter.Write("VaR-LogReturns", lrVAR);

                var varhist = new ValueAtRiskHistoric(Domain.StandardConfidenceLevels, prices, lr);
                varhist.Compute();
                Reporter.Write("VaR-Historic", varhist);

                var commonTimeHorizon = 10;
                var runPack = new List<MonteCarloResult>()
                {
                    new MonteCarloResult()
                    {
                        Params =  new MonteCarlo.Params()
                        {
                            
                            ReturnsType = ReturnType.Log,
                            ReturnsDist = new Normal(ExcelHelper.ToExcelPrecision(lr.Mean()), ExcelHelper.ToExcelPrecision(lr.StandardDeviation())),
                            InitialPrice = prices.Last(),
                            TimeHorizon = commonTimeHorizon,
                            ConfidenceIntervals = Domain.StandardConfidenceLevels,

                            // Quality
                            Quality_IntraDaySteps = 8,
                            Quality_ScenarioCount = 1000,
                        },
                    },
                    new MonteCarloResult()
                    {
                        Params = new MonteCarlo.Params()
                        {
                            
                            ReturnsType = ReturnType.Classic,
                            ReturnsDist = new Normal(ExcelHelper.ToExcelPrecision(cr.Mean()), ExcelHelper.ToExcelPrecision(cr.StandardDeviation())),
                            InitialPrice = prices.Last(),
                            TimeHorizon = commonTimeHorizon,
                            ConfidenceIntervals = Domain.StandardConfidenceLevels,

                            // Quality
                            Quality_IntraDaySteps = 8,
                            Quality_ScenarioCount = 1000
                        }
                    },

                    new MonteCarloResult()
                    {
                        Params =  new MonteCarlo.Params()
                        {
                            
                            ReturnsType = ReturnType.Log,
                            ReturnsDist = new Normal(ExcelHelper.ToExcelPrecision(lr.Mean()), ExcelHelper.ToExcelPrecision(lr.StandardDeviation())),
                            InitialPrice = prices.Last(),
                            TimeHorizon = commonTimeHorizon,
                            ConfidenceIntervals = Domain.StandardConfidenceLevels,

                            // Quality
                            Quality_IntraDaySteps = 8,
                            Quality_ScenarioCount = 10000,
                        },
                    },
                    new MonteCarloResult()
                    {
                        Params = new MonteCarlo.Params()
                        {
                            
                            ReturnsType = ReturnType.Classic,
                            ReturnsDist = new Normal(ExcelHelper.ToExcelPrecision(cr.Mean()), ExcelHelper.ToExcelPrecision(cr.StandardDeviation())),
                            InitialPrice = prices.Last(),
                            TimeHorizon = commonTimeHorizon,
                            ConfidenceIntervals = Domain.StandardConfidenceLevels,

                            // Quality
                            Quality_IntraDaySteps = 8,
                            Quality_ScenarioCount = 10000
                        }
                    },

                    new MonteCarloResult()
                    {
                        Params =  new MonteCarlo.Params()
                        {
                            
                            ReturnsType = ReturnType.Log,
                            ReturnsDist = new Normal(ExcelHelper.ToExcelPrecision(lr.Mean()), ExcelHelper.ToExcelPrecision(lr.StandardDeviation())),
                            InitialPrice = prices.Last(),
                            TimeHorizon = commonTimeHorizon,
                            ConfidenceIntervals = Domain.StandardConfidenceLevels,

                            // Quality
                            Quality_IntraDaySteps = 32,
                            Quality_ScenarioCount = 5000,
                        },
                    },
                    new MonteCarloResult()
                    {
                        Params = new MonteCarlo.Params()
                        {
                            
                            ReturnsType = ReturnType.Classic,
                            ReturnsDist = new Normal(ExcelHelper.ToExcelPrecision(cr.Mean()), ExcelHelper.ToExcelPrecision(cr.StandardDeviation())),
                            InitialPrice = prices.Last(),
                            TimeHorizon = commonTimeHorizon,
                            ConfidenceIntervals = Domain.StandardConfidenceLevels,

                            // Quality
                            Quality_IntraDaySteps = 32,
                            Quality_ScenarioCount = 5000
                        }
                    },

                    // new MonteCarloResult()
                    //{
                    //    Params =  new MonteCarlo.Params()
                    //    {
                            
                    //        ReturnsType = ReturnType.Log,
                    //        ReturnsDist = new Normal(ExcelHelper.ToExcelPrecision(lr.Mean()), ExcelHelper.ToExcelPrecision(lr.StandardDeviation())),
                    //        InitialPrice = prices.Last(),
                    //        TimeHorizon = commonTimeHorizon,
                    //        ConfidenceIntervals = Domain.StandardConfidenceLevels,

                    //        // Quality
                    //        Quality_IntraDaySteps = 32,
                    //        Quality_ScenarioCount = 250000,
                    //    },
                    //},
                    //new MonteCarloResult()
                    //{
                    //    Params = new MonteCarlo.Params()
                    //    {
                            
                    //        ReturnsType = ReturnType.Classic,
                    //        ReturnsDist = new Normal(ExcelHelper.ToExcelPrecision(cr.Mean()), ExcelHelper.ToExcelPrecision(cr.StandardDeviation())),
                    //        InitialPrice = prices.Last(),
                    //        TimeHorizon = commonTimeHorizon,
                    //        ConfidenceIntervals = Domain.StandardConfidenceLevels,

                    //        // Quality
                    //        Quality_IntraDaySteps = 32,
                    //        Quality_ScenarioCount = 250000
                    //    }
                    //},
                };


                foreach (var item in runPack)
                {
                    if (item.Params.Name == null)
                    {
                        item.Params.Name = string.Format("T:{0} Q(s:{1}, dt:{2})", item.Params.TimeHorizon,
                            item.Params.Quality_ScenarioCount, item.Params.Quality_IntraDaySteps);
                    }
                    
                }

                bool parra = true;
                if (parra)
                {
                    Parallel.ForEach(runPack, item =>
                    {
                        using (var timer = new CodeTimerConsole(item.Params.Name))
                        {
                            item.MonteCarlo = new MonteCarlo(item.Params);
                            item.MonteCarlo.Compute();
                            timer.Count = item.Params.TimeHorizon;
                        }
                    });
                }
                else
                {
                    foreach (var item in runPack)
                    {
                        using (var timer = new CodeTimerConsole(item.Params.Name))
                        {
                            item.MonteCarlo = new MonteCarlo(item.Params);
                            item.MonteCarlo.Compute();
                            timer.Count = item.Params.TimeHorizon;
                        }
                    }
                }

                Reporter.Write(new HeadingResult("COMPARISON"));

                Reporter.Write(new UIMatrixResult(UiMatrixDefinitionBySet4D<double, string, ReturnType, VaRMethod>.Define(
                    (ci, name, method, type) =>
                    {
                        var calcWrap = runPack.FirstOrDefault(x=>x.Params.Name == name && x.Params.ReturnsType == method);
                        if (calcWrap == null) return "(null)";
                        var calc = calcWrap.MonteCarlo;
                        if (type == VaRMethod.VarCoVar)
                        {
                            return TextHelper.ToCell(
                                calc.ResultVarCoVar.Results[calc.Parameters.ConfidenceIntervals.IndexOf(ci)].Item2 - calc.Parameters.InitialPrice
                                );
                        }
                        else // Percentile
                        {
                            return TextHelper.ToCell(
                                calc.ResultPercentile[calc.Parameters.ConfidenceIntervals.IndexOf(ci)]
                                );
                        }
                    },
                    Domain.StandardConfidenceLevels,
                    runPack.Select(x=>x.Params.Name).Distinct(),
                    new ReturnType[] { ReturnType.Classic, ReturnType.Log },
                    new VaRMethod[] { VaRMethod.Percentile, VaRMethod.VarCoVar }
                    
                    )));


                var simpleVAR = new ValueAtRisk(new Normal(lr.Mean(), lr.StandardDeviation()), Domain.StandardConfidenceLevels, commonTimeHorizon)
                {
                    Interpretations = new List<Interpretation>()
                    {
                        new Interpretation()
                        {
                            Name = "VaR Price",
                            Transform =(var) => Domain.LogReturnInv(var, lastPrice)
                        },
                        new Interpretation()
                        {
                            Name = "VaR delta(Price)",
                            Transform =(var) => Domain.LogReturnInv(var, lastPrice) - lastPrice
                        }
                    }
                };
                simpleVAR.Compute();
                Reporter.Write("Simple VaR using LogReturns", simpleVAR);
            }
        }

       
    }
}