//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.Utility
{
    /// <summary>
    /// Carries logical operation metadata that should be written with log messages.
    /// </summary>
    public sealed class LogOperationContext
    {
        public LogOperationContext(
            string operationId,
            string service,
            string rpcMethod,
            string rpcId = null,
            string rpcType = null,
            string flowId = null)
        {
            OperationId = operationId;
            Service = service;
            RpcMethod = rpcMethod;
            RpcId = rpcId;
            RpcType = rpcType;
            FlowId = flowId;
        }

        public string OperationId { get; }

        public string Service { get; }

        public string RpcMethod { get; }

        public string RpcId { get; }

        public string RpcType { get; }

        public string FlowId { get; }

        public string ToLogPrefix()
        {
            var parts = new List<string>();
            AddPart(parts, "operationId", OperationId);
            AddPart(parts, "service", Service);
            AddPart(parts, "rpcMethod", RpcMethod);
            AddPart(parts, "rpcId", RpcId);
            AddPart(parts, "rpcType", RpcType);
            AddPart(parts, "flowId", FlowId);
            return string.Join(" ", parts);
        }

        private static void AddPart(ICollection<string> parts, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            parts.Add($"{key}:{Sanitize(value)}");
        }

        private static string Sanitize(string value)
        {
            var sanitized = value.Trim().Replace(" ", "_").Replace("\t", "_").Replace("\r", "_").Replace("\n", "_");
            return sanitized.Length <= 256 ? sanitized : sanitized.Substring(0, 256);
        }
    }
}
