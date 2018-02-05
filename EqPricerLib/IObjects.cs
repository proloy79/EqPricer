using System;
using System.Collections.Generic;

namespace EqPricerLib
{    
    public interface ISampling
    {        
        double StateVariable { get; }        
        void AddSample(double assetPrice);
        double ApplyUpdateRule();
    }
    
    public interface ISde
    {        
        double EvaluateAtT(double prevAssetPrice, double normStdRandom);
    }

    public interface IPricer
    {
        string TradeId { get; }
        double PV { get; }
        
        double CalcPv();
        string Summarize(bool onlyResults);
        List<string> ResultTable { get; }
        double RunTime { get; }
    }

    public interface IOptionPricer : IPricer
    {
        double Price { get; } //asset price at start        
        DateTime StartDate { get; }
        double TimeToExpiryInYrs { get; }
        double Ir { get; }
        double Df { get; }        
        double Vol { get; }
        double Strike { get; set; }
        SamplingTypeE SamplingType { get; }
        FreqE SamplingFreq { get; }
        OptionTypeE OptionType { get; }
        INumericalMethod NumericalMethod { get; } 
    }

    public interface INumericalMethod
    {
        IOptionPricer AssetPricer { get; }
        SdeTypeE UnderlyingSdeType { get; }
        OptionTypeE OptionType { get; }
        double Simulate();
        double CalcPayOff(double priceAtExpiry, ISampling samplingObj);
    }

    public interface IMonteCarlo : INumericalMethod
    {                
        int NoOfPaths { get; }        
        double Strike { get; }     
    }

    public interface IFiniteDifference : INumericalMethod
    {
        //IPde Pde { get; }

    }

}
