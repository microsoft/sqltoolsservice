//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.SqlTools.ServiceLayer.BatchParser;

namespace Microsoft.SqlTools.ManagedBatchParser.UnitTests.BatchParser
{
    internal class TestCommandHandler : ICommandHandler
    {
        private Parser parser;
        private StringBuilder outputString;

        public TestCommandHandler(StringBuilder outputString)
        {
            this.outputString = outputString;
        }

        public void SetParser(Parser parser)
        {
            this.parser = parser;
        }

        public BatchParserAction Go(TextBlock batch, int repeatCount)
        {
            string textWithVariablesResolved;
            string textWithVariablesUnresolved;
            LineInfo lineInfoVarsResolved;
            LineInfo lineInfoVarsUnresolved;

            batch.GetText(true, out textWithVariablesResolved, out lineInfoVarsResolved);
            batch.GetText(false, out textWithVariablesUnresolved, out lineInfoVarsUnresolved);
            outputString.AppendFormat(CultureInfo.InvariantCulture, "*** Execute batch ({0})\n", repeatCount);

            if (string.Compare(textWithVariablesUnresolved, textWithVariablesResolved, StringComparison.Ordinal) != 0)
            {
                outputString.AppendLine("Text with variables not resolved:");
                outputString.AppendLine(textWithVariablesResolved);
                outputString.AppendLine("Text with variables not resolved:");
                outputString.AppendLine(textWithVariablesUnresolved);
                int i = 0;
                outputString.AppendLine("Mapping from resolved string to unresolved:");
                while (i <= textWithVariablesResolved.Length)
                {
                    PositionStruct pos = lineInfoVarsResolved.GetStreamPositionForOffset(i);
                    string character = i < textWithVariablesResolved.Length ? ("" + textWithVariablesResolved[i]).Replace("\n", @"\n").Replace("\r", @"\r") : "EOF";
                    outputString.AppendFormat(CultureInfo.InvariantCulture, "Pos [{0}] {1}:{2} \"{3}\"",
                        i,
                        BatchParserTests.GetFilenameOnly(pos.Filename),
                        pos.Offset,
                        character);
                    outputString.AppendLine();
                    i++;
                }
            }
            else
            {
                outputString.AppendLine("Batch text:");
                outputString.AppendLine(textWithVariablesUnresolved);
            }
            outputString.AppendLine();
            return BatchParserAction.Continue;
        }

        public BatchParserAction OnError(Token token, OnErrorAction action)
        {
            outputString.AppendFormat(CultureInfo.InvariantCulture, "*** PARSER: On error: {0}", action.ToString());
            outputString.AppendLine();
            return BatchParserAction.Continue;
        }

        public BatchParserAction Include(TextBlock filename, out TextReader stream, out string newFilename)
        {
            string resolvedFilename;
            LineInfo lineInfo;

            filename.GetText(true, out resolvedFilename, out lineInfo);
            outputString.AppendFormat(CultureInfo.InvariantCulture, "*** PARSER: Include file \"{0}\"\n", resolvedFilename);
            outputString.AppendLine();
            string currentFilename = lineInfo.GetStreamPositionForOffset(0).Filename;
            string currentFilePath = Path.Combine(Path.GetDirectoryName(currentFilename), resolvedFilename);
            stream = new StreamReader(File.Open(currentFilePath, FileMode.Open));
            newFilename = resolvedFilename;
            return BatchParserAction.Continue;
        }
    }
}