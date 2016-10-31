//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Driver;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Microsoft.SqlTools.ServiceLayer.TestDriver
{
    internal class Program
    {
        internal static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine(  "Microsoft.SqlTools.ServiceLayer.TestDriver.exe [tests]" + Environment.NewLine +
                                    "    [tests] is a space-separated list of tests to run." + Environment.NewLine + 
                                    "            They are qualified within the Microsoft.SqlTools.ServiceLayer.TestDriver.Tests namespace" + Environment.NewLine +
                                    "Be sure to set the environment variable " + ServiceTestDriver.ServiceHostEnvironmentVariable + " to the full path of the sqltoolsservice executable.");
                Environment.Exit(0);
            }

            Logger.Initialize("testdriver", LogLevel.Verbose);

            Task.Run(async () => 
            {
                foreach (var test in args)
                {
                    try
                    {
                        var className = test.Substring(0, test.LastIndexOf('.'));
                        var methodName = test.Substring(test.LastIndexOf('.') + 1);
                        
                        var type = Type.GetType("Microsoft.SqlTools.ServiceLayer.TestDriver.Tests." + className);
                        using (var typeInstance = (IDisposable)Activator.CreateInstance(type))
                        {
                            MethodInfo methodInfo = type.GetMethod(methodName);

                            Console.WriteLine("Running test " + test);
                            await (Task)methodInfo.Invoke(typeInstance, null);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }).Wait();
        }
    }
}
