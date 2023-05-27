//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.SqlTools.ServiceLayer.Benchmarks.Test;

namespace Microsoft.SqlTools.ServiceLayer.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Use BenchmarkRunner.Run to Benchmark your code
            var summary = BenchmarkRunner.Run<Sha512VsSha256>();
        }
    }
}