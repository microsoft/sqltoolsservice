﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
using Microsoft.SqlTools.ServiceLayer.Formatter.Contracts;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Utility;
using Range = Microsoft.SqlTools.ServiceLayer.Workspace.Contracts.Range;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{

    [Export(typeof(IHostedService))]
    public class TSqlFormatterService : HostedService<TSqlFormatterService>, IComposableService
    {
        private FormatterSettings settings;
        /// <summary>
        /// The default constructor is required for MEF-based composable services
        /// </summary>
        public TSqlFormatterService()
        {
            settings = new FormatterSettings();
        }



        public override void InitializeService(IProtocolEndpoint serviceHost)
        {
            Logger.Write(TraceEventType.Verbose, "TSqlFormatter initialized");
            serviceHost.SetRequestHandler(DocumentFormattingRequest.Type, HandleDocFormatRequest);
            serviceHost.SetRequestHandler(DocumentRangeFormattingRequest.Type, HandleDocRangeFormatRequest);
            WorkspaceService?.RegisterConfigChangeCallback(HandleDidChangeConfigurationNotification);
        }

        /// <summary>
        /// Gets the workspace service. Note: should handle case where this is null in cases where unit tests do not set this up
        /// </summary>
        private WorkspaceService<SqlToolsSettings> WorkspaceService
        {
            get { return ServiceProvider.GetService<WorkspaceService<SqlToolsSettings>>(); }
        }

        /// <summary>
        /// Gets the language service. Note: should handle case where this is null in cases where unit tests do not set this up
        /// </summary>
        private LanguageService LanguageService
        {
            get { return ServiceProvider.GetService<LanguageService>(); }
        }

        /// <summary>
        /// Ensure formatter settings are always up to date
        /// </summary>
        public Task HandleDidChangeConfigurationNotification(
            SqlToolsSettings newSettings,
            SqlToolsSettings oldSettings,
            EventContext eventContext)
        {
            // update the current settings to reflect any changes (assuming formatter settings exist)
            settings = newSettings?.SqlTools?.Format ?? settings;
            return Task.FromResult(true);
        }

        public async Task HandleDocFormatRequest(DocumentFormattingParams docFormatParams, RequestContext<TextEdit[]> requestContext)
        {
            Logger.Verbose("HandleDocFormatRequest");
            TextEdit[] result = await FormatAndReturnEdits(docFormatParams);
            await requestContext.SendResult(result);
            DocumentStatusHelper.SendTelemetryEvent(requestContext, CreateTelemetryProps(isDocFormat: true));
        }

        public async Task HandleDocRangeFormatRequest(DocumentRangeFormattingParams docRangeFormatParams, RequestContext<TextEdit[]> requestContext)
        {
            Logger.Verbose("HandleDocRangeFormatRequest");
            TextEdit[] result = await FormatRangeAndReturnEdits(docRangeFormatParams);
            await requestContext.SendResult(result);
            DocumentStatusHelper.SendTelemetryEvent(requestContext, CreateTelemetryProps(isDocFormat: false));
        }
        private static TelemetryProperties CreateTelemetryProps(bool isDocFormat)
        {
            return new TelemetryProperties
            {
                Properties = new Dictionary<string, string>
                {
                    { TelemetryPropertyNames.FormatType,
                        isDocFormat ? TelemetryPropertyNames.DocumentFormatType : TelemetryPropertyNames.RangeFormatType }
                },
                EventName = TelemetryEventNames.FormatCode
            };
        }

        private Task<TextEdit[]> FormatRangeAndReturnEdits(DocumentRangeFormattingParams docFormatParams)
        {
            return Task.Run(() =>
            {
                if (ShouldSkipFormatting(docFormatParams))
                {
                    return Array.Empty<TextEdit>();
                }

                var range = docFormatParams.Range;
                ScriptFile scriptFile = GetFile(docFormatParams);
                if (scriptFile == null)
                {
                    return Array.Empty<TextEdit>();
                }
                TextEdit textEdit = new TextEdit { Range = range };
                string text = scriptFile.GetTextInRange(range.ToBufferRange());
                return DoFormat(docFormatParams, textEdit, text);
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
            return (LanguageService != null && LanguageService.ShouldSkipNonMssqlFile(docFormatParams.TextDocument.Uri));
        }

        private Task<TextEdit[]> FormatAndReturnEdits(DocumentFormattingParams docFormatParams)
        {
            return Task.Factory.StartNew(() =>
            {
                if (ShouldSkipFormatting(docFormatParams))
                {
                    return Array.Empty<TextEdit>();
                }

                var scriptFile = GetFile(docFormatParams);
                if (scriptFile == null
                    || scriptFile.FileLines.Count == 0)
                {
                    return Array.Empty<TextEdit>();
                }
                TextEdit textEdit = PrepareEdit(scriptFile);
                string text = scriptFile.Contents;
                return DoFormat(docFormatParams, textEdit, text);
            });
        }

        private TextEdit[] DoFormat(DocumentFormattingParams docFormatParams, TextEdit edit, string text)
        {
            Validate.IsNotNull(nameof(docFormatParams), docFormatParams);

            FormatOptions options = GetOptions(docFormatParams);
            List<TextEdit> edits = new List<TextEdit>();
            edit.NewText = Format(text, options, false);
            // TODO do not add if no formatting needed?
            edits.Add(edit);
            return edits.ToArray();
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
            return WorkspaceService.Workspace.GetFile(docFormatParams.TextDocument.Uri);
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
