//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    /// <summary>
    /// Timer to calculate the test run time
    /// </summary>
    public class TestTimer
    {
        private static string resultFolder = InitResultFolder();

        private static string InitResultFolder()
        {
            string resultFodler = Environment.GetEnvironmentVariable("ResultFolder");
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
        }

        public void EndAndPrint([CallerMemberName] string testName = "")
        {
            End();
            if (PrintResult)
            {
                var currentColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Test Name: {0} Run time in milliSeconds: {1}", testName, TotalMilliSeconds));
                Console.ForegroundColor = currentColor;
                string resultContent = Newtonsoft.Json.JsonConvert.SerializeObject(new TestResult { ElapsedTime = TotalMilliSeconds });
                string fileName = testName + ".json";
                string resultFilePath = string.IsNullOrEmpty(resultFolder) ? fileName : Path.Combine(resultFolder, fileName);
                File.WriteAllText(resultFilePath, resultContent);
                Console.WriteLine("Result file: " + resultFilePath);
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
