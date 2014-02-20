using System;
using System.Collections.Generic;
using System.IO;

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
        public bool PlayBack { get; set; }
        public Random Underlying { get; set; }


        public RandomWrapper InitRecord(int seed, int guessSize)
        {
            Underlying = new Random(seed);
            PlayBack = true;
            Record = true;
            names = new List<string>(guessSize);
            sequence = new List<double>(guessSize);

            return this;
        }
        
        
        private List<double> sequence;
        private List<string> names; 
        private int count;

        public double NextDouble()
        {
            if (PlayBack)
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
            if (PlayBack)
            {
                if (Record)
                {
                    var next = Underlying.NextDouble();
                    count++;
                    sequence.Add(next);
                    if (names != null) names.Add(name);
                    return next;

                }
                return sequence[count++];    
            }

            return Underlying.NextDouble();

        }

        public void SaveFixed(string filename)
        {
            using (var fs = new StreamWriter(filename, false))
            {
                for (int cc = 0; cc < sequence.Count; cc++)
                {
                    fs.WriteLine("{0}, {1}, {2}", cc, names[cc], sequence[cc]);
                }    
            }
            
        }
    }
}