//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Microsoft.SqlTools.Sts2.Testing.Scenarios
{
    /// <summary>An expected JSON-RPC error (SPEC §7.6 shape).</summary>
    public sealed record ScenarioExpectedError
    {
        /// <summary>Required stable identity in <c>error.data.code</c>.</summary>
        public required string DataCode { get; init; }

        /// <summary>Optional numeric JSON-RPC code assertion.</summary>
        public int? JsonRpcCode { get; init; }
    }

    /// <summary>One executable scenario step.</summary>
    public sealed record ScenarioStep
    {
        /// <summary><c>request</c> or <c>awaitTerminal</c>.</summary>
        public required string Kind { get; init; }

        /// <summary>v2 method for request steps.</summary>
        public string? Method { get; init; }

        /// <summary>Request params before variable substitution.</summary>
        public JsonNode? Params { get; init; }

        /// <summary>False fires the request without awaiting its terminal (race scenarios).</summary>
        public bool Await { get; init; } = true;

        /// <summary>Label for later <c>awaitTerminal</c> reference.</summary>
        public string? Label { get; init; }

        /// <summary>Partial-match expectation against the result body.</summary>
        public JsonNode? ExpectResult { get; init; }

        /// <summary>Expected error, mutually exclusive with <see cref="ExpectResult"/>.</summary>
        public ScenarioExpectedError? ExpectError { get; init; }

        /// <summary>Bindings: result property name -> variable name (with $).</summary>
        public IReadOnlyDictionary<string, string> Bind { get; init; } = new Dictionary<string, string>();
    }

    /// <summary>A parsed scenario file: header, driver script, steps, invariants.</summary>
    public sealed record ScenarioDefinition
    {
        /// <summary>Header fields shared with the catalog.</summary>
        public required ScenarioInfo Info { get; init; }

        /// <summary>Scripted FakeDriver open behaviors, in order.</summary>
        public IReadOnlyList<FakeOpenBehavior> OpenBehaviors { get; init; } = [];

        /// <summary>Steps in execution order.</summary>
        public IReadOnlyList<ScenarioStep> Steps { get; init; } = [];

        /// <summary>Invariants to check after the run (I1, I5, I6, I7, I8, I12).</summary>
        public IReadOnlyList<string> Invariants { get; init; } = [];

        /// <summary>Optional limit overrides journaled into session.start (for example maxConnections).</summary>
        public JsonNode? ConfigLimits { get; init; }
    }
}
