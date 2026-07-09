//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.LanguageService.Formatter.ScriptDom
{
    internal sealed class ScriptDomSqlFormatter
    {
        public ScriptDomFormatterResult Format(string text, FormatOptions formatOptions)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new ScriptDomFormatterResult(ScriptDomFormatterOutcome.EmptyDocument);
            }

            try
            {
                ScriptDomFormatterSettings settings = ScriptDomFormatterSettings.FromFormatOptions(formatOptions);
                TSqlParser parser = CreateParser(settings.SqlVersion, settings.SqlEngineType);
                TSqlFragment fragment;
                IList<ParseError> errors;

                using (StringReader reader = new StringReader(text))
                {
                    fragment = parser.Parse(reader, out errors);
                }

                if (errors.Any())
                {
                    return new ScriptDomFormatterResult(ScriptDomFormatterOutcome.ParseError, parseErrorCount: errors.Count);
                }

                SqlScriptGeneratorOptions generatorOptions = ScriptDomFormatterOptionsMapper.ToScriptGeneratorOptions(settings);
                SqlScriptGenerator generator = CreateScriptGenerator(generatorOptions);
                generator.GenerateScript(fragment, out string generatedText);

                string lineEnding = GetDominantLineEnding(text);
                string formattedText = NormalizeLineEndings(generatedText.Trim(), lineEnding);
                string normalizedInput = NormalizeLineEndings(text.Trim(), lineEnding);
                if (normalizedInput == formattedText)
                {
                    return new ScriptDomFormatterResult(ScriptDomFormatterOutcome.NoChange);
                }

                return new ScriptDomFormatterResult(ScriptDomFormatterOutcome.Formatted, formattedText);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new ScriptDomFormatterResult(ScriptDomFormatterOutcome.Exception);
            }
        }

        private static TSqlParser CreateParser(SqlVersion sqlVersion, SqlEngineType engineType)
        {
            switch (sqlVersion)
            {
                case SqlVersion.Sql80:
                    return new TSql80Parser(initialQuotedIdentifiers: true);
                case SqlVersion.Sql90:
                    return new TSql90Parser(initialQuotedIdentifiers: true);
                case SqlVersion.Sql100:
                    return new TSql100Parser(initialQuotedIdentifiers: true);
                case SqlVersion.Sql110:
                    return new TSql110Parser(initialQuotedIdentifiers: true);
                case SqlVersion.Sql120:
                    return new TSql120Parser(initialQuotedIdentifiers: true);
                case SqlVersion.Sql130:
                    return new TSql130Parser(initialQuotedIdentifiers: true);
                case SqlVersion.Sql140:
                    return new TSql140Parser(initialQuotedIdentifiers: true);
                case SqlVersion.Sql150:
                    return new TSql150Parser(initialQuotedIdentifiers: true, engineType);
                case SqlVersion.Sql160:
                    return new TSql160Parser(initialQuotedIdentifiers: true, engineType);
                case SqlVersion.Sql170:
                default:
                    return new TSql170Parser(initialQuotedIdentifiers: true, engineType);
            }
        }

        private static SqlScriptGenerator CreateScriptGenerator(SqlScriptGeneratorOptions options)
        {
            switch (options.SqlVersion)
            {
                case SqlVersion.Sql80:
                    return new Sql80ScriptGenerator(options);
                case SqlVersion.Sql90:
                    return new Sql90ScriptGenerator(options);
                case SqlVersion.Sql100:
                    return new Sql100ScriptGenerator(options);
                case SqlVersion.Sql110:
                    return new Sql110ScriptGenerator(options);
                case SqlVersion.Sql120:
                    return new Sql120ScriptGenerator(options);
                case SqlVersion.Sql130:
                    return new Sql130ScriptGenerator(options);
                case SqlVersion.Sql140:
                    return new Sql140ScriptGenerator(options);
                case SqlVersion.Sql150:
                    return new Sql150ScriptGenerator(options);
                case SqlVersion.Sql160:
                    return new Sql160ScriptGenerator(options);
                case SqlVersion.Sql170:
                default:
                    return new Sql170ScriptGenerator(options);
            }
        }

        private static string GetDominantLineEnding(string text)
        {
            int crlfCount = 0;
            int lfCount = 0;
            for (int index = 0; index < text.Length; index++)
            {
                if (text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                {
                    crlfCount++;
                    index++;
                }
                else if (text[index] == '\n')
                {
                    lfCount++;
                }
            }

            if (crlfCount == 0 && lfCount == 0)
            {
                return Environment.NewLine;
            }

            return crlfCount >= lfCount ? "\r\n" : "\n";
        }

        private static string NormalizeLineEndings(string text, string lineEnding)
        {
            return text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", lineEnding);
        }
    }
}
