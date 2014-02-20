using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VARSpike
{
    public interface ICodeTimer : IDisposable
    {
        int Count { get; set; }
        void Increment();
    }

    public class CodeTimerConsole : ICodeTimer
    {
        private TimerHighPrecision perf;
        private string name;

        public CodeTimerConsole(string name = null)
        {
            this.name = name;
            perf = new TimerHighPrecision();
            Count = 1;
            perf.Start();
        }

        public int Count { get; set; }
        public void Increment()
        {
            Count++;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            perf.Stop();
            Reporter.WriteLine("[{0}] {1} execution took {2:0.000} sec. {3:0.0000} per {4}", "Timer", name, perf.Duration(1), perf.Duration(Count), Count);
        }


    }

    /// <summary>
    /// Measures time intervals for perfmon
    /// </summary>
    public class TimerHighPrecision
    {
        [DllImport("KERNEL32")]
        private static extern bool QueryPerformanceCounter(
            out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        private long start = 0;
        private long stop = 0;
        private long frequency;

        /// <summary>
        /// Default constructor passing a global variable to hold a value that will be used to calculate a duration in nanoseconds. 
        /// </summary>
        public TimerHighPrecision()
        {
            if (QueryPerformanceFrequency(out frequency) == false)
            {
                // Frequency not supported
                throw new Win32Exception();
            }
        }

        /// <summary>
        /// Start method that gets the current value from QueryPerformanceCounter. Uses a global variable to store the retrieved value. 
        /// </summary>
        public void Start()
        {
            start = 0;
            stop = 0;
            QueryPerformanceCounter(out start);
        }

        /// <summary>
        /// Stop method that gets the current value from QueryPerformanceCounter. Uses a global variable to store the retrieved value. 
        /// </summary>
        public void Stop()
        {
            QueryPerformanceCounter(out stop);
        }

        /// <summary>
        /// TicksDuration method that returns the ticks for all operations
        /// </summary>
        /// <returns></returns>
        public long TicksOperationDuration()
        {
            if (stop == 0) Stop();
            return stop - start;
        }

        /// <summary>
        /// Duration method that accepts the number of iterations as an argument and returns a duration per operation value. 
        /// </summary>
        /// <param name="iterations"></param>
        /// <returns></returns>
        public double Duration(int iterations)
        {
            if (stop == 0) Stop();
            return ((((double)(TicksOperationDuration()) / (double)frequency) / (double)iterations));
        }

        /// <summary>
        /// Number of iterations per second achieved.
        /// </summary>
        /// <param name="iterations"></param>
        /// <returns></returns>
        public double IterationsPerSecond(int iterations)
        {
            if (stop == 0) Stop();
            return (double)iterations / (((double)(TicksOperationDuration()) / (double)frequency));
        }
    }
}
