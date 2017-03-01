//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common.Baselined
{
    /// <summary>
    /// This class serves as the base class for all baselined tests
    /// It will provide easy services for you to interact with your test files and their baselines
    /// </summary>
    public abstract class BaselinedTest
    {
        /// <summary>
        /// Holds the extension for the TestScripts
        /// </summary>
        private string _testScriptExtension;

        /// <summary>
        /// Holds the extensionf or the Baseline files
        /// </summary>
        private string _baselineExtension;

        /// <summary>
        /// Holds the path to the base location of both TestScripts and Baselines
        /// </summary>
        private string _testCategoryName;

        /// <summary>
        /// Holds the ROOT Dir for trace output
        /// </summary>
        private string _traceOutputDir;

        /// <summary>
        /// Holds the prefix for the baseline
        /// </summary>
        private string _baselinePrefix;

        /// <summary>
        /// Holds the prefix for the Testscript
        /// </summary>
        private string _testscriptPrefix;

        /// <summary>
        /// Holds the name of the current test
        /// </summary>
        private string _currentTestname;

        private string _baselineSubDir = string.Empty;

        public const string TestScriptDirectory = @"Testscripts\";
        public const string BaselineDirectory = @"Baselines\";

        /// <summary>
        /// Gets/Sets the extension for the Testscript files
        /// </summary>
        public string TestscriptFileExtension
        {
            get
            {
                return _testScriptExtension;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("TestscriptFileExtension needs a value");
                _testScriptExtension = value;
            }
        }

        /// <summary>
        /// Gets/Sets the extension for the Baseline files
        /// </summary>
        public string BaselineFileExtension
        {
            get
            {
                return _baselineExtension;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("BaselineFileExtension needs a value");
                _baselineExtension = value;
            }
        }

        /// <summary>
        /// Gets/Sets the path to the base location of both test scripts and baseline files
        /// </summary>
        /// <remarks>
        /// Just use the SubDir name
        /// TestScripts should be in FileBaseLocation\Testscripts; and Baselines should be in FileBaseLocation\Baselines
        /// The value of this will be appended to ROOT_DIR (QA\SrcUTest\Common)
        /// </remarks>
        public string CategoryName
        {
            get
            {
                return _testCategoryName;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("FileBaseLocation needs a value");
                _testCategoryName = value;
            }
        }

        /// <summary>
        /// Gets/Sets the output base directory for trace output (null = no trace output)
        /// </summary>
        public string TraceOutputDirectory
        {
            get
            {
                return _traceOutputDir;
            }
            set
            {
                _traceOutputDir = value;
            }
        }

        /// <summary>
        /// Gets the full path of where the files will be pulled from
        /// </summary>
        public string FilesLocation
        {
            get
            {
                return Path.Combine(RunEnvironmentInfo.GetTestDataLocation(), CategoryName, TestScriptDirectory);
            }
        }

        /// <summary>
        /// Gets or Sets the sub directory in Baselines where the exected baseline results are located
        /// </summary>
        public string BaselinesSubdir 
        {
            get 
            {
                if (this._baselineSubDir == null)
                    this._baselineSubDir = string.Empty;
                return this._baselineSubDir; 
            }
            set { this._baselineSubDir = value; }
        }

        /// <summary>
        /// Gets the full path of where the baseline files will be pulled from
        /// </summary>
        public string BaselineFilePath
        {
            get
            {
                return Path.Combine(RunEnvironmentInfo.GetTestDataLocation(), CategoryName, Path.Combine( BaselineDirectory, BaselinesSubdir ));
            }
        }

        /// <summary>
        /// Gets the full path of where the Trace will output
        /// </summary>
        public string TraceFilePath
        {
            get
            {
                return Path.Combine(Path.GetFullPath(TraceOutputDirectory), this.CategoryName, this.BaselinesSubdir);
            }
        }

        /// <summary>
        /// Gets/Sets the prefix used for baseline files
        /// </summary>
        public string BaselinePrefix
        {
            get
            {
                return _baselinePrefix;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("BaselinePrefix needs a value");
                _baselinePrefix = value;
            }
        }

        /// <summary>
        /// Gets/Sets the prefix used for testscript files
        /// </summary>
        public string TestscriptPrefix
        {
            get
            {
                return _testscriptPrefix;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("TestscriptPrefix needs a value");
                _testscriptPrefix = value;
            }
        }

        /// <summary>
        /// Gets/Sets the name of the current test
        /// </summary>
        public string CurrentTestName
        {
            get
            {
                return _currentTestname;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public BaselinedTest()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the class
        /// </summary>
        private void Initialize()
        {
            _testScriptExtension = _baselineExtension = "txt"; //default to txt
            _testCategoryName = null;
            string projectPath = Environment.GetEnvironmentVariable(Constants.ProjectPath);
            if (projectPath != null)
            {
                _traceOutputDir = Path.Combine(projectPath, "trace");
            }
            else
            {
                _traceOutputDir = Environment.ExpandEnvironmentVariables(@"%SystemDrive%\trace\");
            }
            _baselinePrefix = "BL";
            _testscriptPrefix = "TS";
        }

        /// <summary>
        /// This method should be called whenever you do a [TestInitialize]
        /// </summary>
        public virtual void TestInitialize()
        {

            if (string.IsNullOrEmpty(_testCategoryName))
                throw new ArgumentException("Set CategoryName to the name of the directory containing your Testscripts and Baseline files");

            if (!Directory.Exists(FilesLocation))
                throw new FileNotFoundException(string.Format("Path to Testscripts ([{0}]) does not exist.", FilesLocation));
            if (!Directory.Exists(BaselineFilePath))
                throw new FileNotFoundException(string.Format("Path to Baseline Files [{0}] does not exist.", BaselineFilePath));
            if (!string.IsNullOrEmpty(TraceFilePath) && !Directory.Exists(TraceFilePath))   //if this does not exist, then we want it (pronto)
                Directory.CreateDirectory(TraceFilePath);

        }

        /// <summary>
        /// Compares two strings and gives appropriate output
        /// </summary>
        /// <param name="actualContent">Actual string</param>
        /// <param name="baselineContent">Expected string</param>
        /// <remarks>Fails test if strings do not match; comparison is done using an InvariantCulture StringComparer</remarks>
        public void CompareActualWithBaseline(string actualContent, string baselineContent)
        {

            int _compareResult = string.Compare(actualContent, baselineContent, StringComparison.OrdinalIgnoreCase);
            if (_compareResult != 0)
            {
                Trace.WriteLine("Debug Info:");
                Trace.WriteLine("========BEGIN=EXPECTED========");
                Trace.WriteLine(baselineContent);
                Trace.WriteLine("=========END=EXPECTED=========");
                Trace.WriteLine("=========BEGIN=ACTUAL=========");
                Trace.WriteLine(actualContent);
                Trace.WriteLine("==========END=ACTUAL==========");
                Assert.True(false, string.Format("Comparison failed! (actualContent {0} baselineContent)", (_compareResult < 0 ? "<" : ">")));    //we already know it is not equal
            }
            else
            {
                Trace.WriteLine("Compare match! All is fine...");
            }
        }

        /// <summary>
        /// Gets the name of the testscript with the provided name
        /// </summary>
        /// <param name="name">Name of the test</param>
        /// <returns>the path to the baseline file</returns>
        /// <remarks>Asserts that file exists</remarks>
        public string GetTestscriptFilePath(string name)
        {
            string retVal = Path.Combine(FilesLocation, string.Format("{0}-{1}.{2}", TestscriptPrefix, name, TestscriptFileExtension));
            Assert.True(File.Exists(retVal), string.Format("TestScript [{0}] does not exist", retVal));
            return retVal;
        }

        /// <summary>
        /// Gets the name of the test script with the provided name and the provided index
        /// </summary>
        /// <param name="name">Name of the test</param>
        /// <param name="index">File index</param>
        /// <returns>the path to the baseline file</returns>
        /// <remarks>Asserts that file exists</remarks>
        public string GetTestscriptFilePath(string name, int index)
        {
            string retVal = Path.Combine(FilesLocation, string.Format("{0}-{1}{2}.{3}", TestscriptPrefix, name, index.ToString(), TestscriptFileExtension));
            Assert.True(File.Exists(retVal), string.Format("TestScript [{0}] does not exist", retVal));
            return retVal;
        }

        /// <summary>
        /// Gets the formatted baseline file name
        /// </summary>
        /// <param name="name">Name of the test</param>
        public string GetBaselineFileName(string name)
        {
            return string.Format("{0}-{1}.{2}", BaselinePrefix, name, BaselineFileExtension);
        }

        /// <summary>
        /// Gets the file path to the baseline file for the named case
        /// </summary>
        /// <param name="name">Name of the test</param>
        /// <returns>the path to the baseline file</returns>
        /// <remarks>Asserts that file exists</remarks>
        public string GetBaselineFilePath(string name, bool assertIfNotFound)
        {
            string retVal = Path.Combine(BaselineFilePath, GetBaselineFileName(name));

            if (assertIfNotFound)
            {
                Assert.True(File.Exists(retVal), string.Format("Baseline [{0}] does not exist", retVal));
            }
            return retVal;
        }

        public string GetBaselineFilePath(string name)
        {
            return GetBaselineFilePath(name, true);
        }

        /// <summary>
        /// Gets the contents of a file
        /// </summary>
        /// <param name="path">Path of the file to read</param>
        /// <returns>The contents of the file</returns>
        public string GetFileContent(string path)
        {
            Trace.WriteLine(string.Format("GetFileContent for [{0}]", Path.GetFullPath(path)));

            using (StreamReader sr = new StreamReader(File.Open(path, FileMode.Open), Encoding.UTF8))
            {
                return sr.ReadToEnd();
            }
        }

        /// <summary>
        /// Dumps the text to a Trace file
        /// </summary>
        /// <param name="testName">Test name used to create file name</param>
        /// <param name="text">Text to dump to the trace file</param>
        /// <remarks>Overwrites whatever is already in the file (if anything)</remarks>
        public string DumpToTrace(string testName, string text)
        {
            if (string.IsNullOrEmpty(TraceFilePath))
            {
                return string.Empty; //nothing to do
            }

            string traceFile = Path.Combine(TraceFilePath, GetBaselineFileName(testName));

            if (File.Exists(traceFile))
            {
                Trace.Write(string.Format("Overwriting existing trace file [{0}]", traceFile));
                File.Delete(traceFile);
            }
            else
            {
                Trace.Write(string.Format("Dumping to trace file [{0}]", traceFile));
            }

            if (Directory.Exists(TraceFilePath) == false)
            {
                Directory.CreateDirectory(TraceFilePath);
            }
            WriteTraceFile(traceFile, text);
            return traceFile;
        }

        /// <summary>
        /// Writes the context to the trace file
        /// </summary>
        /// <param name="traceFile">The file name for the trace output</param>
        /// <param name="text">The content for the trace file</param>
        public void WriteTraceFile(string traceFile, string text)
        {
            File.WriteAllText(traceFile, text);
        }

        /// <summary>
        /// Converts a string to a stream
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private Stream GetStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// Starts the actual running of the test after nicely initializing
        /// </summary>
        /// <param name="testname">Name of the test</param>
        public void Start(string testname)
        {
            Trace.WriteLine(string.Format("Starting test named [{0}]", testname));
            _currentTestname = testname;

            Run();

            Trace.WriteLine("Test Completed");
        }

        /// <summary>
        /// Runs the actual test
        /// </summary>
        /// <remarks>Override this method to put in your test logic</remarks>
        public abstract void Run();

    }
}
