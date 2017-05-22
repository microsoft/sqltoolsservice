//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Driver;
using Microsoft.SqlTools.Utility;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

// turn off entry point since this is xUnit project
#if false

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Tests
{
    internal class Program
    {
        internal static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Microsoft.SqlTools.ServiceLayer.TestDriver.exe [tests]" + Environment.NewLine +
                                    "    [tests] is a space-separated list of tests to run." + Environment.NewLine +
                                    "            They are qualified within the Microsoft.SqlTools.ServiceLayer.TestDriver.Tests namespace" + Environment.NewLine +
                                    "Be sure to set the environment variable " + ServiceTestDriver.ServiceHostEnvironmentVariable + " to the full path of the sqltoolsservice executable.");
                return 0;
            }

            Logger.Initialize("testdriver", LogLevel.Verbose);

            int returnCode = 0;
            Task.Run(async () =>
            {
                string testNamespace = "Microsoft.SqlTools.ServiceLayer.TestDriver.Tests.";
                foreach (var test in args)
                {
                    try
                    {
                        var testName = test.Contains(testNamespace) ? test.Replace(testNamespace, "") : test;
                        bool containsTestName = testName.Contains(".");
                        var className = containsTestName ? testName.Substring(0, testName.LastIndexOf('.')) : testName;
                        var methodName = containsTestName ? testName.Substring(testName.LastIndexOf('.') + 1) : null;

                        var type = Type.GetType(testNamespace + className);
                        if (type == null)
                        {
                            Console.WriteLine("Invalid class name");
                        }
                        else
                        {
                            try
                            {
                                if (string.IsNullOrEmpty(methodName))
                                {
                                    var methods = type.GetMethods().Where(x => x.CustomAttributes.Any(a => a.AttributeType == typeof(FactAttribute)));
                                    foreach (var method in methods)
                                    {
                                        await RunTest(type, method, method.Name);
                                    }
                                }
                                else
                                {
                                    MethodInfo methodInfo = type.GetMethod(methodName);
                                    await RunTest(type, methodInfo, test);
                                }
                            }
                            finally
                            {
                                RunTestCleanup(type);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        returnCode = -1;
                    }
                }
            }).Wait();

            return returnCode;
        }

        private static async Task RunTest(Type type, MethodInfo methodInfo, string testName)
        {
            if (methodInfo == null)
            {
                Console.WriteLine("Invalid method name");
            }
            else
            {
                using (var typeInstance = (IDisposable)Activator.CreateInstance(type))
                {
                    Console.WriteLine("Running test " + testName);
                    await (Task)methodInfo.Invoke(typeInstance, null);
                    Console.WriteLine("Test ran successfully: " + testName);
                }
            }
        }

        private static void RunTestCleanup(Type type)
        {
            try
            {
                MethodInfo cleanupMethod = type.GetMethod("Cleanup");
                if (cleanupMethod != null)
                {
                    cleanupMethod.Invoke(null, null);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "An exception occurred running Cleanup for type {0}: {1}",
                    type.FullName,
                    e);
            }
        }
    }
}
#endif
