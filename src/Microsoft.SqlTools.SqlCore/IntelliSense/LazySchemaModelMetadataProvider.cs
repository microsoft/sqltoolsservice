//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;

// SSDT counterpart: MetadataProvider/SchemaModelMetadataProvider.cs

namespace Microsoft.SqlTools.SqlCore.IntelliSense
{
    /// <summary>
    /// <see cref="MetadataProviderBase"/> backed by a <see cref="TSqlModel"/>. Fully offline — no server connection.
    /// <para>
    /// The <see cref="TSqlModel"/> exposes two object scopes:<br/>
    /// - <c>DacQueryScopes.UserDefined</c>: objects from the project's .sql files.<br/>
    /// - <c>DacQueryScopes.BuiltIn</c>: <c>sys.*</c>, <c>INFORMATION_SCHEMA.*</c> and all other
    ///   system catalog objects embedded in the DacFx assembly.
    /// </para>
    /// <para>
    /// Construction is lazy: the constructor only creates the server/database shells.
    /// Schema collections are built on first access; object and column collections within
    /// each schema are built on first access within that schema.
    /// </para>
    /// </summary>
    public sealed class LazySchemaModelMetadataProvider : MetadataProviderBase
    {
        private readonly TSqlModel _model;
        private readonly LazyModelServer _server;
        private readonly Dictionary<string, SourceInformation> _sourceLocations;

        /// <summary>
        /// Initializes a new lazy provider from an already-loaded <paramref name="model"/>.
        /// </summary>
        public LazySchemaModelMetadataProvider(TSqlModel model, string databaseName)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            if (string.IsNullOrEmpty(databaseName))
                throw new ArgumentNullException(nameof(databaseName));

            _model = model;
            _server = new LazyModelServer(model, databaseName);
            _sourceLocations = BuildSourceLocationIndex();
        }

        /// <summary>
        /// Builds the source location index by scanning all user-defined objects in the model.
        /// Called once during construction.
        /// </summary>
        private Dictionary<string, SourceInformation> BuildSourceLocationIndex()
        {
            var index = new Dictionary<string, SourceInformation>(StringComparer.OrdinalIgnoreCase);

            foreach (TSqlObject obj in _model.GetObjects(DacQueryScopes.UserDefined))
            {
                if (obj.Name?.Parts == null)
                    continue;

                string qualifiedName = string.Join(".", obj.Name.Parts);
                SourceInformation? sourceInfo = obj.GetSourceInformation();

                if (sourceInfo?.SourceName != null)
                {
                    index[qualifiedName] = sourceInfo;
                }
            }

            return index;
        }

        /// <summary>
        /// Retrieves source file information for a schema-qualified object name.
        /// </summary>
        /// <param name="qualifiedName">Schema-qualified name (e.g., "dbo.Orders").</param>
        /// <param name="sourceInfo">Source information if found; otherwise null.</param>
        /// <returns>True if source information was found; otherwise false.</returns>
        public bool TryGetSourceInformation(string qualifiedName, out SourceInformation? sourceInfo)
        {
            return _sourceLocations.TryGetValue(qualifiedName, out sourceInfo);
        }

        /// <inheritdoc/>
        public override IServer Server => _server;

        /// <inheritdoc/>
        public override MetadataProviderEventHandler? AfterBindHandler => null;

        /// <inheritdoc/>
        public override MetadataProviderEventHandler? BeforeBindHandler => null;
    }
}
