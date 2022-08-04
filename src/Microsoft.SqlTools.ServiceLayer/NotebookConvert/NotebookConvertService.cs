//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.NotebookConvert.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Newtonsoft.Json;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.SqlTools.ServiceLayer.NotebookConvert
{
    enum NotebookTokenType
    {
        MultilineComment,
        SinglelineComment,
        Batch
    }

    class NotebookToken
    {
        public int StartOffset;
        public string Text;
        public NotebookTokenType TokenType;
    }

    /// <summary>
    /// Main class for Notebook Convert Service
    /// </summary>
    public class NotebookConvertService
    {
        private static readonly Lazy<NotebookConvertService> instance = new Lazy<NotebookConvertService>(() => new NotebookConvertService());

        public NotebookConvertService()
        {
        }

        public static NotebookConvertService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Service host object for sending/receiving requests/events.
        /// </summary>
        internal IProtocolEndpoint ServiceHost
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes the service by doing tasks such as setting up request handlers.
        /// </summary>
        /// <param name="serviceHost"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            this.ServiceHost = serviceHost;

            this.ServiceHost.SetRequestHandler(ConvertNotebookToSqlRequest.Type, HandleConvertNotebookToSqlRequest);
            this.ServiceHost.SetRequestHandler(ConvertSqlToNotebookRequest.Type, HandleConvertSqlToNotebookRequest);


        }

        #region Convert Handlers

        internal async Task HandleConvertNotebookToSqlRequest(ConvertNotebookToSqlParams parameters, RequestContext<ConvertNotebookToSqlResult> requestContext)
        {
            var notebookDoc = JsonConvert.DeserializeObject<NotebookDocument>(parameters.Content);

            var result = new ConvertNotebookToSqlResult
            {
                Content = ConvertNotebookDocToSql(notebookDoc)
            };
            await requestContext.SendResult(result);
        }

        internal async Task HandleConvertSqlToNotebookRequest(ConvertSqlToNotebookParams parameters, RequestContext<ConvertSqlToNotebookResult> requestContext)
        {
            // This URI doesn't come in escaped - so if it's a file path with reserved characters (such as %)
            // then we'll fail to find it since GetFile expects the URI to be a fully-escaped URI as that's
            // what the document events are sent in as.
            var escapedClientUri = Uri.EscapeUriString(parameters.ClientUri);
            var file = WorkspaceService<SqlToolsSettings>.Instance.Workspace.GetFile(escapedClientUri);
            // Temporary notebook that we just fill in with the sql until the parsing logic is added
            var result = new ConvertSqlToNotebookResult
            {
                Content = JsonConvert.SerializeObject(ConvertSqlToNotebook(file.Contents))
            };
            await requestContext.SendResult(result);
        }

        #endregion // Convert Handlers

        internal static NotebookDocument ConvertSqlToNotebook(string? sql)
        {
            // Notebooks use \n so convert any other newlines now
            sql = sql?.Replace("\r\n", "\n") ?? string.Empty;

            var doc = new NotebookDocument
            {
                NotebookMetadata = new NotebookMetadata()
                {
                    KernelSpec = new NotebookKernelSpec()
                    {
                        Name = "SQL",
                        DisplayName = "SQL",
                        Language = "sql"
                    },
                    LanguageInfo = new NotebookLanguageInfo()
                    {
                        Name = "sql",
                        Version = ""
                    }
                }
            };
            var parser = new TSql150Parser(false);
            IList<ParseError> errors = new List<ParseError>();
            var parseResult = parser.Parse(new StringReader(sql), out errors);
            if (errors.Count > 0)
            {
                throw new Exception(string.Join(Environment.NewLine, errors.Select(error => error.Message)));
            }
            var batches = (parseResult as TSqlScript).Batches;
            var tokens = parseResult.ScriptTokenStream;

            /**
             * Split the text into separate chunks - blocks of Mutliline comments and blocks
             * of everything else (batches). We then create a markdown cell for each multiline comment and a code
             * cell for the other blocks.
             * We only take multiline comments which aren't part of a batch - since otherwise they would
             * break up the T-SQL in separate code cells and since we currently don't share state between
             * cells that could break the script
             */
            var multilineComments = tokens
                .Where(token => token.TokenType == TSqlTokenType.MultilineComment || token.TokenType == TSqlTokenType.SingleLineComment)
                // Ignore comments that are within a batch. This won't include comments at the start/end of a batch though  - the parser is smart enough
                // to have the batch only contain the code and any comments that are embedded within it
                .Where(token => !batches.Any(batch => token.Offset > batch.StartOffset && token.Offset < (batch.StartOffset + batch.FragmentLength)))
                .Select(token => new NotebookToken() { StartOffset = token.Offset, Text = token.Text.Trim(), TokenType = token.TokenType == TSqlTokenType.MultilineComment ? NotebookTokenType.MultilineComment : NotebookTokenType.SinglelineComment });

            // Combine batches and comments into a single list of all the fragments we need to add to the Notebook
            var allFragments = batches.Select(batch =>
                {
                    string text = sql.Substring(batch.StartOffset, batch.FragmentLength).Trim();
                    return new NotebookToken() { StartOffset = batch.StartOffset, Text = text, TokenType = NotebookTokenType.Batch };
                })
                .Concat(multilineComments)
                .OrderBy(token => token.StartOffset);

            foreach (var fragment in allFragments)
            {
                if (fragment.TokenType == NotebookTokenType.Batch)
                {
                    // Batches are just simple code cells so no additional logic needed
                    doc.Cells.Add(GenerateCodeCell(fragment.Text));
                }
                else if (fragment.TokenType == NotebookTokenType.MultilineComment)
                {
                    string commentBlock = fragment.Text;
                    // Trim off the starting comment tokens (/** or /*)
                    if (commentBlock.StartsWith("/**"))
                    {
                        commentBlock = commentBlock.Remove(0, 3);
                    }
                    else if (commentBlock.StartsWith("/*"))
                    {
                        commentBlock = commentBlock.Remove(0, 2);
                    }
                    // Trim off the ending comment tokens (**/ or */)
                    if (commentBlock.EndsWith("**/"))
                    {
                        commentBlock = commentBlock.Remove(commentBlock.Length - 3);
                    }
                    else if (commentBlock.EndsWith("*/"))
                    {
                        commentBlock = commentBlock.Remove(commentBlock.Length - 2);
                    }

                    // Trim off extra spaces for each line. This helps keep comment asterisks aligned on the 
                    // same column for multiline comments.
                    var commentLines = commentBlock.Trim().Split("\n").Select(comment => comment.Trim());
                    commentBlock = string.Join("\n", commentLines);
                    doc.Cells.Add(GenerateMarkdownCell(commentBlock));
                }
                else if (fragment.TokenType == NotebookTokenType.SinglelineComment)
                {
                    string commentBlock = fragment.Text;
                    // Trim off the starting comment token (--)
                    if (commentBlock.StartsWith("--"))
                    {
                        commentBlock = commentBlock.Remove(0, 2);
                    }

                    doc.Cells.Add(GenerateMarkdownCell(commentBlock.Trim()));
                }
            }
            return doc;
        }

        private static NotebookCell GenerateCodeCell(string contents)
        {
            // Each line is a separate entry in the contents array so split that now, but
            // Notebooks still expect each line to end with a newline so keep that
            var contentsArray = contents
                    .Split('\n')
                    .Select(line => $"{line}\n")
                .ToList();
            // Last line shouldn't have a newline
            contentsArray[^1] = contentsArray[^1].TrimEnd();
            return new NotebookCell("code", contentsArray);
        }

        private static NotebookCell GenerateMarkdownCell(string contents)
        {
            // Each line is a separate entry in the contents array so split that now, but
            // Notebooks still expect each line to end with a newline so keep that.
            // In addition - markdown newlines have to be prefixed by 2 spaces
            var contentsArray = contents
                    .Split('\n')
                    .Select(line => $"{line}  \n")
                .ToList();
            // Last line shouldn't have a newline
            contentsArray[^1] = contentsArray[^1].TrimEnd();
            return new NotebookCell("markdown", contentsArray);
        }

        /// <summary>
        /// Converts a Notebook document into a single string that can be inserted into a SQL
        /// query.
        /// </summary>
        internal static string ConvertNotebookDocToSql(NotebookDocument? doc)
        {
            if (doc?.Cells == null)
            {
                return string.Empty;
            }
            else
            {
                // Add an extra blank line between each block for readability
                return string.Join(Environment.NewLine + Environment.NewLine, doc.Cells.Select(cell =>
                {
                    // Notebooks use \n newlines, so convert the cell source to \r\n if running on Windows.
                    IEnumerable<string> cellSource;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        cellSource = cell.Source.Select(text => text.Replace("\n", Environment.NewLine));
                    }
                    else
                    {
                        cellSource = cell.Source;
                    }

                    return cell.CellType switch
                    {
                        // Markdown is text so wrapped in a comment block
                        "markdown" => $@"/*
{string.Join("", cellSource)}
*/",
                        // Everything else (just code blocks for now) is left as is
                        _ => string.Join("", cellSource),
                    };
                }));
            }
        }
    }

}
