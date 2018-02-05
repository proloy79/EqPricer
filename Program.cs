using System;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.Diagnostics;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;
//using Microsoft.VisualBasic;
using NLog;

namespace EqPricer
{
    class Program
    {        
        static void Main(string[] args)
        {
            bool onlyResults = !(args.Any() && args.Contains("displayAll"));

            var msg = new StringBuilder("Parsing trades");
            string portfolioId = "";

            Directory.CreateDirectory("trades");            
            var portfolioDefs = Directory.EnumerateFiles("trades").ToList();

            if (!portfolioDefs.Any())
                Console.WriteLine("ERROR : No trade definition files availalble in trades folder, please add files similar to TestPortfolio.txt");

            var resultDataTbl = new List<string>();
            resultDataTbl.Add("Portfolio,TradeId,PV,RunTime(sec)");

            foreach (var file in portfolioDefs)
            {
                try
                {
                    int id = 1;
                    var trades = File.ReadAllLines(file);
                    portfolioId = Path.GetFileNameWithoutExtension(file);
                    var portfolio = PortfolioPricer.Factory.CreatePortfolioPricer(portfolioId);
                                        
                    string prodType = "";
                    DateTime startDate = DateTime.Now;
                    double assetPrice = 100;
                    double expiryInYrs = 1;
                    double ir = 0.05;
                    double strike = 100;
                    double vol = 0.2;
                    var samplingType = SamplingTypeE.NoSampling;
                    var optionType = OptionTypeE.Call;
                    var samplingFreq = FreqE.Daily;
                    int noOfPaths = 0;
                    var underlyingSdeType = SdeTypeE.Gbm;

                    foreach (var trade in trades)
                    {
                        if (trade == "")
                            continue;

                        var dict = new Dictionary<string, string>();
                        trade.Split(';').ToList().ForEach(arg =>
                        {
                            if (arg != "")
                            {
                                var tmp = arg.Split(':');
                                dict.Add(tmp[0], tmp[1]);
                            }
                        });

                        msg.Clear().Append("parsed trade : " + trade);

                        string tradeId = "";
                        foreach (var key in dict.Keys)
                        {
                            msg.Clear().Append("reading " + key + " : " + dict[key]);

                            if (key == "StockPrice") assetPrice = GetDoubleValue(dict[key]);
                            msg.Clear().Append("reading " + key + " : " + dict[key]);
                            if (key == "Strike") strike = GetDoubleValue(dict[key]);
                            msg.Clear().Append("reading " + key + " : " + dict[key]);
                            if (key == "TimeToExpiry") expiryInYrs = GetDoubleValue(dict[key]);
                            msg.Clear().Append("reading " + key + " : " + dict[key]);
                            if (key == "Vol") vol = GetDoubleValue(dict[key]);
                            msg.Clear().Append("reading " + key + " : " + dict[key]);
                            if (key == "Ir") ir = GetDoubleValue(dict[key]);
                            msg.Clear().Append("reading " + key + " : " + dict[key]);
                            if (key == "NoOfPaths") noOfPaths = Convert.ToInt32(dict[key]);                            
                            msg.Clear().Append("reading " + key + " : " + dict[key]);
                            if (key == "StartDate")
                                startDate = DateTime.ParseExact(dict[key], "yyyyMMdd", CultureInfo.InvariantCulture);
                            msg.Clear().Append("reading " + key + " : " + dict[key]);
                            if (key == "ProdType") prodType = dict[key];
                            msg.Clear().Append("reading " + key + " : " + dict[key]);
                            if (key == "TradeId") tradeId = dict[key];
                            msg.Clear().Append("reading " + key + " : " + dict[key]);
                            if (key == "SamplingType")
                                samplingType = (SamplingTypeE) Enum.Parse(typeof (SamplingTypeE), dict[key]);
                            msg.Clear().Append("reading " + key + " : " + dict[key]);
                            if (key == "Freq") samplingFreq = (FreqE) Enum.Parse(typeof (FreqE), dict[key]);
                            msg.Clear().Append("reading " + key + " : " + dict[key]);
                            if (key == "SdeType")
                                underlyingSdeType = (SdeTypeE) Enum.Parse(typeof (SdeTypeE), dict[key]);
                            msg.Clear().Append("reading " + key + " : " + dict[key]);
                            if (key == "OptionType")
                                optionType = (OptionTypeE) Enum.Parse(typeof (OptionTypeE), dict[key]);
                            msg.Clear().Append("reading " + key + " : " + dict[key]);
                        }
                        IPricer pricer = null;

                        msg.Clear().Append("instantiating ").Append(prodType);

                        tradeId = id + "_" + optionType + "_" + noOfPaths;
                        id = id + 1;

                        if (prodType == "AsianOptFixedStrikeMC")
                        {
                            pricer = PortfolioPricer.Factory.CreateAsianOptMcFixedStrike(tradeId, startDate,
                                assetPrice,
                                expiryInYrs, ir, vol,
                                samplingType, samplingFreq, optionType, noOfPaths, underlyingSdeType, strike);
                        }
                        else if (prodType == "AsianOptFloatStrikeMC")
                        {
                            pricer = PortfolioPricer.Factory.CreateAsianOptMcFloatStrike(tradeId, startDate,
                                assetPrice,
                                expiryInYrs, ir, vol,
                                samplingType, samplingFreq, optionType, noOfPaths, underlyingSdeType);
                        }
                        else if (prodType == "AsianCfGeoAvFixedStrikeMC")
                        {
                            pricer = PortfolioPricer.Factory.CreateAsianCfGeoAvFixedStrikePricer(tradeId, startDate,
                                assetPrice, expiryInYrs, ir, vol,
                                optionType, strike);
                        }
                        else if (prodType == "AsianCfGeoAvFloatStrikeMC")
                        {
                            pricer = PortfolioPricer.Factory.CreateAsianCfGeoAvFloatStrikePricer(tradeId, startDate,
                                assetPrice, expiryInYrs, ir, vol,
                                optionType);
                        }
                        else if (prodType == "EurOptMC")
                        {
                            pricer = PortfolioPricer.Factory.CreateEurOptionMcPricer(tradeId, startDate, assetPrice,
                                expiryInYrs, ir, vol,
                                optionType, noOfPaths, underlyingSdeType, strike);
                        }
                        else if (prodType == "LookbackFixedStrikeMC")
                        {
                            pricer = PortfolioPricer.Factory.CreateLookbackFixedStrikeMcPricer(tradeId, 
                                startDate, assetPrice, expiryInYrs, ir, vol, samplingType, samplingFreq, 
                                optionType, noOfPaths, underlyingSdeType, strike);
                        }
                        else if (prodType == "LookbackFloatStrikeMC")
                        {
                            pricer = PortfolioPricer.Factory.CreateLookbackFloatStrikeMcPricer(tradeId,
                                startDate, assetPrice, expiryInYrs, ir, vol, samplingType, samplingFreq,
                                optionType, noOfPaths, underlyingSdeType);
                        }

                        portfolio.Trades.Add((PortfolioPricer)pricer);                        
                    }

                    msg.Clear().Append("pricing portfolio");
                    var pv = portfolio.CalcPv();
                    
                    msg.Clear().Append("portfolio pv : ").Append(pv);
                    var result = portfolio.Summarize(onlyResults);

                    resultDataTbl.AddRange(portfolio.ResultTable.Skip(1));

                    EqLogger.Log.Info(result);
                    
                }
                catch (Exception ex)
                {
                    EqLogger.Log.Error("Failed for portfolio : " + portfolioId + "\nError : " + msg + " : " + ex.Message);
                }

                File.WriteAllLines(Path.Combine("logs",DateTime.Now.ToString("yyyyMMdd") + "_results.csv"), resultDataTbl);
            }            
            Console.WriteLine("\nEnter and key to exit!");
            Console.ReadKey();
        }

        static double GetDoubleValue(string str)
        {
            double rtn;

            if (str.Contains('%'))
            {
                str = str.Replace(CultureInfo.CurrentCulture.NumberFormat.PercentSymbol, "");
                rtn = Convert.ToDouble(str) * 1 / 100;
            }
            else
                rtn = Convert.ToDouble(str);
            
            return rtn;
        }        
    }
    public enum FreqE
    {
        Daily,      // 1/252
        Weekly,     // 5/252
        Monthly,    // 1/12
        Quarterly,   // 1/4
        SemiAnually,// 1/2
        Anually     // 1
    }
    public enum SdeTypeE
    {
        Gbm,
        GbmClosedForm //will use single leap to expiry
    }

    public enum OptionTypeE
    {
        Put,
        Call
    }
    
    //Pass freq as Daily for the continous versions            
    public enum SamplingTypeE
    {
        NoSampling,
        DiscreteArithAv,
        DiscreteGeoAv,
        DiscreteMax,
        DiscreteMin        
    }

    #region PRICER
    
    public class PortfolioPricer : IPricer
    {        
        public string TradeId { get; protected set; }
        public double PV { get; protected set; }
        public double RunTime { get; protected set; }

        private readonly Lazy<List<PortfolioPricer>> _trades = new Lazy<List<PortfolioPricer>>();

        public List<PortfolioPricer> Trades
        {
            get
            {
                return _trades.Value;
            }
        }

        protected PortfolioPricer(string portfolioId)
        {
            TradeId = portfolioId;      
            ResultTable = new List<string>();
        }

        public virtual double CalcPv()
        {            
            var stopwatch = new Stopwatch();

            stopwatch.Start();
            foreach (var trade in Trades)
            {
                var stopwatchT = new Stopwatch();
                stopwatchT.Start();
                PV += trade.CalcPv();
                stopwatchT.Stop();
                trade.RunTime = stopwatchT.Elapsed.TotalSeconds;
            }
            stopwatch.Stop();
            
            RunTime = stopwatch.Elapsed.TotalSeconds;

            return PV;
        }

        public virtual string Summarize(bool onlyResults)
        {
            ResultTable.Add("Portfolio,TradeId,PV,RunTime(sec)");

            var sb = new StringBuilder();
            sb.AppendFormat("--------------------------------------------------\nPortfolio : {0}" +
                            "\n--------------------------------------------------\n\tPV : {1}" +
                            "\n\tTime : {2} (sec)\n", 
                            TradeId, PV, RunTime);
            
            foreach (IOptionPricer trade in Trades)
            {
                sb.Append("\n").AppendFormat(trade.Summarize(onlyResults));   
                ResultTable.AddRange(trade.ResultTable.Select(x => TradeId + "," + x));                                          
                
            }

            sb.Append("--------------------------------------------------\n");

            return sb.ToString();            
        }

        public List<string> ResultTable { get; protected set; }

        public static class Factory
        {
            public static PortfolioPricer CreatePortfolioPricer(string portfolioId)
            {
                return new PortfolioPricer(portfolioId);
            }

            public static IPricer CreateAsianOptMcFixedStrike(string tradeId, DateTime startDate, 
                            double assetPrice,double expiryInYrs, double ir, double vol, 
                            SamplingTypeE samplingType, FreqE samplingFreq, OptionTypeE optionType,
                            int noOfPaths, SdeTypeE underlyingAssetSde, double fixedStrike)
            {                
                    return new AsianOptionMcFixedStrikePricer(tradeId, startDate, assetPrice, 
                                expiryInYrs, ir, vol, samplingType, samplingFreq, optionType,
                                noOfPaths, underlyingAssetSde,fixedStrike);                
            }

            public static IPricer CreateAsianOptMcFloatStrike(string tradeId, DateTime startDate, 
                            double assetPrice, double expiryInYrs, double ir, double vol, 
                            SamplingTypeE samplingType, FreqE samplingFreq, OptionTypeE optionType,
                            int noOfPaths, SdeTypeE underlyingAssetSde)
            {
                return new AsianOptionMcFloatStrikePricer(tradeId, startDate, assetPrice, 
                                expiryInYrs, ir, vol, samplingType, samplingFreq, optionType,
                                noOfPaths, underlyingAssetSde);
            }

            public static IPricer CreateAsianCfGeoAvFixedStrikePricer(string tradeId, DateTime startDate, 
                double assetPrice,
                double expiryInYrs, double ir, double vol, OptionTypeE optionType, double fixedStrike)
            {
                return new AsianCfGeoAvFixedStrikePricer(tradeId, startDate, assetPrice, 
                                            expiryInYrs, ir, vol, optionType, fixedStrike);                
            }

            public static IPricer CreateAsianCfGeoAvFloatStrikePricer(string tradeId, DateTime startDate, 
                                double assetPrice, double expiryInYrs, double ir, double vol, 
                                OptionTypeE optionType)
            {
                return new AsianCfGeoAvFloatStrikePricer(tradeId, startDate, assetPrice, 
                                    expiryInYrs, ir, vol, optionType);
            }

            public static IPricer CreateEurOptionMcPricer(string tradeId, DateTime startDate, double assetPrice,
                                    double expiryInYrs, double ir, double vol, OptionTypeE optionType,
                                    int noOfPaths, SdeTypeE underlyingAssetSde, double fixedStrike)
            {                
                return new EurOptionMcPricer(tradeId, startDate, assetPrice, expiryInYrs, ir, vol, optionType,
                                noOfPaths, underlyingAssetSde, fixedStrike);
            }

            public static IPricer CreateLookbackFixedStrikeMcPricer(string tradeId, DateTime startDate, 
                            double assetPrice,double expiryInYrs, double ir, double vol, 
                            SamplingTypeE samplingType, FreqE samplingFreq, OptionTypeE optionType,
                            int noOfPaths, SdeTypeE underlyingAssetSde, double fixedStrike)
            {
                return new LookbackMcFixedStrikePricer(tradeId, startDate, assetPrice, 
                            expiryInYrs, ir, vol, samplingType, samplingFreq, optionType,
                            noOfPaths, underlyingAssetSde, fixedStrike);
            }
            public static IPricer CreateLookbackFloatStrikeMcPricer(
                            string tradeId, DateTime startDate, double assetPrice,
                            double expiryInYrs, double ir, double vol, 
                            SamplingTypeE samplingType, FreqE samplingFreq, OptionTypeE optionType,
                            int noOfPaths, SdeTypeE underlyingAssetSde)
            {
                return new LookbackMcFloatStrikePricer(tradeId, startDate, assetPrice, 
                                    expiryInYrs, ir, vol, samplingType, samplingFreq, optionType,
                                    noOfPaths, underlyingAssetSde);
            }
        }
    }

    public abstract class OptionPricer : PortfolioPricer,IOptionPricer
    {
        public INumericalMethod NumericalMethod { get; protected set; }
        public double Price { get; protected set; }        
        public DateTime StartDate { get; private set; }

        public double TimeToExpiryInYrs { get; protected set; }
        public double Ir { get; protected set; }
        public double Vol { get; protected set; }
        public double Strike { get; set; }

        public SamplingTypeE SamplingType { get; private set; }
        public FreqE SamplingFreq { get; private set; }
        public OptionTypeE OptionType { get; private set; }
        public double Df { get; protected set; }

        protected OptionPricer(string tradeId, DateTime startDate,
            double price, double timeToExpiryInYrs,
            double ir, double vol, SamplingTypeE samplingType,
            FreqE samplingFreq, OptionTypeE optionType, double fixedStrike)
            :base(tradeId)
        {
            TradeId = tradeId;
            StartDate = startDate;
            Price = price;
            TimeToExpiryInYrs = timeToExpiryInYrs;
            Ir = ir;
            Vol = vol;
            SamplingType = samplingType;
            Strike = fixedStrike;
            OptionType = optionType;
            Df = Math.Exp(-Ir * TimeToExpiryInYrs);

            SamplingFreq = samplingFreq;
                
        }
        public override double CalcPv()
        {
            double payoff = NumericalMethod.Simulate();
            PV = payoff * Df;
            return PV;
        }
    }

    public class AsianOptionMcFixedStrikePricer : OptionPricer
    {                
        internal AsianOptionMcFixedStrikePricer(string tradeId, DateTime startDate, double price,
            double expiryInYrs, double ir, double vol, SamplingTypeE samplingType, FreqE samplingFreq, OptionTypeE optionType,
            int noOfPaths, SdeTypeE underlyingSdeType, double fixedStrike)
            : base(tradeId, startDate, price, expiryInYrs, ir, vol, samplingType, samplingFreq, optionType, fixedStrike)
        {
            if (!(samplingType == SamplingTypeE.DiscreteArithAv || samplingType == SamplingTypeE.DiscreteGeoAv))
                throw new Exception("Invalid samplingType");

            NumericalMethod = new MonteCarloFixedStrike(this, underlyingSdeType, noOfPaths, optionType, fixedStrike);
        }

        public override string Summarize(bool onlyResults)
        {

            ResultTable.Add(string.Format("{0},{1},{2}", TradeId, PV, RunTime));

            var sb = new StringBuilder();

            if (onlyResults)
            {
                sb.AppendFormat("\tTradeId : {0}\n\t\tNoOfPaths : {1}\n\t\tStrike : {2}\n",
                                TradeId, ((MonteCarloFixedStrike)NumericalMethod).NoOfPaths, Strike);
            }
            else
            {
                sb.AppendFormat("\tTradeId : {0}\n\t\tStartDate : {1}\n\t\tExpiry : {2} yrs\n\t\tAssetPrice : {3}\n",
                                TradeId, StartDate.ToString("dd-MMM-yyyy"), TimeToExpiryInYrs, Price);
                sb.AppendFormat("\t\tIR : {0}\n\t\tVol : {1}\n\t\tSamplingType : {2}\n\t\tSamplingFreq : {3}\n",
                                    Ir, Vol, SamplingType, SamplingFreq);
                sb.AppendFormat("\t\tOptionType : {0}\n\t\tNoOfPaths : {1}\n\t\tUnderlyingSdeType : {2}\n\t\tStrike : {3}\n",
                                    OptionType, ((MonteCarloFixedStrike)NumericalMethod).NoOfPaths, NumericalMethod.UnderlyingSdeType, Strike);               
            }

            sb.AppendFormat("\n\t\tDf : {0}\n\t\tTime : {1} (sec)\n\t\tPV : {2}\n",Df, RunTime, PV);    

            return sb.ToString();
        }        
    }

    public class AsianOptionMcFloatStrikePricer : OptionPricer
    {
        internal AsianOptionMcFloatStrikePricer(string tradeId, DateTime startDate, double price,
            double expiryInYrs, double ir, double vol, SamplingTypeE samplingType, FreqE samplingFreq, OptionTypeE optionType,
            int noOfPaths, SdeTypeE underlyingSdeType)
            : base(tradeId, startDate, price, expiryInYrs, ir, vol, samplingType, samplingFreq, optionType, 0)
        {
            if (samplingType != SamplingTypeE.DiscreteArithAv && samplingType != SamplingTypeE.DiscreteGeoAv)
                throw new Exception("Invalid samplingType");

            NumericalMethod = new MonteCarloFloatingStrike(this, underlyingSdeType, noOfPaths, optionType);
        }

        public override string Summarize(bool onlyResults)
        {
            ResultTable.Add(string.Format("{0},{1},{2}", TradeId, PV, RunTime));

            var sb = new StringBuilder();

            if (onlyResults)
            {
                sb.AppendFormat("\tTradeId : {0}\n\t\tNoOfPaths : {1}\n\t\tStrike : {2}\n",
                                TradeId, ((MonteCarloFloatingStrike)NumericalMethod).NoOfPaths, Strike);
            }
            else
            {
                sb.AppendFormat("\tTradeId : {0}\n\t\tStartDate : {1}\n\t\tExpiry : {2} yrs\n\t\tAssetPrice : {3}\n",
                                TradeId, StartDate.ToString("dd-MMM-yyyy"), TimeToExpiryInYrs, Price);
                sb.AppendFormat("\t\tIR : {0}\n\t\tVol : {1}\n\t\tSamplingType : {2}\n\t\tSamplingFreq : {3}\n",
                                    Ir, Vol, SamplingType, SamplingFreq);
                sb.AppendFormat("\t\tOptionType : {0}\n\t\tNoOfPaths : {1}\n\t\tUnderlyingSdeType : {2}\n\t\tStrike : {3}\n",
                                    OptionType, ((MonteCarloFloatingStrike)NumericalMethod).NoOfPaths, NumericalMethod.UnderlyingSdeType, Strike);
            }

            sb.AppendFormat("\n\t\tDf : {0}\n\t\tTime : {1} (sec)\n\t\tPV : {2}\n", Df, RunTime, PV);

            return sb.ToString();
        }
    }

    public abstract class AsianClosedFormPricer : OptionPricer
    {
        internal AsianClosedFormPricer(string tradeId, DateTime startDate, double price,
                                    double expiryInYrs, double ir, double vol, 
                                    OptionTypeE optionType, double fixedStrike)
            : base(tradeId, startDate, price, expiryInYrs, ir, vol, SamplingTypeE.NoSampling, FreqE.Daily, optionType, fixedStrike)
        {                                    
        }

        public override string Summarize(bool onlyResults)
        {
            ResultTable.Add(string.Format("{0},{1},{2}", TradeId, PV, RunTime));

            var sb = new StringBuilder();
            
            if (onlyResults)
            {
                sb.AppendFormat("\tTradeId : {0}\n\t\tStrike : {1}\n",
                                TradeId, Strike);
            }
            else
            {
                sb.AppendFormat("\tTradeId : {0}\n\t\tStartDate : {1}\n\t\tExpiry : {2} yrs\n\t\tAssetPrice : {3}\n",
                                TradeId, StartDate.ToString("dd-MMM-yyyy"), TimeToExpiryInYrs, Price);
                sb.AppendFormat("\t\tIR : {0}\n\t\tVol : {1}\n\t\tSamplingType : {2}\n\t\tSamplingFreq : {3}\n",
                                    Ir, Vol, SamplingType, SamplingFreq);
                sb.AppendFormat("\t\tOptionType : {0}\n\t\tStrike : {1}\n",
                                    OptionType, Strike);
            }

            sb.AppendFormat("\n\t\tDf : {0}\n\t\tTime : {1} (sec)\n\t\tPV : {2}\n", Df, RunTime, PV);     

            return sb.ToString();
        }  

    }
    public class AsianCfGeoAvFixedStrikePricer : AsianClosedFormPricer
    {
        internal AsianCfGeoAvFixedStrikePricer(string tradeId, DateTime startDate, double price,
                                double expiryInYrs, double ir, double vol, 
                                OptionTypeE optionType, double fixedStrike)
            : base(tradeId, startDate, price, expiryInYrs, ir, vol,
                optionType, fixedStrike)
        {                        
        }
        public override double CalcPv()
        {
            double K = Strike;            
            double T = TimeToExpiryInYrs;

            //asuming t=0            
            var d1 = (-Math.Log(K) +                                             
                        (Ir - Vol * Vol / 2) * T / 2 +
                        Math.Log(Price)
                      ) /
                      (Vol * Math.Sqrt(T / 3));            

            if (OptionType == OptionTypeE.Call)
            {
                PV = (  Price *
                        Math.Exp((Ir - Vol * Vol / 2) * T / 2) *
                        Math.Exp(Vol * Vol * T / 6) *
                        Normal.CDF(0, 1, ( d1 + Vol / T * Math.Sqrt(Math.Pow(T, 3) / 3))) -
                        K * Normal.CDF(0, 1, d1)
                    ) * Df;   
            }
            else
            {
                PV = (  -1 * Price *
                        Math.Exp((Ir - Vol * Vol / 2) * T / 2) *
                        Math.Exp(Vol * Vol * T / 6) *
                        Normal.CDF(0, 1, ( -1 * d1 - Vol / T * Math.Sqrt(Math.Pow(T, 3) / 3))) +
                        K * Normal.CDF(0, 1, -1 * d1)
                    ) * Df;
            }
            
            return PV;
        }

    }
    public class AsianCfGeoAvFloatStrikePricer : AsianClosedFormPricer
    {
        internal AsianCfGeoAvFloatStrikePricer(string tradeId, DateTime startDate, double price,
                                double expiryInYrs, double ir, double vol, 
                                OptionTypeE optionType)
            : base(tradeId, startDate, price, expiryInYrs, ir, vol,
                optionType, 0)
        {            
            
        }
        public override double CalcPv()
        {                                             
            double T = TimeToExpiryInYrs;                     

            //asuming t=0            
            var d1 = ((Ir + Vol * Vol / 2) * T * T / 2) / (Vol * Math.Sqrt(Math.Pow(T, 3) / 3));
            var d2 = d1 - Vol * Math.Sqrt(Math.Pow(T, 3) / 3);

            if (OptionType == OptionTypeE.Call)
            {
                PV = Price * (
                    Normal.CDF(0, 1, d1) -
                    Math.Exp((-1 * (Ir + Vol * Vol / 2) * T / 2) + Vol * Vol/ 6 * T) *
                    Normal.CDF(0, 1, d2));
            }
            else
            {
                PV = -1 * Price * (
                    Normal.CDF(0, 1, -1 * d1) -
                    Math.Exp((-1 * (Ir + Vol * Vol / 2) * T / 2) + Vol * Vol / 6 * T) *
                    Normal.CDF(0, 1, -1 * d2));
            }            
            return PV;
        }
    }

    public class EurOptionMcPricer : OptionPricer
    {
        internal EurOptionMcPricer(string tradeId, DateTime startDate, double price,
            double expiryInYrs, double ir, double vol, OptionTypeE optionType,
            int noOfPaths, SdeTypeE underlyingSdeType, double fixedStrike)
            : base(tradeId, startDate, price, expiryInYrs, ir, vol, SamplingTypeE.NoSampling, FreqE.Daily, optionType, fixedStrike)
        {
            NumericalMethod = new MonteCarloNoSampling(this, underlyingSdeType, noOfPaths, optionType, fixedStrike);
        }

        public override string Summarize(bool onlyResults)
        {
            ResultTable.Add(string.Format("{0},{1},{2}", TradeId, PV, RunTime));

            var sb = new StringBuilder();

            if (onlyResults)
            {
                sb.AppendFormat("\tTradeId : {0}\n\t\tNoOfPaths : {1}\n\n\t\tStrike : {2}\n",
                                TradeId, ((MonteCarloNoSampling)NumericalMethod).NoOfPaths, Strike);
            }
            else
            {
                sb.AppendFormat("\tTradeId : {0}\n\t\tStartDate : {1}\n\t\tExpiry : {2} yrs\n\t\tAssetPrice : {3}\n",
                                TradeId, StartDate.ToString("dd-MMM-yyyy"), TimeToExpiryInYrs, Price);
                sb.AppendFormat("\t\tIR : {0}\n\t\tVol : {1}\n",
                                    Ir, Vol);
                sb.AppendFormat("\t\tOptionType : {0}\n\t\tNoOfPaths : {1}\n\t\tUnderlyingSdeType : {2}\n\t\tStrike : {3}\n",
                                    OptionType, ((MonteCarloNoSampling)NumericalMethod).NoOfPaths, NumericalMethod.UnderlyingSdeType, Strike);
            }

            sb.AppendFormat("\n\t\tDf : {0}\n\t\tTime : {1} (sec)\n\t\tPV : {2}\n", Df, RunTime, PV);        

            return sb.ToString();
        }   
    }

    public abstract class LookbackMcPricer : OptionPricer
    {
        internal LookbackMcPricer(string tradeId, DateTime startDate, double price,
            double expiryInYrs, double ir, double vol, SamplingTypeE samplingType, FreqE samplingFreq,OptionTypeE optionType,
            double fixedStrike = 0)
            : base(tradeId, startDate, price, expiryInYrs, ir, vol, samplingType, samplingFreq, optionType, fixedStrike)
        {            
        }

        public override string Summarize(bool onlyResults)
        {

            ResultTable.Add(string.Format("{0},{1},{2}", TradeId, PV, RunTime));

            var sb = new StringBuilder();

            if (onlyResults)
            {
                sb.AppendFormat("\tTradeId : {0}\n\t\tNoOfPaths : {1}\n\t\tStrike : {2}\n",
                                TradeId, ((MonteCarlo)NumericalMethod).NoOfPaths, Strike);
            }
            else
            {
                sb.AppendFormat("\tTradeId : {0}\n\t\tStartDate : {1}\n\t\tExpiry : {2} yrs\n\t\tAssetPrice : {3}\n",
                                TradeId, StartDate.ToString("dd-MMM-yyyy"), TimeToExpiryInYrs, Price);
                sb.AppendFormat("\t\tIR : {0}\n\t\tVol : {1}\n\t\tSamplingType : {2}\n\t\tSamplingFreq : {3}\n",
                                    Ir, Vol, SamplingType, SamplingFreq);
                sb.AppendFormat("\t\tOptionType : {0}\n\t\tNoOfPaths : {1}\n\t\tUnderlyingSdeType : {2}\n\t\tStrike : {3}\n",
                                    OptionType, ((MonteCarlo)NumericalMethod).NoOfPaths, NumericalMethod.UnderlyingSdeType, Strike);
            }

            sb.AppendFormat("\n\t\tDf : {0}\n\t\tTime : {1} (sec)\n\t\tPV : {2}\n", Df, RunTime, PV);

            return sb.ToString();
        }
    }
    public class LookbackMcFixedStrikePricer : LookbackMcPricer
    {
        internal LookbackMcFixedStrikePricer(string tradeId, DateTime startDate, double price,
            double expiryInYrs, double ir, double vol, SamplingTypeE samplingType, FreqE samplingFreq,
            OptionTypeE optionType,int noOfPaths, SdeTypeE underlyingSdeType, double fixedStrike)
            : base(tradeId, startDate, price, expiryInYrs, ir, vol, samplingType, samplingFreq, optionType, fixedStrike)
        {
            if (samplingType != SamplingTypeE.DiscreteMin && samplingType != SamplingTypeE.DiscreteMax)
                throw new Exception("Invalid samplingType");

            NumericalMethod = new MonteCarloFixedStrike(this, underlyingSdeType, noOfPaths, optionType, fixedStrike);
        }         
    }

    public class LookbackMcFloatStrikePricer : LookbackMcPricer
    {
        internal LookbackMcFloatStrikePricer(string tradeId, DateTime startDate, double price,
            double expiryInYrs, double ir, double vol, SamplingTypeE samplingType, FreqE samplingFreq, 
            OptionTypeE optionType, int noOfPaths, SdeTypeE underlyingSdeType)
            : base(tradeId, startDate, price, expiryInYrs, ir, vol, samplingType, samplingFreq, optionType, 0)
        {
            if (samplingType != SamplingTypeE.DiscreteMin && samplingType != SamplingTypeE.DiscreteMax)
                throw new Exception("Invalid samplingType");

            NumericalMethod = new MonteCarloFloatingStrike(this, underlyingSdeType, noOfPaths, optionType);
        }
    }

    #endregion PRICER
    
    #region SAMPLING

    internal abstract class Sampling : ISampling
    {
        protected int _counter;
        public double StateVariable { get; protected set; }
        
        public abstract void AddSample(double assetPrice);        
        public abstract double ApplyUpdateRule();
        
        public static class Factory
        {            
            public static ISampling Create(SamplingTypeE samplingType)
            {                                
                var samplingObj = (Sampling)Assembly.GetExecutingAssembly().CreateInstance("EqPricer." + samplingType);
                Debug.Assert(samplingObj != null, "SamplingObj " + samplingType + " is null");                
                return samplingObj;
            }
        }
    }

    internal class NoSampling : Sampling
    {    
        public override void AddSample(double assetPrice)
        {
        }

        public override double ApplyUpdateRule()
        {
            return StateVariable;
        }
    }
    internal class DiscreteArithAv : Sampling
    {
        public override void AddSample(double assetPrice)
        {
            StateVariable += assetPrice;
            _counter += 1;
        }

        public override double ApplyUpdateRule()
        {         
            return StateVariable / _counter;
        }
    }
    
    internal class DiscreteGeoAv : Sampling
    {                
        public override void AddSample(double assetPrice)
        {            
            StateVariable += Math.Log(assetPrice);
            _counter += 1;
        }
        public override double ApplyUpdateRule()
        {
            return Math.Exp(StateVariable / _counter);
        }
    }

    internal class DiscreteMax : Sampling
    {
        public override void AddSample(double assetPrice)
        {
            if (_counter == 0)
            {
                StateVariable = assetPrice;
                _counter += 1;
            }
            else
            {
                StateVariable = Math.Max(StateVariable, assetPrice);
            }            
        }

        public override double ApplyUpdateRule()
        {            
            return StateVariable;
        }
    }
    internal class DiscreteMin : Sampling
    {
        public override void AddSample(double assetPrice)
        {
            if (_counter == 0)
            {
                StateVariable = assetPrice;
                _counter += 1;
            }
            else
            {
                StateVariable = Math.Min(StateVariable, assetPrice);
            }
        }

        public override double ApplyUpdateRule()
        {            
            return StateVariable;
        }
    }
    
    #endregion SAMPLING

    #region SDE

    /// <summary>
    /// force random instance to be thread specific, otherwise getting the same numbers if no of threads increase
    /// https://stackoverflow.com/questions/767999/random-number-generator-only-generating-one-random-number    
    /// </summary>
    public static class StaticRandom
    {
        private static int _seed;

        private static readonly ThreadLocal<Random> ThreadLocal = new ThreadLocal<Random>
            (() => new Random(Interlocked.Increment(ref _seed)));

        static StaticRandom()
        {
            //_seed = Environment.TickCount;
            _seed = MathNet.Numerics.Random.RandomSeed.Robust();
        }

        public static Random Instance { get { return ThreadLocal.Value; } }
    }

    internal abstract class RandomWalk : ISde
    {        
        public abstract double EvaluateAtT(double prevAssetPrice, double normStdRandom);

        /// <summary>
        /// Box-Muller
        /// </summary>
        /// <returns>std normal random value</returns>
        public static double GetStdNormRnd()
        {            
            double u1 = 1.0 - StaticRandom.Instance.NextDouble();
            double u2 = 1.0 - StaticRandom.Instance.NextDouble();
            double random = Math.Sqrt(-2.0 * Math.Log(u2)) *
                         Math.Cos(2.0 * Math.PI * u1);

            //var random = (VBMath.Rnd() + VBMath.Rnd() + VBMath.Rnd() + VBMath.Rnd()
            //     + VBMath.Rnd() + VBMath.Rnd() + VBMath.Rnd() + VBMath.Rnd()
            //     + VBMath.Rnd() + VBMath.Rnd() + VBMath.Rnd() + VBMath.Rnd() - 6);
            
            return random;
        }

        /// <summary>
        /// Box-Muller generation
        /// </summary>
        /// <param name="count"></param>
        /// <returns>list of n(0,1) values</returns>
        public static List<double> GetStdNormRandoms(int count)
        {
            //return StRnds(count);

            //these gives much better results and about 12% faster
            return AntitheticRnd(count);
        }

        /// <summary>
        /// Box-Muller generation. The StaticRandom is not working as expected when 
        /// hit with a higerh level of concurrency, the workaround is to get the full list 
        /// of numbers in a single call and then get them from the collection (index wise)        
        /// </summary>
        /// <returns>list of n(0,1) values</returns>
        static List<double> StRnds(int count)
        {
            var rndList = new List<double>();

            for (int i = 0; i < count; i++)
            {
                double u1 = 1.0 - StaticRandom.Instance.NextDouble();
                double u2 = 1.0 - StaticRandom.Instance.NextDouble();
                double random = Math.Sqrt(-2.0 * Math.Log(u1)) *
                             Math.Sin(2.0 * Math.PI * u2); 

                rndList.Add(random);
            }
            return rndList;
        }

        /// <summary>
        /// Box-Muller generation. The StaticRandom is not working as expected when 
        /// hit with a higerh level of concurrency, the workaround is to get the full list 
        /// of numbers in a single call and then get them from the collection (index wise)
        /// 8.3.1 anti-thetic approach : storing half the set and return the -ve for the 
        /// other half
        /// </summary>
        /// <returns>list of n(0,1) values</returns>
        static List<double> AntitheticRnd(int count)
        {
            var rndList = new List<double>();

            //keep an extra value to cater for odd counts
            var half = count/2 + count%2;

            for (int i = 0; i < half; i++)
            {
                double u1 = 1.0 - StaticRandom.Instance.NextDouble();
                double u2 = 1.0 - StaticRandom.Instance.NextDouble();
                double random = Math.Sqrt(-2.0 * Math.Log(u1)) *
                             Math.Sin(2.0 * Math.PI * u2);

                rndList.Add(random);
            }
            for (int i = 0; i < half; i++)            
                rndList.Add(-1 * rndList[i]);
            
            return rndList;
        }

        public static class Factory
        {
            public static ISde Create(SdeTypeE sdeType,double prevAssetPrice, double drift, double vol,double expiryInYears, double timeStepInYrs=0)
            {
                if(sdeType == SdeTypeE.GbmClosedForm)
                    return new GbmClosedForm(prevAssetPrice, drift, vol, expiryInYears);

                return new Gbm(prevAssetPrice, drift, timeStepInYrs, vol, expiryInYears);                                
            }            
        }
    }

    /// <summary>
    /// time stepping sde for lognormal random walk
    /// </summary>
    internal class Gbm : RandomWalk
    {
        public double PrevAssetPrice { get; private set; }
        public double Drift { get; private set; } //will be the IR in risk neutral world
        public double TimeStepInYrs { get; private set; }
        public double ExpiryInYrs { get; private set; }
        public double Vol { get; private set; }

        internal Gbm(double prevAssetPrice, double drift, double timeStepInYrs, double vol, double expiryInYears)
        {
            PrevAssetPrice = prevAssetPrice;
            Drift = drift;
            TimeStepInYrs = timeStepInYrs;
            ExpiryInYrs = expiryInYears;
            Vol = vol;
        }

        public override double EvaluateAtT(double prevAssetPrice, double normStdRandom)
        {
            var newPrice = prevAssetPrice * (1 + Drift * TimeStepInYrs + Vol * Math.Sqrt(TimeStepInYrs) * normStdRandom);             
            return newPrice;
        }        
    }
    
    /// <summary>
    /// can only be used for path-independent products like european options
    /// </summary>
    internal class GbmClosedForm : Gbm
    {
        internal GbmClosedForm(double prevAssetPrice, double drift, double vol, double expiryInYears)
            : base(prevAssetPrice, drift, expiryInYears, vol, expiryInYears)
        {
        }
        
        public override double EvaluateAtT(double prevAssetPrice, double normStdRandom)
        {
            return prevAssetPrice * Math.Exp((Drift - 0.5 * Vol * Vol) * TimeStepInYrs + Vol * Math.Sqrt(TimeStepInYrs) * normStdRandom);
        }
    }
    #endregion SDE

    #region NUMERICAL METHODS

    internal abstract class NumericalMethod : INumericalMethod
    {
        public IOptionPricer AssetPricer { get; private set; }
        public SdeTypeE UnderlyingSdeType { get; private set; }
        public OptionTypeE OptionType { get; private set; }

        protected NumericalMethod(IOptionPricer assetPricer, SdeTypeE underlyingSdeType, OptionTypeE optionType)
        {
            AssetPricer = assetPricer;
            UnderlyingSdeType = underlyingSdeType;
            OptionType = optionType;
        }

        public abstract double Simulate();
        public abstract double CalcPayOff(double priceAtExpiry, ISampling samplingObj);        
    }

    internal abstract class MonteCarlo : NumericalMethod,IMonteCarlo
    {                
        public int NoOfPaths { get; private set; }                
        public double Strike { get; private set; }
        public double TimeStepInYrs { get; private set; }        
        public int NoOfTs { get; private set; }

        internal MonteCarlo(IOptionPricer pricer, SdeTypeE underlyingSdeType, int noOfPaths, OptionTypeE optionType, double strike = 0) 
            : base(pricer, underlyingSdeType, optionType)
        {            
            NoOfPaths = noOfPaths;
            TimeStepInYrs = 1.0 / 252;            
            NoOfTs = GetNoOfTs(AssetPricer.TimeToExpiryInYrs,FreqE.Daily);                        
            Strike = strike;
        }        

        public static DateTime GetNextDate(DateTime start, double noOfYrs)
        {
            return start.AddDays(noOfYrs * 252);
        }

        public static List<DateTime> GetSchedule(DateTime startDate, double timeToExpiryInYrs, FreqE freq)
        {
            var sched = new List<DateTime>();
            var noOfTs = GetNoOfTs(timeToExpiryInYrs, freq);            
            double timeStepInYrs =GetTs(freq);

            //exclude start and expiry date
            for (int tsCount = 0; tsCount < noOfTs; tsCount++)
            {
                sched.Add(GetNextDate(startDate, tsCount * timeStepInYrs));
            }
            return sched;
        }

        public static int GetNoOfTs(double timeToExpiryInYrs, FreqE freq)
        {
            double timeStepInYrs =GetTs(freq);
            //return (int)(timeToExpiryInYrs / timeStepInYrs) + ((timeToExpiryInYrs % timeStepInYrs) > timeStepInYrs ? 1 : 0);
            return (int)(timeToExpiryInYrs / timeStepInYrs);
        }

        public static double GetTs(FreqE freq)
        {
            double timeStepInYrs;
            const double noOfDaysInYr = 252.0;

            switch (freq)
            {
                case FreqE.Daily:
                    timeStepInYrs = 1 / noOfDaysInYr;
                    break;
                case FreqE.Weekly:
                    timeStepInYrs = 5 / noOfDaysInYr;
                    break;
                case FreqE.Monthly:
                    timeStepInYrs = 1.0 / 12;
                    break;
                case FreqE.Quarterly:
                    timeStepInYrs = 0.25;
                    break;
                case FreqE.SemiAnually:
                    timeStepInYrs = 0.5;
                    break;
                case FreqE.Anually:
                    timeStepInYrs = 1;
                    break;
                default:
                    throw new Exception("Invalid frequency : " + freq);
            }
            return timeStepInYrs;
        }

        public override double Simulate()
        {
            double sum = 0;
            var obj = new object();

            var samplingDates = GetSchedule(AssetPricer.StartDate, AssetPricer.TimeToExpiryInYrs, AssetPricer.SamplingFreq);
            var normalStdRandoms = RandomWalk.GetStdNormRandoms(NoOfTs * NoOfPaths).ToList();

            //var mean = normalStdRandoms.Average();
            //var stdDev = normalStdRandoms.StandardDeviation();
            //EqLogger.Log.Info("mean-stdDev : [ " + mean + " : " + stdDev + " ]");
            
            Parallel.ForEach(Partitioner.Create(0, NoOfPaths), //divides the workload into baskets. Each basket will be run concurrently while the contents run synchronously
                    () => 0.0, //intitialize the threadlocal area for each partition
                    (range, state, partial) => //range is populated internally as inclusive_from-to-exclusive_to blocks like 1-51,51-101 etc,state gives option to name the thread state(not used) and partial is thread local storage of each basket
                    {
                        //move this out from the loop as a higher level of concurrency results in same set of 
                        //random nos being returned by the generator
                        //double[] normalStdRandoms = RandomWalk.GetStdNormRandoms(NoOfTs).ToArray();                                                

                        //run the basket synchronously
                        for (int i = range.Item1; i < range.Item2; i++)
                        {
                            double assetPrice = AssetPricer.Price;

                            var underlyingSde = RandomWalk.Factory.Create(UnderlyingSdeType,
                                                            AssetPricer.Price,
                                                            AssetPricer.Ir,
                                                            AssetPricer.Vol,
                                                            AssetPricer.TimeToExpiryInYrs,
                                                            TimeStepInYrs);

                            var samplingObj = Sampling.Factory.Create(AssetPricer.SamplingType);

                            for (int tsCount = 0; tsCount < NoOfTs; tsCount++)
                            {
                                assetPrice = underlyingSde.EvaluateAtT(assetPrice, normalStdRandoms[i * NoOfTs + tsCount]);

                                //EqLogger.Log.Info(assetPrice);

                                DateTime dt = GetNextDate(AssetPricer.StartDate, TimeStepInYrs * tsCount);

                                if(samplingDates.Contains(dt))
                                    samplingObj.AddSample(assetPrice);
                            }

                            double payoff = CalcPayOff(assetPrice, samplingObj);

                            partial += payoff;                            
                        }
                        
                        return partial;
                    },
                    partial =>
                    {
                        lock (obj)
                        {
                            sum += partial;
                        }
                    }
            );

            //the synchronous version
            /*for (int j = 0; j < NoOfPaths; j++)
            {
                double[] normalStdRandoms = RandomWalk.GetStdNormRandoms(NoOfTs).ToArray();

                //var mean = normalStdRandoms.Average();
                //var stdDev = normalStdRandoms.StandardDeviation();

                //EqLogger.Log.Info(mean + " : " + stdDev);

                //for (int i = 0; i < NoOfTs; i++)
                {
                    double assetPrice = AssetPricer.Price;

                    var underlyingSde = RandomWalk.Factory.Create(UnderlyingSdeType,
                        AssetPricer.Price,
                        AssetPricer.Ir,
                        AssetPricer.Vol,
                        AssetPricer.TimeToExpiryInYrs,
                        TimeStepInYrs);

                    var samplingObj = Sampling.Factory.Create(AssetPricer.SamplingType,
                        samplingDates);

                    for (int tsCount = 0; tsCount < NoOfTs; tsCount++)
                    {
                        assetPrice = underlyingSde.EvaluateAtT(assetPrice, normalStdRandoms[tsCount]);
                        DateTime dt = GetNextDate(AssetPricer.StartDate, TimeStepInYrs*tsCount);
                        samplingObj.AddSample(assetPrice, dt);
                    }

                    double payoff = CalcPayOff(assetPrice, samplingObj);

                    sum += payoff;
                }
            }*/
            
            //cleanup the memory
            normalStdRandoms.Clear();

            return sum / NoOfPaths;
        }
    }

    internal class MonteCarloNoSampling : MonteCarlo
    {
        internal MonteCarloNoSampling(IOptionPricer pricer, SdeTypeE underlyingSdeType, int noOfPaths, 
            OptionTypeE optionType, double strike)
            : base(pricer, underlyingSdeType, noOfPaths, optionType, strike)
        {
        }

        public override double CalcPayOff(double priceAtExpiry, ISampling samplingObj)
        {            
            double payoff = priceAtExpiry - Strike;

            if (OptionType == OptionTypeE.Call)
                return Math.Max(payoff, 0);

            return Math.Max(-1 * payoff, 0);
        }
    }

    internal class MonteCarloFixedStrike : MonteCarlo
    {
        internal MonteCarloFixedStrike(IOptionPricer pricer, SdeTypeE underlyingSdeType, int noOfPaths, OptionTypeE optionType, double strike)
            : base(pricer, underlyingSdeType, noOfPaths, optionType, strike)
        {
        }

        public override double CalcPayOff(double priceAtExpiry, ISampling samplingObj)
        {
            double A = samplingObj.ApplyUpdateRule();
            double payoff = A - Strike;

            if (OptionType == OptionTypeE.Call)
                return Math.Max(payoff, 0);

            return Math.Max(-1 * payoff, 0); 
        }        
    }

    internal class MonteCarloFloatingStrike : MonteCarlo
    {
        internal MonteCarloFloatingStrike(IOptionPricer pricer, SdeTypeE underlyingSdeType, int noOfPaths, OptionTypeE optionType)
            : base(pricer, underlyingSdeType, noOfPaths, optionType, 0)
        {
        }

        public override double CalcPayOff(double priceAtExpiry, ISampling samplingObj)
        {
            double strike = samplingObj.ApplyUpdateRule();
            double payoff = priceAtExpiry - strike;

            AssetPricer.Strike = strike;

            if (OptionType == OptionTypeE.Call)
                return Math.Max(payoff, 0);

            return Math.Max(-1 * payoff, 0);
        }
    }    

    #endregion NUMERICAL METHODS   
 
    internal static class EqLogger
    {
        internal static Logger Log = LogManager.GetCurrentClassLogger();
    }
}
