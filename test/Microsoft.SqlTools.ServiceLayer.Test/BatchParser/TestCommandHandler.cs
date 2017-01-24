//------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using System.Globalization;
using Microsoft.SqlTools.ServiceLayer.BatchParser;

namespace Microsoft.SqlTools.ServiceLayer.Test.BatchParser
{
    internal class TestCommandHandler : ICommandHandler
    {
        private Parser _parser;
        private StringBuilder _outputString;

        public TestCommandHandler(StringBuilder outputString)
        {
            _outputString = outputString;
        }

        public void SetParser(Parser parser)
        {
            _parser = parser;
        }

        public BatchParserAction Go(TextBlock batch, int repeatCount)
        {
            string textWithVariablesResolved;
            string textWithVariablesUnresolved;
            LineInfo lineInfoVarsResolved;
            LineInfo lineInfoVarsUnresolved;

            batch.GetText(true, out textWithVariablesResolved, out lineInfoVarsResolved);
            batch.GetText(false, out textWithVariablesUnresolved, out lineInfoVarsUnresolved);
            _outputString.AppendFormat(CultureInfo.InvariantCulture, "*** Execute batch ({0})\n", repeatCount);
            
            if (string.Compare(textWithVariablesUnresolved, textWithVariablesResolved, StringComparison.Ordinal) != 0)
            {
                _outputString.AppendLine("Text with variables not resolved:");
                _outputString.AppendLine(textWithVariablesResolved);
                _outputString.AppendLine("Text with variables not resolved:");
                _outputString.AppendLine(textWithVariablesUnresolved);
                int i = 0;
                _outputString.AppendLine("Mapping from resolved string to unresolved:");
                while (i <= textWithVariablesResolved.Length)
                {
                    PositionStruct pos = lineInfoVarsResolved.GetStreamPositionForOffset(i);
                    string character = i < textWithVariablesResolved.Length ? ("" + textWithVariablesResolved[i]).Replace("\n", @"\n").Replace("\r", @"\r") : "EOF";
                    _outputString.AppendFormat(CultureInfo.InvariantCulture, "Pos [{0}] {1}:{2} \"{3}\"", 
                        i, 
                        BatchParserTests.GetFilenameOnly(pos.Filename), 
                        pos.Offset, 
                        character);
                    _outputString.AppendLine();
                    i++;
                }
            }
            else
            {
                _outputString.AppendLine("Batch text:");
                _outputString.AppendLine(textWithVariablesUnresolved);
            }
            _outputString.AppendLine();
            return BatchParserAction.Continue;
        }

        public BatchParserAction OnError(Token token, OnErrorAction action)
        {
            _outputString.AppendFormat(CultureInfo.InvariantCulture, "*** PARSER: On error: {0}", action.ToString());
            _outputString.AppendLine();
            return BatchParserAction.Continue;
        }

        public BatchParserAction Include(TextBlock filename, out TextReader stream, out string newFilename)
        {
            string resolvedFilename;
            LineInfo lineInfo;

            filename.GetText(true, out resolvedFilename, out lineInfo);
            _outputString.AppendFormat(CultureInfo.InvariantCulture, "*** PARSER: Include file \"{0}\"\n", resolvedFilename);
            _outputString.AppendLine();
            string currentFilename = lineInfo.GetStreamPositionForOffset(0).Filename;
            string currentFilePath = Path.Combine(Path.GetDirectoryName(currentFilename), resolvedFilename);
            stream = new StreamReader(File.Open(currentFilePath, FileMode.Open));
            newFilename = resolvedFilename;
            return BatchParserAction.Continue;
        }
    }
}
