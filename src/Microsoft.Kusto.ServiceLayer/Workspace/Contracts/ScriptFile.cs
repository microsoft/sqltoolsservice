//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Kusto.ServiceLayer.Workspace.Contracts
{
    /// <summary>
    /// Contains the details and contents of an open script file.
    /// </summary>
    public class ScriptFile
    {
        #region Properties

        /// <summary>
        /// Gets a unique string that identifies this file.  At this time,
        /// this property returns a normalized version of the value stored
        /// in the ClientUri property.
        /// </summary>
        public string Id
        {
            get { return this.ClientUri.ToLower(); }
        }

        /// <summary>
        /// Gets the path at which this file resides.
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// Gets or sets the URI which the editor client uses to identify this file.
        /// Setter for testing purposes only
        /// virtual to allow mocking.
        /// </summary>
        public virtual string ClientUri { get; internal set; }

        /// <summary>
        /// Gets or sets a boolean that determines whether
        /// semantic analysis should be enabled for this file.
        /// For internal use only.
        /// </summary>
        internal bool IsAnalysisEnabled { get; set; }

        /// <summary>
        /// Gets a boolean that determines whether this file is
        /// in-memory or not (either unsaved or non-file content).
        /// </summary>
        public bool IsInMemory { get; private set; }

        /// <summary>
        /// Gets or sets a string containing the full contents of the file.
        /// Setter for testing purposes only
        /// </summary>
        public virtual string Contents 
        {
            get
            {
                return string.Join("\r\n", FileLines);
            }
            set
            {
                FileLines = value != null ? value.Split('\n') : null;
            }
        }

        /// <summary>
        /// Gets a BufferRange that represents the entire content
        /// range of the file.
        /// </summary>
        public BufferRange FileRange { get; private set; }

        /// <summary>
        /// Gets the list of syntax markers found by parsing this
        /// file's contents.
        /// </summary>
        public ScriptFileMarker[] SyntaxMarkers
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the list of strings for each line of the file.
        /// </summary>
        internal IList<string> FileLines
        {
            get;
            private set;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Add a default constructor for testing
        /// </summary>
        public ScriptFile()
        {
            ClientUri = "test.sql";
        }

        /// <summary>
        /// Creates a new ScriptFile instance by reading file contents from
        /// the given TextReader.
        /// </summary>
        /// <param name="filePath">The path at which the script file resides.</param>
        /// <param name="clientUri">The URI which the client uses to identify the file.</param>
        /// <param name="textReader">The TextReader to use for reading the file's contents.</param>
        public ScriptFile(
            string filePath,
            string clientUri,
            TextReader textReader)
        {
            FilePath = filePath;
            ClientUri = clientUri;
            IsAnalysisEnabled = true;
            IsInMemory = Workspace.IsPathInMemoryOrNonFileUri(filePath);

            SetFileContents(textReader.ReadToEnd());
        }

        /// <summary>
        /// Creates a new ScriptFile instance with the specified file contents.
        /// </summary>
        /// <param name="filePath">The path at which the script file resides.</param>
        /// <param name="clientUri">The path which the client uses to identify the file.</param>
        /// <param name="initialBuffer">The initial contents of the script file.</param>
        public ScriptFile(
            string filePath,
            string clientUri,
            string initialBuffer)
        {
            FilePath = filePath;
            ClientUri = clientUri;
            IsAnalysisEnabled = true;

            SetFileContents(initialBuffer);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets a range of lines from the file's contents. Virtual method to allow for
        /// mocking.
        /// </summary>
        /// <param name="bufferRange">The buffer range from which lines will be extracted.</param>
        /// <returns>An array of strings from the specified range of the file.</returns>
        public virtual string[] GetLinesInRange(BufferRange bufferRange)
        {
            ValidatePosition(bufferRange.Start);
            ValidatePosition(bufferRange.End);

            List<string> linesInRange = new List<string>();

            int startLine = bufferRange.Start.Line,
                endLine = bufferRange.End.Line;

            for (int line = startLine; line <= endLine; line++)
            {
                string currentLine = FileLines[line - 1];
                int startColumn =
                    line == startLine
                    ? bufferRange.Start.Column
                    : 1;
                int endColumn =
                    line == endLine
                    ? bufferRange.End.Column
                    : currentLine.Length + 1;

                currentLine =
                    currentLine.Substring(
                        startColumn - 1,
                        endColumn - startColumn);

                linesInRange.Add(currentLine);
            }

            return linesInRange.ToArray();
        }

        /// <summary>
        /// Throws ArgumentOutOfRangeException if the given position is outside
        /// of the file's buffer extents.
        /// </summary>
        /// <param name="bufferPosition">The position in the buffer to be validated.</param>
        public void ValidatePosition(BufferPosition bufferPosition)
        {
            ValidatePosition(
                bufferPosition.Line,
                bufferPosition.Column);
        }

        /// <summary>
        /// Throws ArgumentOutOfRangeException if the given position is outside
        /// of the file's buffer extents.
        /// </summary>
        /// <param name="line">The 1-based line to be validated.</param>
        /// <param name="column">The 1-based column to be validated.</param>
        public void ValidatePosition(int line, int column)
        {
            if (line < 1 || line > FileLines.Count + 1)
            {
                throw new ArgumentOutOfRangeException(nameof(line), SR.WorkspaceServicePositionLineOutOfRange);
            }

            // The maximum column is either one past the length of the string
            // or 1 if the string is empty.
            string lineString = FileLines[line - 1];
            int maxColumn = lineString.Length > 0 ? lineString.Length + 1 : 1;

            if (column < 1 || column > maxColumn)
            {
                throw new ArgumentOutOfRangeException(nameof(column), SR.WorkspaceServicePositionColumnOutOfRange(line));
            }
        }

        /// <summary>
        /// Applies the provided FileChange to the file's contents
        /// </summary>
        /// <param name="fileChange">The FileChange to apply to the file's contents.</param>
        public void ApplyChange(FileChange fileChange)
        {
            ValidatePosition(fileChange.Line, fileChange.Offset);
            ValidatePosition(fileChange.EndLine, fileChange.EndOffset);

            // Break up the change lines
            string[] changeLines = fileChange.InsertString.Split('\n');

            // Get the first fragment of the first line
            string firstLineFragment = 
                FileLines[fileChange.Line - 1]
                    .Substring(0, fileChange.Offset - 1);

            // Get the last fragment of the last line
            string endLine = FileLines[fileChange.EndLine - 1];
            string lastLineFragment = 
                endLine.Substring(
                    fileChange.EndOffset - 1, 
                    (FileLines[fileChange.EndLine - 1].Length - fileChange.EndOffset) + 1);

            // Remove the old lines
            for (int i = 0; i <= fileChange.EndLine - fileChange.Line; i++)
            {
                FileLines.RemoveAt(fileChange.Line - 1); 
            }

            // Build and insert the new lines
            int currentLineNumber = fileChange.Line;
            for (int changeIndex = 0; changeIndex < changeLines.Length; changeIndex++)
            {
                // Since we split the lines above using \n, make sure to
                // trim the ending \r's off as well.
                string finalLine = changeLines[changeIndex].TrimEnd('\r');

                // Should we add first or last line fragments?
                if (changeIndex == 0)
                {
                    // Append the first line fragment
                    finalLine = firstLineFragment + finalLine;
                }
                if (changeIndex == changeLines.Length - 1)
                {
                    // Append the last line fragment
                    finalLine = finalLine + lastLineFragment;
                }

                FileLines.Insert(currentLineNumber - 1, finalLine);
                currentLineNumber++;
            }
        }

        /// <summary>
        /// Set the script files contents
        /// </summary>
        /// <param name="fileContents"></param>
        public void SetFileContents(string fileContents)
        {
            // Split the file contents into lines and trim
            // any carriage returns from the strings.
            FileLines =
                fileContents
                    .Split('\n')
                    .Select(line => line.TrimEnd('\r'))
                    .ToList();
        }

        #endregion
    }
}
