//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Interface for a factory that constructs a reader and writer pair reading and writing to a
    /// service buffer file.
    /// </summary>
    public interface IServiceBufferFileStreamFactory
    {
        /// <summary>
        /// Creates a temporary file for the service buffer file.
        /// </summary>
        /// <returns>The path to the service buffer file.</returns>
        string CreateFile();

        /// <summary>
        /// Constructs a reader for reading from a service buffer file.
        /// </summary>
        /// <param name="fileName">Path to the service buffer file.</param>
        /// <returns>A reader for reading from the service buffer file.</returns>
        IFileStreamReader GetReader(string fileName);

        /// <summary>
        /// Constructs a writer for writing to a service buffer file.
        /// </summary>
        /// <param name="fileName">Path to the service buffer file.</param>
        /// <returns>A writer for writing to the service buffer file.</returns>
        IServiceBufferFileStreamWriter GetWriter(string fileName);

        /// <summary>
        /// Cleans up a service buffer file that was created by this factory.
        /// </summary>
        /// <param name="fileName">Path to the service buffer file.</param>
        void DisposeFile(string fileName);
    }
}
