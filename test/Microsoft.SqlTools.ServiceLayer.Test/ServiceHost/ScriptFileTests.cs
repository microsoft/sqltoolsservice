//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.ServiceHost
{
    /// <summary>
    /// ScriptFile test case
    /// </summary>
    public class ScriptFileTests
    {
        internal static object fileLock = new object();

        private static readonly string query = 
            "SELECT * FROM sys.objects as o1" + Environment.NewLine +
            "SELECT * FROM sys.objects as o2" + Environment.NewLine +
            "SELECT * FROM sys.objects as o3" + Environment.NewLine;

        internal static ScriptFile GetTestScriptFile(string initialText = null)
        {
            if (initialText == null)
            {
                initialText = ScriptFileTests.query;
            }

            string ownerUri = System.IO.Path.GetTempFileName();
           
            // Write the query text to a backing file
            lock (fileLock)
            {
                System.IO.File.WriteAllText(ownerUri, initialText);
            }

            return new ScriptFile(ownerUri, ownerUri, initialText);
        }

        /// <summary>
        /// Validate GetLinesInRange with invalid range   
        /// </summary>
        [Fact]
        public void GetLinesInRangeWithInvalidRangeTest()
        {
            ScriptFile scriptFile = GetTestScriptFile();

            bool exceptionRaised = false;
            try
            {
                scriptFile.GetLinesInRange(
                    new BufferRange(
                        new BufferPosition(1, 0), 
                        new BufferPosition(2, 0)));
            }
            catch (ArgumentOutOfRangeException)
            {
                exceptionRaised = true;
            }
            
            Assert.True(exceptionRaised, "ArgumentOutOfRangeException raised for invalid index");

        }

        /// <summary>
        /// Validate GetLinesInRange       
        /// </summary>
        [Fact]
        public void GetLinesInRangeTest()
        {
            ScriptFile scriptFile = GetTestScriptFile();

            string id = scriptFile.Id;
            Assert.True(!string.IsNullOrWhiteSpace(id));

            BufferRange range =scriptFile.FileRange;
            Assert.Null(range);

            string[] lines = scriptFile.GetLinesInRange(
                    new BufferRange(
                        new BufferPosition(2, 1), 
                        new BufferPosition(2, 7)));

            Assert.True(lines.Length == 1, "One line in range");
            Assert.True(lines[0].Equals("SELECT"), "Range text is correct");

            string[] queryLines = query.Split('\n');

            string line = scriptFile.GetLine(2);
            Assert.True(queryLines[1].StartsWith(line), "GetLine text is correct");
        }

        [Fact]
        public void GetOffsetAtPositionTest()
        {
            ScriptFile scriptFile = GetTestScriptFile();
            int offset = scriptFile.GetOffsetAtPosition(2, 5);
            int expected = 35 + Environment.NewLine.Length;
            Assert.True(offset == expected, "Offset is at expected location");

            BufferPosition position = scriptFile.GetPositionAtOffset(offset);
            Assert.True(position.Line == 2 && position.Column == 5, "Position is at expected location");
        }

        [Fact]
        public void GetRangeBetweenOffsetsTest()
        {
            ScriptFile scriptFile = GetTestScriptFile();
            BufferRange range = scriptFile.GetRangeBetweenOffsets(
                scriptFile.GetOffsetAtPosition(2, 1),
                scriptFile.GetOffsetAtPosition(2, 7));
            Assert.NotNull(range);
        }
        
        [Fact]
        public void CanApplySingleLineInsert()
        {
            this.AssertFileChange(
                "This is a test.",
                "This is a working test.",
                new FileChange
                {
                    Line = 1,
                    EndLine = 1,
                    Offset = 10,
                    EndOffset = 10,
                    InsertString = " working"
                });
        }

        [Fact]
        public void CanApplySingleLineReplace()
        {
            this.AssertFileChange(
                "This is a potentially broken test.",
                "This is a working test.",
                new FileChange
                {
                    Line = 1,
                    EndLine = 1,
                    Offset = 11,
                    EndOffset = 29,
                    InsertString = "working"
                });
        }

        [Fact]
        public void CanApplySingleLineDelete()
        {
            this.AssertFileChange(
                "This is a test of the emergency broadcasting system.",
                "This is a test.",
                new FileChange
                {
                    Line = 1,
                    EndLine = 1,
                    Offset = 15,
                    EndOffset = 52,
                    InsertString = ""
                });
        }

        [Fact]
        public void CanApplyMultiLineInsert()
        {
            this.AssertFileChange(
                "first\r\nsecond\r\nfifth",
                "first\r\nsecond\r\nthird\r\nfourth\r\nfifth",
                new FileChange
                {
                    Line = 3,
                    EndLine = 3,
                    Offset = 1,
                    EndOffset = 1,
                    InsertString = "third\r\nfourth\r\n"
                });
        }

        [Fact]
        public void CanApplyMultiLineReplace()
        {
            this.AssertFileChange(
                "first\r\nsecoXX\r\nXXfth",
                "first\r\nsecond\r\nthird\r\nfourth\r\nfifth",
                new FileChange
                {
                    Line = 2,
                    EndLine = 3,
                    Offset = 5,
                    EndOffset = 3,
                    InsertString = "nd\r\nthird\r\nfourth\r\nfi"
                });
        }

        [Fact]
        public void CanApplyMultiLineReplaceWithRemovedLines()
        {
            this.AssertFileChange(
                "first\r\nsecoXX\r\nREMOVE\r\nTHESE\r\nLINES\r\nXXfth",
                "first\r\nsecond\r\nthird\r\nfourth\r\nfifth",
                new FileChange
                {
                    Line = 2,
                    EndLine = 6,
                    Offset = 5,
                    EndOffset = 3,
                    InsertString = "nd\r\nthird\r\nfourth\r\nfi"
                });
        }

        [Fact]
        public void CanApplyMultiLineDelete()
        {
            this.AssertFileChange(
                "first\r\nsecond\r\nREMOVE\r\nTHESE\r\nLINES\r\nthird",
                "first\r\nsecond\r\nthird",
                new FileChange
                {
                    Line = 3,
                    EndLine = 6,
                    Offset = 1,
                    EndOffset = 1,
                    InsertString = ""
                });
        }

        [Fact]
        public void ThrowsExceptionWithEditOutsideOfRange()
        {
            Assert.Throws(
                typeof(ArgumentOutOfRangeException),
                () =>
                {
                    this.AssertFileChange(
                        "first\r\nsecond\r\nREMOVE\r\nTHESE\r\nLINES\r\nthird",
                        "first\r\nsecond\r\nthird",
                        new FileChange
                        {
                            Line = 3,
                            EndLine = 7,
                            Offset = 1,
                            EndOffset = 1,
                            InsertString = ""
                        });
                });
        }

        private void AssertFileChange(
            string initialString,
            string expectedString,
            FileChange fileChange)
        {
            // Create an in-memory file from the StringReader
            ScriptFile fileToChange = GetTestScriptFile(initialString);

            // Apply the FileChange and assert the resulting contents
            fileToChange.ApplyChange(fileChange);
            Assert.Equal(expectedString, fileToChange.Contents);
        }
    }

    public class ScriptFileGetLinesTests
    {
        private ScriptFile scriptFile;

        private const string TestString = "Line One\r\nLine Two\r\nLine Three\r\nLine Four\r\nLine Five";
        private readonly string[] TestStringLines =
            TestString.Split(
                new string[] { "\r\n" },
                StringSplitOptions.None);

        public ScriptFileGetLinesTests()
        {
            this.scriptFile =
                ScriptFileTests.GetTestScriptFile(
                    "Line One\r\nLine Two\r\nLine Three\r\nLine Four\r\nLine Five\r\n");
        }

        [Fact]
        public void CanGetWholeLine()
        {
            string[] lines =
                this.scriptFile.GetLinesInRange(
                    new BufferRange(5, 1, 5, 10));

            Assert.Equal(1, lines.Length);
            Assert.Equal("Line Five", lines[0]);
        }

        [Fact]
        public void CanGetMultipleWholeLines()
        {
            string[] lines =
                this.scriptFile.GetLinesInRange(
                    new BufferRange(2, 1, 4, 10));

            Assert.Equal(TestStringLines.Skip(1).Take(3), lines);
        }

        [Fact]
        public void CanGetSubstringInSingleLine()
        {
            string[] lines =
                this.scriptFile.GetLinesInRange(
                    new BufferRange(4, 3, 4, 8));

            Assert.Equal(1, lines.Length);
            Assert.Equal("ne Fo", lines[0]);
        }

        [Fact]
        public void CanGetEmptySubstringRange()
        {
            string[] lines =
                this.scriptFile.GetLinesInRange(
                    new BufferRange(4, 3, 4, 3));

            Assert.Equal(1, lines.Length);
            Assert.Equal("", lines[0]);
        }

        [Fact]
        public void CanGetSubstringInMultipleLines()
        {
            string[] expectedLines = new string[]
            {
                "Two",
                "Line Three",
                "Line Fou"
            };

            string[] lines =
                this.scriptFile.GetLinesInRange(
                    new BufferRange(2, 6, 4, 9));

            Assert.Equal(expectedLines, lines);
        }

        [Fact]
        public void CanGetRangeAtLineBoundaries()
        {
            string[] expectedLines = new string[]
            {
                "",
                "Line Three",
                ""
            };

            string[] lines =
                this.scriptFile.GetLinesInRange(
                    new BufferRange(2, 9, 4, 1));

            Assert.Equal(expectedLines, lines);
        }
    }

    public class ScriptFilePositionTests
    {
        private ScriptFile scriptFile;

        public ScriptFilePositionTests()
        {
            this.scriptFile =
                ScriptFileTests.GetTestScriptFile(@"
First line
  Second line is longer
    Third line
");
        }

        [Fact]
        public void CanOffsetByLine()
        {
            this.AssertNewPosition(
                1, 1,
                2, 0,
                3, 1);

            this.AssertNewPosition(
                3, 1,
                -2, 0,
                1, 1);
        }

        [Fact]
        public void CanOffsetByColumn()
        {
            this.AssertNewPosition(
                2, 1,
                0, 2,
                2, 3);

            this.AssertNewPosition(
                2, 5,
                0, -3,
                2, 2);
        }

        [Fact]
        public void ThrowsWhenPositionOutOfRange()
        {
            // Less than line range
            Assert.Throws(
                typeof(ArgumentOutOfRangeException),
                () =>
                {
                    scriptFile.CalculatePosition(
                        new BufferPosition(1, 1),
                        -10, 0);
                });

            // Greater than line range
            Assert.Throws(
                typeof(ArgumentOutOfRangeException),
                () =>
                {
                    scriptFile.CalculatePosition(
                        new BufferPosition(1, 1),
                        10, 0);
                });

            // Less than column range
            Assert.Throws(
                typeof(ArgumentOutOfRangeException),
                () =>
                {
                    scriptFile.CalculatePosition(
                        new BufferPosition(1, 1),
                        0, -10);
                });

            // Greater than column range
            Assert.Throws(
                typeof(ArgumentOutOfRangeException),
                () =>
                {
                    scriptFile.CalculatePosition(
                        new BufferPosition(1, 1),
                        0, 10);
                });
        }

        [Fact]
        public void CanFindBeginningOfLine()
        {
            this.AssertNewPosition(
                4, 12,
                pos => pos.GetLineStart(),
                4, 5);
        }

        [Fact]
        public void CanFindEndOfLine()
        {
            this.AssertNewPosition(
                4, 12,
                pos => pos.GetLineEnd(),
                4, 15);
        }

        [Fact]
        public void CanComposePositionOperations()
        {
            this.AssertNewPosition(
                4, 12,
                pos => pos.AddOffset(-1, 1).GetLineStart(),
                3, 3);
        }

        private void AssertNewPosition(
            int originalLine, int originalColumn,
            int lineOffset, int columnOffset,
            int expectedLine, int expectedColumn)
        {
            this.AssertNewPosition(
                originalLine, originalColumn,
                pos => pos.AddOffset(lineOffset, columnOffset),
                expectedLine, expectedColumn);
        }

        private void AssertNewPosition(
            int originalLine, int originalColumn,
            Func<FilePosition, FilePosition> positionOperation,
            int expectedLine, int expectedColumn)
        {
            var newPosition =
                positionOperation(
                    new FilePosition(
                        this.scriptFile,
                        originalLine,
                        originalColumn));

            Assert.Equal(expectedLine, newPosition.Line);
            Assert.Equal(expectedColumn, newPosition.Column);
        }        


    }
}
