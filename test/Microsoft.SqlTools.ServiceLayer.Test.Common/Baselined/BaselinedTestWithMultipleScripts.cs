//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common.Baselined
{
    public abstract class BaselinedTestWithMultipleScripts : BaselinedTest
    {
        /// <summary>
        /// Holds the number of files that are associated with this test
        /// </summary>
        private int _fileCount;

        /// <summary>
        /// Gets or Sets the amount of scripts to process
        /// </summary>
        public int FileCount
        {
            get
            {
                return _fileCount;
            }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("FileCount", value, "FileCount must be > 0");
                _fileCount = value;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public BaselinedTestWithMultipleScripts()
            : base()
        {
            //set invalid value
            _fileCount = 0;
        }

        /// <summary>
        /// Runs the test
        /// </summary>
        public override void Run()
        {
            //little self-assigning sanity check there (if invalid value, it will throw)
            FileCount = FileCount;

            //process all files
            for (int n = 0; n < FileCount; n++)
            {
                ProcessFile(GetTestscriptFilePath(this.CurrentTestName, n));
            }

            PostProcessFiles();

            Verify();
        }

        /// <summary>
        /// Starts a test with the specified fileCount
        /// </summary>
        /// <param name="name">Name of the test</param>
        /// <param name="fileCount">Number of files</param>
        public void Start(string name, int fileCount)
        {
            FileCount = fileCount;
            base.Start(name);
        }

        /// <summary>
        /// This method gives you an opportunity to handle one specific file
        /// </summary>
        /// <param name="filePath">Path to the current file</param>
        public abstract void ProcessFile(string filePath); 

        /// <summary>
        /// This method gives you an opportunity to perform any actions after files are processed
        /// </summary>
        public virtual void PostProcessFiles()
        {
        }

        /// <summary>
        /// This method will be called when all files have been processed; add verification logic here
        /// </summary>
        public abstract void Verify(); 

    }
}
