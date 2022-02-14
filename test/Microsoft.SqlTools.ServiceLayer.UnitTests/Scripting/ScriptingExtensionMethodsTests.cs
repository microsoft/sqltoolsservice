using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlTools.ServiceLayer.Scripting;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using NUnit.Framework;

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
            // Arrange
            var scriptingObject = new ScriptingObject { Name = "quoted'Name", Schema = "quoted'Schema", Type = "Table" };

            // Act
            var urn = scriptingObject.ToUrn("server", "quoted'db");

            // Assert
            Assert.AreEqual(
                "Server[@Name='SERVER']/Database[@Name='quoted''db']/Table[@Name='quoted''Name' and @Schema = 'quoted''Schema']",
                urn.ToString,
                "Urn should have escaped Name attributes");
            Assert.AreEqual("Table", urn.Type, "Urn Type");

            // These assertions are more for educational purposes than for testing, since the methods are Urn methods in SFC.
            Assert.AreEqual("quoted'db", urn.GetNameForType("Database"), "GetNameForType('Database')");
            Assert.AreEqual("quoted'Schema", urn.GetAttribute("Schema"), "GetAttribute('Schema')");
        }

        [Test]
        public void ToObjectStringUnescapesAttributes()
        {
            // Arrange
            var urn = new Urn(@"Server[@Name = 'SERVER']/Database[@Name = 'quoted''db']/Table[@Name = 'quoted''Name' and @Schema = 'quoted''Schema']");

            // Act
            var scriptingObject = urn.ToScriptingObject();

            // Assert
            Assert.AreEqual("Table", scriptingObject.Type, "Type");
            Assert.AreEqual("quoted'Name", scriptingObject.Name, "Name");
            Assert.AreEqual("quoted'Schema", scriptingObject.Schema, "Schema");
        }
    }
}
