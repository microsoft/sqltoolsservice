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
using Microsoft.Extensions.DependencyModel;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.Extensibility
{
    public class ExtensionServiceProvider : RegisteredServiceProvider
    {
        private Func<ConventionBuilder, ContainerConfiguration> config;

        public ExtensionServiceProvider(Func<ConventionBuilder, ContainerConfiguration> config)
        {
            Validate.IsNotNull(nameof(config), config);
            this.config = config;
        }

        public static ExtensionServiceProvider CreateDefaultServiceProvider()
        {
            // only allow loading MEF dependencies from our assemblies until we can 
            // better seperate out framework assemblies and extension assemblies
            string[] inclusionList = 
            {
                "microsofsqltoolscredentials.dll",
                "microsoft.sqltools.hosting.dll",
                "microsoftsqltoolsservicelayer.dll"
            };

            return CreateFromAssembliesInDirectory(inclusionList);
        }

        /// <summary>
        /// Creates a service provider by loading a set of named assemblies, expected to be in the current working directory
        /// </summary>
        /// <param name="inclusionList">full DLL names, as a string enumerable</param>
        /// <returns><see cref="ExtensionServiceProvider"/> instance</returns>
        public static ExtensionServiceProvider CreateFromAssembliesInDirectory(IEnumerable<string> inclusionList)
        {
            string assemblyPath = typeof(ExtensionStore).GetTypeInfo().Assembly.Location;
            string directory = Path.GetDirectoryName(assemblyPath);

            AssemblyLoadContext context = new AssemblyLoader(directory);
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
                    assemblies.Add(
                        context.LoadFromAssemblyName(
                            AssemblyLoadContext.GetAssemblyName(path)));
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
                base.Register<T>(() => store.GetExports<T>());
            }
        }
    }
    
    /// <summary>
    /// A store for MEF exports of a specific type. Provides basic wrapper functionality around MEF to standarize how
    /// we lookup types and return to callers.
    /// </summary>
    public class ExtensionStore
    {
        private CompositionHost host;
        private IList exports;
        private Type contractType;
        
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

        /// <summary>
        /// Loads extensions from the current assembly
        /// </summary>
        /// <returns>ExtensionStore</returns>
        public static ExtensionStore CreateDefaultLoader<T>()
        {
            return CreateAssemblyStore<T>(typeof(ExtensionStore).GetTypeInfo().Assembly);
        }
        
        public static ExtensionStore CreateAssemblyStore<T>(Assembly assembly)
        {
            Validate.IsNotNull(nameof(assembly), assembly);
            return new ExtensionStore(typeof(T), (conventions) =>
                new ContainerConfiguration().WithAssembly(assembly, conventions));
        }

        public static ExtensionStore CreateStoreForCurrentDirectory<T>()
        {
            string assemblyPath = typeof(ExtensionStore).GetTypeInfo().Assembly.Location;
            string directory = Path.GetDirectoryName(assemblyPath);
            return new ExtensionStore(typeof(T), (conventions) => 
                new ContainerConfiguration().WithAssembliesInPath(directory, conventions));
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
    
    public static class ContainerConfigurationExtensions
    {
        public static ContainerConfiguration WithAssembliesInPath(this ContainerConfiguration configuration, string path, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            return WithAssembliesInPath(configuration, path, null, searchOption);
        }

        public static ContainerConfiguration WithAssembliesInPath(this ContainerConfiguration configuration, string path, AttributedModelProvider conventions, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            AssemblyLoadContext context = new AssemblyLoader(path);
            var assemblyNames = Directory
                .GetFiles(path, "*.dll", searchOption)
                .Select(AssemblyLoadContext.GetAssemblyName);

            var assemblies = assemblyNames
                .Select(context.LoadFromAssemblyName)
                .ToList();

            configuration = configuration.WithAssemblies(assemblies, conventions);

            return configuration;
        }
    }

    public class AssemblyLoader : AssemblyLoadContext
    {
        private string folderPath;

        public AssemblyLoader(string folderPath)
        {
            this.folderPath = folderPath;
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            var deps = DependencyContext.Default;
            var res = deps.CompileLibraries.Where(d => d.Name.Equals(assemblyName.Name)).ToList();
            if (res.Count > 0)
            {
                return Assembly.Load(new AssemblyName(res.First().Name));
            }
            else
            {
                var apiApplicationFileInfo = new FileInfo($"{folderPath}{Path.DirectorySeparatorChar}{assemblyName.Name}.dll");
                if (File.Exists(apiApplicationFileInfo.FullName))
                {
                    // Creating a new AssemblyContext instance for the same folder puts us at risk 
                    // of loading the same DLL in multiple contexts, which leads to some unpredictable
                    // behavior in the loader. See https://github.com/dotnet/coreclr/issues/19632

                    return LoadFromAssemblyPath(apiApplicationFileInfo.FullName);
                }
            }
            return Assembly.Load(assemblyName);
        }
    }
}
