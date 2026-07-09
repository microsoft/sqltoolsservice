//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Hosting;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.LanguageService.Formatter.Contracts;
using Microsoft.SqlTools.LanguageService.Formatter.ScriptDom;
using Microsoft.SqlTools.LanguageService.LanguageServices;
using Microsoft.SqlTools.LanguageService.LanguageServices.Contracts;
using Microsoft.SqlTools.LanguageService.Workspace.Contracts;
using Microsoft.SqlTools.Utility;
using Range = Microsoft.SqlTools.LanguageService.Workspace.Contracts.Range;

namespace Microsoft.SqlTools.LanguageService.Formatter
{

    [Export(typeof(IHostedService))]
    public class TSqlFormatterService : HostedService<TSqlFormatterService>, IComposableService
    {
        private FormatterSettings settings;
        private Func<string, ScriptFile> fileResolver;
        private readonly ScriptDomSqlFormatter scriptDomFormatter;

        /// <summary>
        /// The default constructor is required for MEF-based composable services
        /// </summary>
        public TSqlFormatterService()
        {
            settings = new FormatterSettings();
            scriptDomFormatter = new ScriptDomSqlFormatter();
        }

        public override void InitializeService(IProtocolEndpoint serviceHost)
        {
            Logger.Verbose("TSqlFormatter initialized");
            serviceHost.SetRequestHandler(DocumentFormattingRequest.Type, HandleDocFormatRequest, true);
            serviceHost.SetRequestHandler(DocumentRangeFormattingRequest.Type, HandleDocRangeFormatRequest, true);
        }

        /// <summary>
        /// Gets the file filter used to determine whether a file should be skipped. Note: may be null
        /// in cases where unit tests do not set this up.
        /// </summary>
        private ILanguageFileFilter FileFilter
        {
            get { return ServiceProvider?.GetService<ILanguageFileFilter>(); }
        }

        /// <summary>
        /// Sets the resolver used to look up a <see cref="ScriptFile"/> by URI. Wired by the host so the
        /// formatter does not need to reference the concrete workspace settings type.
        /// </summary>
        public void SetFileResolver(Func<string, ScriptFile> resolver)
        {
            fileResolver = resolver;
        }

        /// <summary>
        /// Ensure formatter settings are always up to date. Called by the host on configuration changes.
        /// </summary>
        public void UpdateFormatterSettings(FormatterSettings newSettings)
        {
            // update the current settings to reflect any changes (assuming formatter settings exist)
            settings = newSettings ?? settings;
        }

        public async Task HandleDocFormatRequest(DocumentFormattingParams docFormatParams, RequestContext<TextEdit[]> requestContext)
        {
            Logger.Verbose("HandleDocFormatRequest");
            FormatOperationResult result = await FormatAndReturnEdits(docFormatParams);
            await requestContext.SendResult(result.Edits);
            DocumentStatusHelper.SendTelemetryEvent(requestContext, CreateTelemetryProps(isDocFormat: true, result));
        }

        public async Task HandleDocRangeFormatRequest(DocumentRangeFormattingParams docRangeFormatParams, RequestContext<TextEdit[]> requestContext)
        {
            Logger.Verbose("HandleDocRangeFormatRequest");
            FormatOperationResult result = await FormatRangeAndReturnEdits(docRangeFormatParams);
            await requestContext.SendResult(result.Edits);
            DocumentStatusHelper.SendTelemetryEvent(requestContext, CreateTelemetryProps(isDocFormat: false, result));
        }

        private static TelemetryProperties CreateTelemetryProps(bool isDocFormat, FormatOperationResult result)
        {
            return new TelemetryProperties
            {
                Properties = new Dictionary<string, string>
                {
                    { TelemetryPropertyNames.FormatType,
                        isDocFormat ? TelemetryPropertyNames.DocumentFormatType : TelemetryPropertyNames.RangeFormatType },
                    { TelemetryPropertyNames.FormatterImplementation, result.FormatterImplementation },
                    { TelemetryPropertyNames.FormatterOutcome, result.FormatterOutcome }
                },
                Measures = new Dictionary<string, double>
                {
                    { TelemetryMeasureNames.FormatterDurationMs, result.DurationMs },
                    { TelemetryMeasureNames.ParseErrorCount, result.ParseErrorCount }
                },
                EventName = TelemetryEventNames.FormatCode
            };
        }

        private Task<FormatOperationResult> FormatRangeAndReturnEdits(DocumentRangeFormattingParams docFormatParams)
        {
            return Task.Run(() =>
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                if (ShouldSkipFormatting(docFormatParams))
                {
                    return CreateSkippedResult(stopwatch);
                }

                var range = docFormatParams.Range;
                ScriptFile scriptFile = GetFile(docFormatParams);
                if (scriptFile == null)
                {
                    return CreateSkippedResult(stopwatch);
                }
                TextEdit textEdit = new TextEdit { Range = range };
                string text = scriptFile.GetTextInRange(range.ToBufferRange());
                return DoFormat(docFormatParams, textEdit, text, stopwatch);
            });
        }

        private bool ShouldSkipFormatting(DocumentFormattingParams docFormatParams)
        {
            if (docFormatParams == null
                || docFormatParams.TextDocument == null
                || docFormatParams.TextDocument.Uri == null)
            {
                return true;
            }
            ILanguageFileFilter fileFilter = FileFilter;
            return (fileFilter != null && fileFilter.ShouldSkipNonMssqlFile(docFormatParams.TextDocument.Uri));
        }

        private Task<FormatOperationResult> FormatAndReturnEdits(DocumentFormattingParams docFormatParams)
        {
            return Task.Factory.StartNew(() =>
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                if (ShouldSkipFormatting(docFormatParams))
                {
                    return CreateSkippedResult(stopwatch);
                }

                var scriptFile = GetFile(docFormatParams);
                if (scriptFile == null
                    || scriptFile.FileLines.Count == 0)
                {
                    return CreateSkippedResult(stopwatch);
                }
                TextEdit textEdit = PrepareEdit(scriptFile);
                string text = scriptFile.Contents;
                return DoFormat(docFormatParams, textEdit, text, stopwatch);
            });
        }

        private FormatOperationResult DoFormat(DocumentFormattingParams docFormatParams, TextEdit edit, string text, Stopwatch stopwatch)
        {
            Validate.IsNotNull(nameof(docFormatParams), docFormatParams);

            FormatOptions options = GetOptions(docFormatParams);
            if (UsePreviewFormatter)
            {
                return FormatWithScriptDom(edit, text, options, stopwatch);
            }

            edit.NewText = Format(text, options, false);
            return CreateFormatResult(
                new[] { edit },
                TelemetryPropertyNames.LegacyFormatterImplementation,
                TelemetryPropertyNames.FormatterOutcomeApplied,
                stopwatch);
        }

        private FormatOperationResult FormatWithScriptDom(TextEdit edit, string text, FormatOptions options, Stopwatch stopwatch)
        {
            ScriptDomFormatterResult result = scriptDomFormatter.Format(text, options);
            if (result.Outcome != ScriptDomFormatterOutcome.Formatted)
            {
                return CreateFormatResult(
                    Array.Empty<TextEdit>(),
                    TelemetryPropertyNames.ScriptDomFormatterImplementation,
                    ToTelemetryOutcome(result.Outcome),
                    stopwatch,
                    result.ParseErrorCount);
            }

            edit.NewText = result.FormattedText;
            return CreateFormatResult(
                new[] { edit },
                TelemetryPropertyNames.ScriptDomFormatterImplementation,
                TelemetryPropertyNames.FormatterOutcomeApplied,
                stopwatch);
        }

        private FormatOperationResult CreateSkippedResult(Stopwatch stopwatch)
        {
            return CreateFormatResult(
                Array.Empty<TextEdit>(),
                UsePreviewFormatter
                    ? TelemetryPropertyNames.ScriptDomFormatterImplementation
                    : TelemetryPropertyNames.LegacyFormatterImplementation,
                TelemetryPropertyNames.FormatterOutcomeSkipped,
                stopwatch);
        }

        private static FormatOperationResult CreateFormatResult(
            TextEdit[] edits,
            string formatterImplementation,
            string formatterOutcome,
            Stopwatch stopwatch,
            int parseErrorCount = 0)
        {
            stopwatch.Stop();
            return new FormatOperationResult(
                edits,
                formatterImplementation,
                formatterOutcome,
                stopwatch.ElapsedMilliseconds,
                parseErrorCount);
        }

        private static string ToTelemetryOutcome(ScriptDomFormatterOutcome outcome)
        {
            switch (outcome)
            {
                case ScriptDomFormatterOutcome.NoChange:
                    return TelemetryPropertyNames.FormatterOutcomeNoChange;
                case ScriptDomFormatterOutcome.EmptyDocument:
                    return TelemetryPropertyNames.FormatterOutcomeEmptyText;
                case ScriptDomFormatterOutcome.ParseError:
                    return TelemetryPropertyNames.FormatterOutcomeParseFailed;
                case ScriptDomFormatterOutcome.Exception:
                    return TelemetryPropertyNames.FormatterOutcomeException;
                default:
                    return TelemetryPropertyNames.FormatterOutcomeApplied;
            }
        }

        private bool UsePreviewFormatter
        {
            get { return settings?.EnablePreviewFormatter == true; }
        }

        private FormatOptions GetOptions(DocumentFormattingParams docFormatParams)
        {
            return MergeFormatOptions(docFormatParams.Options, settings);
        }

        internal static FormatOptions MergeFormatOptions(FormattingOptions formatRequestOptions, FormatterSettings settings)

        {
            FormatOptions options = new FormatOptions();
            if (formatRequestOptions != null)
            {
                options.UseSpaces = formatRequestOptions.InsertSpaces;
                options.SpacesPerIndent = formatRequestOptions.TabSize;
            }
            UpdateFormatOptionsFromSettings(options, settings);
            return options;
        }

        internal static void UpdateFormatOptionsFromSettings(FormatOptions options, FormatterSettings settings)
        {
            Validate.IsNotNull(nameof(options), options);
            if (settings != null)
            {
                if (settings.AlignColumnDefinitionsInColumns.HasValue) { options.AlignColumnDefinitionsInColumns = settings.AlignColumnDefinitionsInColumns.Value; }

                if (settings.PlaceCommasBeforeNextStatement.HasValue) { options.PlaceCommasBeforeNextStatement = settings.PlaceCommasBeforeNextStatement.Value; }

                if (settings.PlaceSelectStatementReferencesOnNewLine.HasValue) { options.PlaceEachReferenceOnNewLineInQueryStatements = settings.PlaceSelectStatementReferencesOnNewLine.Value; }

                if (settings.UseBracketForIdentifiers.HasValue) { options.EncloseIdentifiersInSquareBrackets = settings.UseBracketForIdentifiers.Value; }

                options.DatatypeCasing = settings.DatatypeCasing;
                options.KeywordCasing = settings.KeywordCasing;
            }
        }

        private ScriptFile GetFile(DocumentFormattingParams docFormatParams)
        {
            return fileResolver?.Invoke(docFormatParams.TextDocument.Uri);
        }

        private sealed class FormatOperationResult
        {
            public FormatOperationResult(
                TextEdit[] edits,
                string formatterImplementation,
                string formatterOutcome,
                double durationMs,
                int parseErrorCount)
            {
                Edits = edits;
                FormatterImplementation = formatterImplementation;
                FormatterOutcome = formatterOutcome;
                DurationMs = durationMs;
                ParseErrorCount = parseErrorCount;
            }

            public TextEdit[] Edits { get; }

            public string FormatterImplementation { get; }

            public string FormatterOutcome { get; }

            public double DurationMs { get; }

            public int ParseErrorCount { get; }
        }

        private static TextEdit PrepareEdit(ScriptFile scriptFile)
        {
            int fileLines = scriptFile.FileLines.Count;
            Position start = new Position { Line = 0, Character = 0 };
            int lastChar = scriptFile.FileLines[scriptFile.FileLines.Count - 1].Length;
            Position end = new Position { Line = scriptFile.FileLines.Count - 1, Character = lastChar };

            TextEdit edit = new TextEdit
            {
                Range = new Range { Start = start, End = end }
            };
            return edit;
        }

        public string Format(TextReader input)
        {
            string originalSql = input.ReadToEnd();
            return Format(originalSql, new FormatOptions());
        }

        public string Format(string input, FormatOptions options)
        {
            return Format(input, options, true);
        }

        public string Format(string input, FormatOptions options, bool verifyOutput)
        {
            string result = null;
            DoFormat(input, options, verifyOutput, visitor =>
            {
                result = visitor.Context.FormattedSql;
            });

            return result;
        }

        public void Format(string input, FormatOptions options, bool verifyOutput, Replacement.OnReplace replace)
        {
            DoFormat(input, options, verifyOutput, visitor =>
            {
                foreach (Replacement r in visitor.Context.Replacements)
                {
                    r.Apply(replace);
                }
            });
        }

        private void DoFormat(string input, FormatOptions options, bool verifyOutput, Action<FormatterVisitor> postFormatAction)
        {
            Validate.IsNotNull(nameof(input), input);
            Validate.IsNotNull(nameof(options), options);

            ParseResult result = Parser.Parse(input);
            FormatContext context = new FormatContext(result.Script, options);

            FormatterVisitor visitor = new FormatterVisitor(context, ServiceProvider);
            result.Script.Accept(visitor);
            if (verifyOutput)
            {
                visitor.VerifyFormat();
            }

            postFormatAction?.Invoke(visitor);
        }
    }

    internal static class RangeExtensions
    {
        public static BufferRange ToBufferRange(this Range range)
        {
            // It turns out that VSCode sends Range objects as 0-indexed lines, while
            // our BufferPosition and BufferRange logic assumes 1-indexed. Therefore
            // need to increment all ranges by 1 when copying internally and reduce
            // when returning to the caller
            return new BufferRange(
                new BufferPosition(range.Start.Line + 1, range.Start.Character + 1),
                new BufferPosition(range.End.Line + 1, range.End.Character + 1)
            );
        }
    }
}
