//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

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
            foreach (var test in Tests)
            {
                try
                {
                    var testName = test.Contains(testNamespace) ? test.Replace(testNamespace, "") : test;
                    bool containsTestName = testName.Contains(".");
                    var className = containsTestName ? testName.Substring(0, testName.LastIndexOf('.')) : testName;
                    var methodName = containsTestName ? testName.Substring(testName.LastIndexOf('.') + 1) : null;
                    Assembly assembly = Assembly.GetEntryAssembly();
                    Type type = assembly.GetType(testNamespace + className);
                    if (type == null)
                    {
                        Console.WriteLine("Invalid class name");
                    }
                    else
                    {
                        var typeInstance = Activator.CreateInstance(type);
                        if (string.IsNullOrEmpty(methodName))
                        {
                            var methods = type.GetMethods().Where(x => x.CustomAttributes.Any(a => a.AttributeType == typeof(FactAttribute)));
                            foreach (var method in methods)
                            {
                                await RunTest(typeInstance, method, method.Name);
                            }
                        }
                        else
                        {
                            MethodInfo methodInfo = type.GetMethod(methodName);
                            await RunTest(typeInstance, methodInfo, test);
                        }

                        IDisposable disposable = typeInstance as IDisposable;
                        if (disposable != null)
                        {
                            disposable.Dispose();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return -1;
                }
            }
            return 0;
        }

        private static async Task RunTest(object typeInstance, MethodInfo methodInfo, string testName)
        {
            try
            {
                if (methodInfo == null)
                {
                    Console.WriteLine("Invalid method name");
                }
                else
                {
                    var testAttributes = methodInfo.CustomAttributes;
                    BeforeAfterTestAttribute beforeAfterTestAttribute = null;
                    foreach (var attribute in testAttributes)
                    {
                        var args = attribute.ConstructorArguments.Select(x => x.Value as object).ToArray();
                        var objAttribute = Activator.CreateInstance(attribute.AttributeType, args);

                        beforeAfterTestAttribute = objAttribute as BeforeAfterTestAttribute;
                        if (beforeAfterTestAttribute != null)
                        {
                            beforeAfterTestAttribute.Before(methodInfo);
                        }
                    }
                    Console.WriteLine("Running test " + testName);
                    await (Task)methodInfo.Invoke(typeInstance, null);
                    if (beforeAfterTestAttribute != null)
                    {
                        beforeAfterTestAttribute.After(methodInfo);
                    }
                    Console.WriteLine("Test ran successfully: " + testName);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Test Failed: {0} error: {1}", testName, ex.Message));

            }
        }
    }
}
