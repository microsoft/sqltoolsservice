//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Driver;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver
{
    internal class Program
    {
        internal static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine(  "Microsoft.SqlTools.ServiceLayer.TestDriver.exe <service host executable> [tests]" + Environment.NewLine +
                                    "    <service host executable> is the path to the Microsoft.SqlTools.ServiceLayer.exe executable" + Environment.NewLine +
                                    "    [tests] is a space-separated list of tests to run." + Environment.NewLine + 
                                    "            They are qualified within the Microsoft.SqlTools.ServiceLayer.TestDriver.Tests namespace");
                Environment.Exit(0);
            }

            Task.Run(async () => 
            {
                var serviceHostExecutable = args[0];
                var tests = args.Skip(1);

                foreach (var test in tests)
                {
                    ServiceTestDriver driver = null;

                    try
                    {
                        driver = new ServiceTestDriver(serviceHostExecutable);

                        var className = test.Substring(0, test.LastIndexOf('.'));
                        var methodName = test.Substring(test.LastIndexOf('.') + 1);
                        
                        var type = Type.GetType("Microsoft.SqlTools.ServiceLayer.TestDriver.Tests." + className);
                        var typeInstance = Activator.CreateInstance(type);
                        MethodInfo methodInfo = type.GetMethod(methodName);

                        await driver.Start();
                        Console.WriteLine("Running test " + test);
                        await (Task)methodInfo.Invoke(typeInstance, new object[] {driver});
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                    finally
                    {
                        if (driver != null)
                        {
                            await driver.Stop();
                        }
                    }
                }
            }).Wait();
        }
    }
}
