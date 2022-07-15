using System;
using System.IO;
using ConditionalAttribute = System.Diagnostics.ConditionalAttribute;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Mirage.Weaver
{
    internal class WeaverDiagnosticsTimer
    {
        public bool writeToFile;
        private StreamWriter writer;
        private Stopwatch stopwatch;
        private string name;

        public long ElapsedMilliseconds => this.stopwatch?.ElapsedMilliseconds ?? 0;

        ~WeaverDiagnosticsTimer()
        {
            this.writer?.Dispose();
            this.writer = null;
        }

        [Conditional("WEAVER_DEBUG_TIMER")]
        public void Start(string name)
        {
            this.name = name;

            if (this.writeToFile)
            {
                var path = $"./Build/WeaverLogs/Timer_{name}.log";
                try
                {
                    this.writer = new StreamWriter(path)
                    {
                        AutoFlush = true,
                    };
                }
                catch (Exception e)
                {
                    this.writer?.Dispose();
                    this.writeToFile = false;
                    this.WriteLine($"Failed to open {path}: {e}");
                }
            }

            this.stopwatch = Stopwatch.StartNew();

            this.WriteLine($"Weave Started - {name}");
#if WEAVER_DEBUG_LOGS
            WriteLine($"Debug logs enabled");
#else
            this.WriteLine($"Debug logs disabled");
#endif 
        }

        [Conditional("WEAVER_DEBUG_TIMER")]
        private void WriteLine(string msg)
        {
            var fullMsg = $"[WeaverDiagnostics] {msg}";
            Console.WriteLine(fullMsg);
            if (this.writeToFile)
            {
                this.writer.WriteLine(fullMsg);
            }
        }

        public long End()
        {
            this.WriteLine($"Weave Finished: {this.ElapsedMilliseconds}ms - {this.name}");
            this.stopwatch?.Stop();
            this.writer?.Close();
            return this.ElapsedMilliseconds;
        }

        public SampleScope Sample(string label)
        {
            return new SampleScope(this, label);
        }

        public struct SampleScope : IDisposable
        {
            private readonly WeaverDiagnosticsTimer timer;
            private readonly long start;
            private readonly string label;

            public SampleScope(WeaverDiagnosticsTimer timer, string label)
            {
                this.timer = timer;
                this.start = timer.ElapsedMilliseconds;
                this.label = label;
            }

            public void Dispose()
            {
                this.timer.WriteLine($"{this.label}: {this.timer.ElapsedMilliseconds - this.start}ms - {this.timer.name}");
            }
        }
    }
}
