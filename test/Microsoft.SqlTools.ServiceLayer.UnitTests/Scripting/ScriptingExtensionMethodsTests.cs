using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlTools.ServiceLayer.Scripting;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Scripting
{
    
    public class ScriptingExtensionMethodsTests
    {
        /// <summary>
        /// SQL sysname supports single quotes in object names, so URN attributes need to be properly escaped
        /// </summary>
        [Test]
        public void ToUrnEscapesAttributes()
        {
            var scriptingObject = new ScriptingObject() { Name = "quoted'Name", Schema = "quoted'Schema", Type = "Table" };
            var urn = scriptingObject.ToUrn("server", "quoted'db");
            Assert.That(urn.ToString, Is.EqualTo("Server[@Name='SERVER']/Database[@Name='quoted''db']/Table[@Name='quoted''Name' and @Schema = 'quoted''Schema']"), "Urn should have escaped Name attributes");
            Assert.That(urn.Type, Is.EqualTo("Table"), "Urn Type");
            // These assertions are more for educational purposes than for testing, since the methods are Urn methods in SFC.
            Assert.That(urn.GetNameForType("Database"), Is.EqualTo("quoted'db"), "GetNameForType('Database')");
            Assert.That(urn.GetAttribute("Schema"), Is.EqualTo("quoted'Schema"), "GetAttribute('Schema')");
        }

        [Test]
        public void ToObjectStringUnescapesAttributes()
        {
            var urn = new Urn(@"Server[@Name = 'SERVER']/Database[@Name = 'quoted''db']/Table[@Name = 'quoted''Name' and @Schema = 'quoted''Schema']");
            var scriptingObject = urn.ToScriptingObject();
            Assert.That(scriptingObject.Type, Is.EqualTo("Table"), "Type");
            Assert.That(scriptingObject.Name, Is.EqualTo("quoted'Name"), "Name");
            Assert.That(scriptingObject.Schema, Is.EqualTo("quoted'Schema"), "Schema");
        }
    }
}
