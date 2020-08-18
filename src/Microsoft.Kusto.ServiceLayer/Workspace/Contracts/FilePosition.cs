//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Kusto.ServiceLayer.Workspace.Contracts
{
    /// <summary>
    /// Provides details and operations for a buffer position in a
    /// specific file.
    /// </summary>
    public class FilePosition : BufferPosition
    {
        #region Private Fields

        private ScriptFile scriptFile;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new FilePosition instance for the 1-based line and
        /// column numbers in the specified file.
        /// </summary>
        /// <param name="scriptFile">The ScriptFile in which the position is located.</param>
        /// <param name="line">The 1-based line number in the file.</param>
        /// <param name="column">The 1-based column number in the file.</param>
        public FilePosition(
            ScriptFile scriptFile,
            int line,
            int column)
                : base(line, column)
        {
            this.scriptFile = scriptFile;
        }

        #endregion

        #region Public Methods

        #endregion
   
    }
}

