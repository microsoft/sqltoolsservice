//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Utility
{
    public class TestRunner
    {
        private int numberOfRuns = 20;
        protected int DefaultNumberOfRuns = 2;

        public static TestRunner Instance { get; } = new TestRunner();
        public string[] Tests { get; set; }
        public int NumberOfRuns { get; set; }
        public string ExecutableFilePath { get; set; }

        private TestRunner()
        {
            InitParameters();
        }

        private void ParseArguments(string[] args)
        {
            int index = 0;
            while (index < args.Length - 1)
            {
                string arg = args[index++];
                string argValue = args[index++];
                switch (arg)
                {
                    case "/t":
                    case "/T":
                    case "/tests":
                        Tests = argValue.Split(" ");
                        break;
                    case "/n":
                    case "/N":
                    case "/numberOfRuns":
                        int value;
                        if (Int32.TryParse(argValue, out value))
                        {
                            NumberOfRuns = value;
                        }
                        break;
                    case "/r":
                    case "/R":
                    case "/Result":
                        ResultFolder = argValue;
                        break;
                    case "/s":
                    case "/S":
                    case "/Service":
                        ExecutableFilePath = argValue;
                        break;
                }
            }

            if ((Tests == null || Tests.Length == 0) && args.Length >= 1)
            {
                Tests = args;
            }
        }

        public string ResultFolder = InitResultFolder();

        private static string InitResultFolder()
        {
            return Environment.GetEnvironmentVariable("ResultFolder");
        }

        private void InitParameters()
        {
            string numberOfRunsEnv = Environment.GetEnvironmentVariable(Constants.NumberOfRunsEnvironmentVariable);

            if (!Int32.TryParse(numberOfRunsEnv, out numberOfRuns))
            {
                numberOfRuns = DefaultNumberOfRuns;
            }

            NumberOfRuns = numberOfRuns;

            ExecutableFilePath = Environment.GetEnvironmentVariable(Constants.ExecutableFileEnvironmentVariable);
        }

        public async Task<int> RunTests(string[] args, string testNamespace)
        {
            ParseArguments(args);
            try
            {
                throw new NotImplementedException("This code needs to change to use 'dotnet test' or nunit directly");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return await Task.FromResult(-1);
            }
        }
    }
}
