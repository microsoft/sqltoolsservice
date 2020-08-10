//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Composition;
using System.Linq;
using System.Reflection;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.ServiceLayer.Formatter;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Extensibility
{
    public class ExtensionTests
    {

        [Test]
        public void CreateAssemblyStoreShouldFindTypesInAssembly()
        {
            // Given a store for MyExportType
            ExtensionStore store = ExtensionStore.CreateAssemblyStore<MyExportType>(GetType().GetTypeInfo().Assembly);
            // Then should get any export for this type and subtypes
            Assert.AreEqual(2, store.GetExports<MyExportType>().Count());

            // But for a different type, expect throw as the store only contains MyExportType
            Assert.Throws<InvalidCastException>(() => store.GetExports<MyOtherType>().Count());
        }
        
        [Test]
        public void CreateDefaultLoaderShouldFindTypesOnlyInMainAssembly()
        {
            // Given a store created using CreateDefaultLoader
            // Then not should find exports from a different assembly
            ExtensionStore store = ExtensionStore.CreateDefaultLoader<MyExportType>();
            Assert.AreEqual(0, store.GetExports<MyExportType>().Count());

            // And should not find exports that are defined in the ServiceLayer assembly
            store = ExtensionStore.CreateDefaultLoader<ASTNodeFormatterFactory>();
            Assert.That(store.GetExports<ASTNodeFormatterFactory>(), Is.Empty);            
        }

        [Test]
        public void CreateDefaultServiceProviderShouldFindTypesInAllKnownAssemblies()
        {
            // Given a default ExtensionServiceProvider
            // Then we should not find exports from a test assembly
            ExtensionServiceProvider serviceProvider = ExtensionServiceProvider.CreateDefaultServiceProvider();
            Assert.That(serviceProvider.GetServices<MyExportType>(), Is.Empty);

            // But should find exports that are defined in the main assembly            
            Assert.That(serviceProvider.GetServices<ASTNodeFormatterFactory>(), Is.Not.Empty);
        }

        // [Test]
        public void CreateStoreForCurrentDirectoryShouldFindExportsInDirectory()
        {
            // Given stores created for types in different assemblies
            ExtensionStore myStore = ExtensionStore.CreateStoreForCurrentDirectory<MyExportType>();
            ExtensionStore querierStore = ExtensionStore.CreateStoreForCurrentDirectory<ASTNodeFormatterFactory>();

            // When I query exports
            // Then exports for all assemblies should be found
            Assert.AreEqual(2, myStore.GetExports<MyExportType>().Count());
            Assert.That(querierStore.GetExports<ASTNodeFormatterFactory>(), Is.Not.Empty);
        }        
    }

    // Note: in order for the MEF lookup to succeed, one class must have 
    [Export(typeof(MyExportType))]
    public class MyExportType
    {

    }
    
    public class MyExportSubType : MyExportType
    {

    }

    public class MyOtherType
    {

    }
}

