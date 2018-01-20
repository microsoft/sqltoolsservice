//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.SqlTools.Dmp.Hosting.Utility;

namespace Microsoft.SqlTools.Dmp.Hosting.Extensibility
{
    /// <summary>
    /// A MEF-based service provider. Supports any MEF-based configuration but is optimized for 
    /// service discovery over a set of DLLs in an application scope. Any service registering using
    /// the <c>[Export(IServiceContract)]</c> attribute will be discovered and used by this service
    /// provider if it's in the set of Assemblies / Types specified during its construction. Manual
    /// override of this is supported by calling 
    /// <see cref="RegisteredServiceProvider.RegisterSingleService" /> and similar methods, since
    /// this will initialize that service contract and avoid the MEF-based search and discovery 
    /// process. This allows the service provider to link into existing singleton / known services
    /// while using MEF-based dependency injection and inversion of control for most of the code.
    /// </summary>
    public class ExtensionServiceProvider : RegisteredServiceProvider
    {
        private readonly Func<ConventionBuilder, ContainerConfiguration> config;

        public ExtensionServiceProvider(Func<ConventionBuilder, ContainerConfiguration> config)
        {
            Validate.IsNotNull(nameof(config), config);
            this.config = config;
        }

        /// <summary>
        /// Creates a service provider by loading a set of named assemblies, expected to be <paramref name="directory"/>
        /// </summary>
        /// <param name="directory">Directory to search for included assemblies</param>
        /// <param name="inclusionList">full DLL names, case insensitive, of assemblies to include</param>
        /// <returns><see cref="ExtensionServiceProvider"/> instance</returns>
        public static ExtensionServiceProvider CreateFromAssembliesInDirectory(string directory, IList<string> inclusionList)
        {
            //AssemblyLoadContext context = new AssemblyLoader(directory);
            var assemblyPaths = Directory.GetFiles(directory, "*.dll", SearchOption.TopDirectoryOnly);

            List<Assembly> assemblies = new List<Assembly>();
            foreach (var path in assemblyPaths)
            {
                // skip DLL files not in inclusion list
                bool isInList = false;
                foreach (var item in inclusionList)
                {
                    if (path.EndsWith(item, StringComparison.OrdinalIgnoreCase))
                    {
                        isInList = true;
                        break;
                    }
                }

                if (!isInList)
                {
                    continue;
                }

                try
                {
                    assemblies.Add(AssemblyLoadContext.Default.LoadFromAssemblyPath(path));
                }
                catch (Exception)
                {
                    // we expect exceptions trying to scan all DLLs since directory contains native libraries
                }
            }

            return Create(assemblies);
        }

        public static ExtensionServiceProvider Create(IEnumerable<Assembly> assemblies)
        {
            Validate.IsNotNull(nameof(assemblies), assemblies);
            return new ExtensionServiceProvider(conventions => new ContainerConfiguration().WithAssemblies(assemblies, conventions));
        }

        public static ExtensionServiceProvider Create(IEnumerable<Type> types)
        {
            Validate.IsNotNull(nameof(types), types);
            return new ExtensionServiceProvider(conventions => new ContainerConfiguration().WithParts(types, conventions));
        }

        protected override IEnumerable<T> GetServicesImpl<T>()
        {
            EnsureExtensionStoreRegistered<T>();
            return base.GetServicesImpl<T>();
        }

        private void EnsureExtensionStoreRegistered<T>()
        {
            if (!services.ContainsKey(typeof(T)))
            {
                ExtensionStore store = new ExtensionStore(typeof(T), config);
                Register(() => store.GetExports<T>());
            }
        }
    }
    
    /// <summary>
    /// A store for MEF exports of a specific type. Provides basic wrapper functionality around MEF to standarize how
    /// we lookup types and return to callers.
    /// </summary>
    public class ExtensionStore
    {
        private readonly CompositionHost host;
        private IList exports;
        private readonly Type contractType;
        
        /// <summary>
        /// Initializes the store with a type to lookup exports of, and a function that configures the
        /// lookup parameters.
        /// </summary>
        /// <param name="contractType">Type to use as a base for all extensions being looked up</param>
        /// <param name="configure">Function that returns the configuration to be used</param>
        public ExtensionStore(Type contractType, Func<ConventionBuilder, ContainerConfiguration> configure)
        {
            Validate.IsNotNull(nameof(contractType), contractType);
            Validate.IsNotNull(nameof(configure), configure);
            this.contractType = contractType;
            ConventionBuilder builder = GetExportBuilder();
            ContainerConfiguration config = configure(builder);
            host = config.CreateContainer();
        }

        public IEnumerable<T> GetExports<T>()
        {
            if (exports == null)
            {
                exports = host.GetExports(contractType).ToList();
            }
            return exports.Cast<T>();
        }
        
        private ConventionBuilder GetExportBuilder()
        {
            // Define exports as matching a parent type, export as that parent type
            var builder = new ConventionBuilder();
            builder.ForTypesDerivedFrom(contractType).Export(exportConventionBuilder  => exportConventionBuilder.AsContractType(contractType));
            return builder;
        }
    }
}
