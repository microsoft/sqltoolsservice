//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.Text.Json.Nodes;

namespace Microsoft.SqlTools.Sts2.Core
{
    /// <summary>
    /// The single, deterministic, redacted dump of a Core state (SPEC §12.2, I16). One
    /// format, shared by live <c>v2/diagnostics.state</c>, replay <c>until --seq</c>,
    /// scenario diffs, and exports, so a viewer can compare live and replayed state without
    /// spurious differences. It contains ids, phases, counters, and the machine flags that
    /// explain why a connection or query is parked — never secrets, row cells, or SQL text.
    /// Runtime-only facts (driver leases, queue depth, config version) are NOT here; they
    /// are overlaid on the live response at the coordinator edge, since replay cannot know them.
    /// </summary>
    public static class CoreStateDump
    {
        /// <summary>Produces the canonical redacted JSON dump of <paramref name="state"/> at <paramref name="atSeq"/>.</summary>
        public static string ToJson(CoreState state, long atSeq) => ToNode(state, atSeq).ToJsonString();

        /// <summary>The dump as a mutable node, so the live path can attach a Runtime overlay.</summary>
        public static JsonObject ToNode(CoreState state, long atSeq)
        {
            ArgumentNullException.ThrowIfNull(state);

            var connections = new JsonObject();
            foreach ((string id, ConnectionInfo connection) in state.Connections)
            {
                connections[id] = new JsonObject
                {
                    ["phase"] = connection.Phase,
                    ["openId"] = connection.OpenId,
                    ["activeQueryId"] = connection.ActiveQueryId,
                    ["hasHandle"] = connection.HandleId is not null,
                    ["cancelRequested"] = connection.CancelRequested,
                    ["closeAfterQuery"] = connection.CloseAfterQuery,
                    ["closePending"] = connection.CloseCorr is not null,
                };
            }

            var queries = new JsonObject();
            foreach ((string id, QueryInfo query) in state.Queries)
            {
                queries[id] = new JsonObject
                {
                    ["phase"] = query.Phase,
                    ["connectionId"] = query.ConnectionId,
                    ["pagesSent"] = query.PagesSent,
                    ["pagesAcked"] = query.PagesAcked,
                    ["creditOutstanding"] = query.CreditOutstanding,
                    ["completeSent"] = query.CompleteSent,
                };
            }

            var drivers = new JsonArray();
            foreach (DriverDescriptor driver in state.Drivers)
            {
                drivers.Add(new JsonObject
                {
                    ["name"] = driver.Name,
                    ["dialects"] = new JsonArray(driver.Dialects.Select(d => (JsonNode)JsonValue.Create(d)).ToArray()),
                    ["production"] = driver.Production,
                });
            }

            return new JsonObject
            {
                ["atSeq"] = atSeq,
                ["lastSeq"] = state.LastSeq,
                ["initialized"] = state.Initialized,
                ["shuttingDown"] = state.ShuttingDown,
                ["serviceVersion"] = state.ServiceVersion,
                ["maxConnections"] = state.MaxConnections,
                ["configVersion"] = state.ConfigVersion,
                ["rowCapture"] = state.RowCapture,
                ["sqlCapture"] = state.SqlCapture,
                ["drivers"] = drivers,
                ["connections"] = connections,
                ["queries"] = queries,
            };
        }
    }
}
