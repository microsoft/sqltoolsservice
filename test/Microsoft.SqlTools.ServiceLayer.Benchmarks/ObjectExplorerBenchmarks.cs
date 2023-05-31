//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Linq;
using BenchmarkDotNet.Running;

namespace Microsoft.SqlTools.ServiceLayer.Benchmarks
{
    public class ObjectExplorerBenchmarks
    {
        public static void Main(string[] args)
        {
            var runAllTests = args.Length == 0;

            if (runAllTests || args.Contains("objectExplorer"))
            {
                BenchmarkRunner.Run<ObjectExplorerPerformance>();
            }
        }
    }    
}
