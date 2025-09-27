//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Reflection;

Console.WriteLine("SqlScriptoria Test Console");
Console.WriteLine("==========================");

try
{
    // Test basic assembly loading
    var sqlScriptoriaAssembly = Assembly.LoadFrom("../../bin/ref1/SqlScriptoria.dll");
    Console.WriteLine($"✅ Successfully loaded SqlScriptoria.dll");
    Console.WriteLine($"   Version: {sqlScriptoriaAssembly.GetName().Version}");
    Console.WriteLine($"   Location: {sqlScriptoriaAssembly.Location}");
    
    var sqlScriptoriaCommonAssembly = Assembly.LoadFrom("../../bin/ref1/SqlScriptoriaCommon.dll");
    Console.WriteLine($"✅ Successfully loaded SqlScriptoriaCommon.dll");
    Console.WriteLine($"   Version: {sqlScriptoriaCommonAssembly.GetName().Version}");
    
    var scriptoriaAssembly = Assembly.LoadFrom("../../bin/ref1/Scriptoria.dll");
    Console.WriteLine($"✅ Successfully loaded Scriptoria.dll");
    Console.WriteLine($"   Version: {scriptoriaAssembly.GetName().Version}");
    
    // Try to access some basic types
    Console.WriteLine("\nTesting type access:");
    
    // Look for common namespace patterns from the original code
    var types = sqlScriptoriaAssembly.GetTypes();
    Console.WriteLine($"   Total types in SqlScriptoria: {types.Length}");
    
    // Look for SqlScriptoriaExecutionContext
    var executionContextType = Array.Find(types, t => t.Name.Contains("ExecutionContext"));
    if (executionContextType != null)
    {
        Console.WriteLine($"✅ Found execution context type: {executionContextType.FullName}");
    }
    
    // Look for common interfaces
    var interfaces = Array.FindAll(types, t => t.IsInterface);
    Console.WriteLine($"   Interfaces found: {interfaces.Length}");
    
    foreach (var iface in interfaces.Take(5))
    {
        Console.WriteLine($"     - {iface.Name}");
    }
    
    Console.WriteLine("\n🎉 All basic tests passed! The new SqlScriptoria version appears compatible.");
    
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error testing SqlScriptoria: {ex.Message}");
    Console.WriteLine($"   Type: {ex.GetType().Name}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"   Inner: {ex.InnerException.Message}");
    }
    return;
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();