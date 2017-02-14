//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    public class TestUtilities
    {
        
        public static void CompareTestFiles(FileInfo baselinePath, FileInfo outputPath, int maxDiffLines = -1 /* unlimited */)
        {
            if (!baselinePath.Exists)
            {
                throw new ComparisonFailureException("echo Test Failed:   Baseline file " + baselinePath.FullName + " does not exist" +
                   Environment.NewLine + Environment.NewLine + "echo test > \"" + baselinePath.FullName + "\"");
            }

            if (!outputPath.Exists)
            {
                throw new ComparisonFailureException("Test Failed:  output file " + outputPath.FullName + " doesn't exist.");
            }

            string baseline = ReadTextAndNormalizeLineEndings(baselinePath.FullName);
            string actual = ReadTextAndNormalizeLineEndings(outputPath.FullName);

            if (baseline.CompareTo(actual) != 0)
            {
                string header = "Test Failed:  Baseline file " + baselinePath.FullName + " differs from output file " + outputPath.FullName + "\r\n\r\n";
                string editAndCopyMessage =
                    "\r\n\r\n copy \"" + outputPath.FullName + "\" \"" + baselinePath.FullName + "\"" +
                    "\r\n\r\n";
                string diffCmdMessage =
                    "code --diff \"" + baselinePath.FullName + "\" \"" + outputPath.FullName + "\"" +
                    "\r\n\r\n";
                
                string diffContents = FindFirstDifference(baseline, actual);
                throw new ComparisonFailureException(header + diffCmdMessage + editAndCopyMessage + diffContents, editAndCopyMessage);
            }
        }


        private static string FindFirstDifference(string baseline, string actual)
        {
            int index = 0;
            int minEnd = Math.Min(baseline.Length, actual.Length);
            while (index < minEnd && baseline[index] == actual[index]) 
                index++;

            int firstDiffIndex = (index == minEnd && baseline.Length == actual.Length) ? -1 : index;

            int startRange = Math.Max(firstDiffIndex - 50, 0);
            int endRange = Math.Min(firstDiffIndex + 50, minEnd);

            string baselineDiff = ShowWhitespace(baseline.Substring(startRange, endRange));
            string actualDiff = ShowWhitespace(actual.Substring(startRange, endRange));
            return "\r\nFirst Diff:\r\n===== Baseline =====\r\n" 
                + baselineDiff
                + "\r\n===== Actual =====\r\n"
                + actualDiff;
        }

        private static string ShowWhitespace(string input)
        {
            return input.Replace("\r", "\\r").Replace("\n", "\\n");
        }

        /// <summary>
        /// Normalizes line endings in a file to facilitate comparisons regardless of OS. On Windows line endings are \r\n, while
        /// on other systems only \n is used
        /// </summary>
        public static string ReadTextAndNormalizeLineEndings(string filePath)
        {
            string text = File.ReadAllText(filePath);
            return NormalizeLineEndings(text);
        }

        public static string NormalizeLineEndings(string text)
        {
            return text.Replace("\r\n", Environment.NewLine);
        }
    }
}
