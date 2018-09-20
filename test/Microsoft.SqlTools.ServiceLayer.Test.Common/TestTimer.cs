//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.TestDriver.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    /// <summary>
    /// Timer to calculate the test run time
    /// </summary>
    public class TestTimer
    {
        private static string resultFolder = InitResultFolder();

        private List<double> iterations = new List<double>();

        private static string InitResultFolder()
        {
            string resultFodler = Environment.GetEnvironmentVariable("ResultFolder");
            if (string.IsNullOrEmpty(resultFodler))
            {
                resultFodler = TestRunner.Instance.ResultFolder;
            }
            if (string.IsNullOrEmpty(resultFodler))
            {
                string assemblyLocation = System.Reflection.Assembly.GetEntryAssembly().Location;
                resultFodler = Path.GetDirectoryName(assemblyLocation);
            }
            return resultFodler;
        }

        public TestTimer()
        {
            Start();
        }

        public bool PrintResult { get; set; }

        public void Start()
        {
            StartDateTime = DateTime.UtcNow;
        }

        public void End()
        {
            EndDateTime = DateTime.UtcNow;
            iterations.Add(TotalMilliSeconds);
            if (PrintResult)
            {
                Console.WriteLine("Result: " + TotalMilliSeconds);
            }
        }

        public void EndAndPrint([CallerMemberName] string testName = "")
        {
            End();
            Print(testName);
        }

        public void Print([CallerMemberName] string testName = "")
        {
            if (PrintResult)
            {
                var iterationArray = iterations.ToArray();
                double elapsed = Percentile(iterationArray, 0.5);
                var currentColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Test Name: {0} Run time in milliSeconds: {1}", testName, elapsed));
                Console.ForegroundColor = currentColor;
                string resultContent = Newtonsoft.Json.JsonConvert.SerializeObject(new TestResult
                {
                    ElapsedTime = elapsed,
                    MetricValue = elapsed,
                    PrimaryMetric = "ElapsedTimeMetric",
                    Iterations = iterationArray,
                    FiftiethPercentile = Percentile(iterationArray, 0.5),
                    NinetiethPercentile = Percentile(iterationArray, 0.9),
                    Average = iterations.Where(x => x > 0).Average()
                });
                string fileName = testName + ".json";
                string resultFilePath = string.IsNullOrEmpty(resultFolder) ? fileName : Path.Combine(resultFolder, fileName);
                File.WriteAllText(resultFilePath, resultContent);
                Console.WriteLine("Result file: " + resultFilePath);
            }
        }

        private static double Percentile(double[] sequence, double excelPercentile)
        {
            Array.Sort(sequence);
            int N = sequence.Length;
            double n = (N - 1) * excelPercentile + 1;
            // Another method: double n = (N + 1) * excelPercentile;
            if (n == 1d) return sequence[0];
            else if (n == N) return sequence[N - 1];
            else
            {
                int k = (int)n;
                double d = n - k;
                return sequence[k - 1] + d * (sequence[k] - sequence[k - 1]);
            }
        }

        public double TotalMilliSeconds
        {
            get
            {
                return (EndDateTime - StartDateTime).TotalMilliseconds;
            }
        }

        public double TotalMilliSecondsUntilNow
        {
            get
            {
                return (DateTime.UtcNow - StartDateTime).TotalMilliseconds;
            }
        }

        public DateTime StartDateTime { get; private set; }
        public DateTime EndDateTime { get; private set; }
    }
}
