//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.LanguageService.Scripting;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.LanguageServer
{
    /// <summary>
    /// Tests the file names Peek Definition assigns to the scripts it generates. The names show up
    /// as editor tab titles, so they need to stay short, stay stable for a given object, and still
    /// keep two different objects apart.
    /// </summary>
    public class PeekDefinitionFileNameTests
    {
        private const string ServerA = "serverA";
        private const string ServerB = "serverB";

        [SetUp]
        [TearDown]
        public void ForgetAssignedNames()
        {
            PeekDefinitionFileNames.Reset();
        }

        private static Sql3PartIdentifier Identifier(
            string databaseName,
            string schemaName,
            string objectName)
        {
            return new Sql3PartIdentifier
            {
                DatabaseName = databaseName,
                SchemaName = schemaName,
                ObjectName = objectName
            };
        }

        private static string NameFor(
            string server,
            string database,
            string schema,
            string objectName)
        {
            Sql3PartIdentifier identifier = Identifier(database, schema, objectName);
            return Scripter.CreateFileName(identifier, server, database);
        }

        [Test]
        public void NameCarriesNoRandomSuffix()
        {
            // The 32 character GUID this replaced is what made tab titles unreadable
            Assert.AreEqual("master.dbo.myTable.sql", NameFor(ServerA, "master", "dbo", "myTable"));
        }

        [Test]
        public void RepeatedRequestsForOneObjectShareOneFile()
        {
            string first = NameFor(ServerA, "master", "dbo", "myTable");
            string second = NameFor(ServerA, "master", "dbo", "myTable");
            string third = NameFor(ServerA, "master", "dbo", "myTable");

            Assert.AreEqual(first, second);
            Assert.AreEqual(first, third);
        }

        [Test]
        public void DifferentObjectsInOneDatabaseGetTheirOwnFiles()
        {
            string table = NameFor(ServerA, "master", "dbo", "myTable");
            string view = NameFor(ServerA, "master", "dbo", "myView");

            Assert.AreEqual("master.dbo.myTable.sql", table);
            Assert.AreEqual("master.dbo.myView.sql", view);
        }

        [Test]
        public void SameObjectNameInDifferentSchemasDoesNotCollide()
        {
            string dbo = NameFor(ServerA, "master", "dbo", "myTable");
            string sales = NameFor(ServerA, "master", "sales", "myTable");

            Assert.AreEqual("master.dbo.myTable.sql", dbo);
            Assert.AreEqual("master.sales.myTable.sql", sales);
        }

        [Test]
        public void SameObjectNameInDifferentDatabasesDoesNotCollide()
        {
            // The database is part of the name, so these never need a numeric suffix
            string first = NameFor(ServerA, "dbOne", "dbo", "myTable");
            string second = NameFor(ServerA, "dbTwo", "dbo", "myTable");

            Assert.AreEqual("dbOne.dbo.myTable.sql", first);
            Assert.AreEqual("dbTwo.dbo.myTable.sql", second);
        }

        [Test]
        public void SameQualifiedNameOnAnotherServerFallsBackToANumberedFile()
        {
            // Nothing in the name distinguishes the servers, so the second one is numbered
            string first = NameFor(ServerA, "master", "dbo", "myTable");
            string second = NameFor(ServerB, "master", "dbo", "myTable");

            Assert.AreEqual("master.dbo.myTable.sql", first);
            Assert.AreEqual("master.dbo.myTable_2.sql", second);
        }

        [Test]
        public void EachAdditionalServerTakesTheNextNumber()
        {
            string first = NameFor(ServerA, "master", "dbo", "myTable");
            string second = NameFor(ServerB, "master", "dbo", "myTable");
            string third = NameFor("serverC", "master", "dbo", "myTable");

            Assert.AreEqual("master.dbo.myTable.sql", first);
            Assert.AreEqual("master.dbo.myTable_2.sql", second);
            Assert.AreEqual("master.dbo.myTable_3.sql", third);

            // and each of them stays put once assigned
            Assert.AreEqual(second, NameFor(ServerB, "master", "dbo", "myTable"));
            Assert.AreEqual(first, NameFor(ServerA, "master", "dbo", "myTable"));
        }

        [Test]
        public void UnqualifiedRequestUsesTheResolvedDatabase()
        {
            // The request did not name a database; the caller resolves it from the connection so
            // that the file name matches the definition actually scripted
            Sql3PartIdentifier unqualified = Identifier(null, "dbo", "myTable");

            string resolvedToOne = Scripter.CreateFileName(unqualified, ServerA, "dbOne");
            string resolvedToTwo = Scripter.CreateFileName(unqualified, ServerA, "dbTwo");

            Assert.AreEqual("dbOne.dbo.myTable.sql", resolvedToOne);
            Assert.AreEqual("dbTwo.dbo.myTable.sql", resolvedToTwo);
        }

        [Test]
        public void RequestWithoutADatabaseOmitsTheDatabaseSegment()
        {
            string name = Scripter.CreateFileName(Identifier(null, "dbo", "myTable"), ServerA, null);

            Assert.AreEqual("dbo.myTable.sql", name);
        }

        [Test]
        public void RequestWithoutASchemaOmitsTheSchemaSegment()
        {
            string name = Scripter.CreateFileName(Identifier(null, null, "myTable"), ServerA, null);

            Assert.AreEqual("myTable.sql", name);
        }

        [Test]
        public void ObjectsDifferingOnlyByCaseGetSeparateFiles()
        {
            // A case sensitive collation can hold both, and sharing one file would show the wrong
            // definition for one of them. The file system ignores case, hence the numbered name.
            string upper = NameFor(ServerA, "master", "dbo", "MyTable");
            string lower = NameFor(ServerA, "master", "dbo", "mytable");

            Assert.AreEqual("master.dbo.MyTable.sql", upper);
            Assert.AreEqual("master.dbo.mytable_2.sql", lower);
        }

        [Test]
        public void CharactersThatAreIllegalInAFileNameAreReplaced()
        {
            string name = NameFor(ServerA, "master", "dbo", "odd/name:here");

            Assert.AreEqual(-1, name.IndexOfAny(Path.GetInvalidFileNameChars()));
            Assert.AreEqual("master.dbo.odd_name_here.sql", name);
        }

        [Test]
        public void ObjectsThatSanitizeToTheSameNameStayApart()
        {
            // "a/b" and "a:b" both sanitize to "a_b", so the second still gets its own file
            string first = NameFor(ServerA, "master", "dbo", "a/b");
            string second = NameFor(ServerA, "master", "dbo", "a:b");

            Assert.AreEqual("master.dbo.a_b.sql", first);
            Assert.AreEqual("master.dbo.a_b_2.sql", second);
        }

        [Test]
        public void ConcurrentRequestsForOneObjectAgreeOnOneFile()
        {
            ConcurrentBag<string> names = new ConcurrentBag<string>();

            Parallel.For(0, 200, _ =>
                names.Add(NameFor(ServerA, "master", "dbo", "myTable")));

            Assert.AreEqual(1, names.Distinct().Count(), "every caller should see the same name");
            Assert.AreEqual("master.dbo.myTable.sql", names.First());
        }

        [Test]
        public void ConcurrentRequestsForDistinctObjectsNeverShareAFile()
        {
            const int objectCount = 200;
            ConcurrentBag<string> names = new ConcurrentBag<string>();

            // Every object has a distinct name, so no numeric suffix should be needed at all
            Parallel.For(0, objectCount, index =>
                names.Add(NameFor(ServerA, "master", "dbo", $"table{index}")));

            Assert.AreEqual(objectCount, names.Distinct().Count());
            CollectionAssert.IsEmpty(names.Where(name => name.Contains("_2.sql")));
        }

        [Test]
        public void ConcurrentRequestsThatAllWantOneNameAreNumberedWithoutDuplicates()
        {
            const int serverCount = 100;
            ConcurrentBag<string> names = new ConcurrentBag<string>();

            // Same qualified name on many servers: all of them want "master.dbo.myTable.sql"
            Parallel.For(0, serverCount, index =>
                names.Add(NameFor($"server{index}", "master", "dbo", "myTable")));

            Assert.AreEqual(serverCount, names.Distinct().Count(), "no two objects may share a file");
            CollectionAssert.Contains(names, "master.dbo.myTable.sql");
        }
    }

    /// <summary>
    /// Tests writing the generated script to its file.
    /// </summary>
    public class PeekDefinitionFileWriteTests
    {
        private string folder;

        [SetUp]
        public void CreateFolder()
        {
            folder = Path.Combine(Path.GetTempPath(), $"peek_write_{Guid.NewGuid():N}");
            Directory.CreateDirectory(folder);
        }

        [TearDown]
        public void RemoveFolder()
        {
            try
            {
                Directory.Delete(folder, recursive: true);
            }
            catch (IOException)
            {
            }
        }

        [Test]
        public void WritesTheScript()
        {
            string path = Path.Combine(folder, "definition.sql");

            Scripter.WriteScriptFile(path, "CREATE VIEW dbo.v AS SELECT 1");

            Assert.AreEqual("CREATE VIEW dbo.v AS SELECT 1", File.ReadAllText(path));
        }

        [Test]
        public void RewritingTheSameScriptLeavesTheFileAlone()
        {
            // Rewriting would make an editor that has the file open reload it, and would discard
            // anything the user had typed into that buffer
            string path = Path.Combine(folder, "definition.sql");
            const string script = "CREATE VIEW dbo.v AS SELECT 1";
            Scripter.WriteScriptFile(path, script);

            DateTime stamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(path, stamp);

            Scripter.WriteScriptFile(path, script);

            Assert.AreEqual(stamp, File.GetLastWriteTimeUtc(path));
        }

        [Test]
        public void AChangedDefinitionReplacesTheFileInPlace()
        {
            string path = Path.Combine(folder, "definition.sql");
            Scripter.WriteScriptFile(path, "CREATE VIEW dbo.v AS SELECT 1");

            Scripter.WriteScriptFile(path, "CREATE VIEW dbo.v AS SELECT 2");

            // Same path, so the editor tab is reused rather than a second one being opened
            Assert.AreEqual("CREATE VIEW dbo.v AS SELECT 2", File.ReadAllText(path));
            Assert.AreEqual(1, Directory.GetFiles(folder).Length);
        }

        [Test]
        public void ConcurrentWritesLeaveOneCompleteScript()
        {
            string path = Path.Combine(folder, "definition.sql");
            string shortScript = "CREATE VIEW dbo.v AS SELECT 1";
            string longScript = "CREATE VIEW dbo.v AS SELECT " + new string('9', 200000);
            List<string> allowed = new List<string> { shortScript, longScript };

            // Hammer one file from both sides. Writes must not interleave, so whichever one lands
            // last the file holds exactly one of the two scripts and never a mixture.
            Parallel.For(0, 200, index =>
                Scripter.WriteScriptFile(path, index % 2 == 0 ? shortScript : longScript));

            CollectionAssert.Contains(allowed, File.ReadAllText(path));
            Assert.AreEqual(1, Directory.GetFiles(folder).Length);
        }
    }
}
