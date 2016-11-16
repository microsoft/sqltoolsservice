using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Utility
{
    public class TestRunner
    {
        public static async Task<int> RunTests(string[] tests, string testNamespace)
        {
            foreach (var test in tests)
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
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return -1;
                }
            }
            return 0;
        }

        private static async Task RunTest(Type type, MethodBase methodInfo, string testName)
        {
            if (methodInfo == null)
            {
                Console.WriteLine("Invalid method name");
            }
            else
            {
                using (var typeInstance = (IDisposable)Activator.CreateInstance(type))
                {
                    PropertyInfo testNameProperty = type.GetProperty("TestName");
                    testNameProperty?.SetValue(typeInstance, testName);
                    Console.WriteLine("Running test " + testName);
                    await (Task)methodInfo.Invoke(typeInstance, null);
                    Console.WriteLine("Test ran successfully: " + testName);
                }
            }
        }
    }
}
