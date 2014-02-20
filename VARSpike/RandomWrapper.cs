using System;
using System.Collections.Generic;

namespace VARSpike
{

    public enum ReturnType
    {
        Classic,
        Log
    }

    public class RandomWrapper
    {
        public RandomWrapper()
        {
            Underlying = new Random(1);
        }

        public bool Record { get; set; }
        public bool UseFixed { get; set; }
        public Random Underlying { get; set; }
        
        
        private List<double> sequence;
        private List<string> names; 
        private int count;

        public double NextDouble()
        {
            if (UseFixed)
            {
                if (Record)
                {
                    return NextDouble(count.ToString());
                }
                else
                {
                    return sequence[count++];
                }
            }
            return Underlying.NextDouble();
        }

        public double NextDouble(string name)
        {
            if (UseFixed)
            {
                if (Record)
                {

                    var next = Underlying.NextDouble();
                    sequence.Add(next);
                    if (names != null) names.Add(name);
                    return next;

                }
                return sequence[count++];    
            }

            return Underlying.NextDouble();

        }
    }
}