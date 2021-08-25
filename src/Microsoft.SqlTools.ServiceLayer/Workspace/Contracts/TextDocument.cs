//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Diagnostics;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Workspace.Contracts
{
    /// <summary>
    /// Defines a base parameter class for identifying a text document.
    /// </summary>
    [DebuggerDisplay("TextDocumentIdentifier = {Uri}")]
    public class TextDocumentIdentifier
    {
        /// <summary>
        /// Gets or sets the URI which identifies the path of the
        /// text document.
        /// </summary>
        public string Uri { get; set; }
    }   

    /// <summary>
    /// Defines a position in a text document.
    /// </summary>
    [DebuggerDisplay("TextDocumentPosition = {Position.Line}:{Position.Character}")]
    public class TextDocumentPosition
    {
        /// <summary>
        /// Gets or sets the document identifier.
        /// </summary>
        public TextDocumentIdentifier TextDocument { get; set; }

        /// <summary>
        /// Gets or sets the position in the document.
        /// </summary>
        public Position Position { get; set; }
    }

    /// <summary>
    /// Defines a text document.
    /// </summary>
    [DebuggerDisplay("TextDocumentItem = {Uri}")]
    public class TextDocumentItem
    {
        /// <summary>
        /// Gets or sets the URI which identifies the path of the
        /// text document.
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// Gets or sets the language of the document
        /// </summary>
        public string LanguageId { get; set; }

        /// <summary>
        /// Gets or sets the version of the document
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the full content of the document.
        /// </summary>
        public string Text { get; set; }
    }

    public class DidOpenTextDocumentNotification
    {
        public static readonly
            EventType<DidOpenTextDocumentNotification> Type =
            EventType<DidOpenTextDocumentNotification>.Create("textDocument/didOpen");

        /// <summary>
        /// Gets or sets the opened document.
        /// </summary>
        public TextDocumentItem TextDocument { get; set; }
    }

    public class DidCloseTextDocumentNotification
    {
        public static readonly
            EventType<DidCloseTextDocumentParams> Type =
            EventType<DidCloseTextDocumentParams>.Create("textDocument/didClose");
    }

    public class DidChangeTextDocumentNotification
    {
        public static readonly
            EventType<DidChangeTextDocumentParams> Type =
            EventType<DidChangeTextDocumentParams>.Create("textDocument/didChange");
    }

    public class DidCloseTextDocumentParams
    {
        /// <summary>
        /// Gets or sets the closed document.
        /// </summary>
        public TextDocumentItem TextDocument { get; set; }
    }

    public class DidChangeTextDocumentParams
    {
        /// <summary>
        /// Gets or sets the changed document.
        /// </summary>
        public VersionedTextDocumentIdentifier TextDocument { get; set; } 

        /// <summary>
        /// Gets or sets the list of changes to the document content.
        /// </summary>
        public TextDocumentChangeEvent[] ContentChanges { get; set; }
    }

    /// <summary>
    /// Define a specific version of a text document
    /// </summary>
    public class VersionedTextDocumentIdentifier : TextDocumentIdentifier
    {        
        /// <summary>
        /// Gets or sets the Version of the changed text document 
        /// </summary>
        public int Version { get; set; }
    }

    public class TextDocumentChangeEvent
    {
        /// <summary>
        /// Gets or sets the Range where the document was changed.  Will
        /// be null if the server's TextDocumentSyncKind is Full.
        /// </summary>
        public Range? Range { get; set; }

        /// <summary>
        /// Gets or sets the length of the Range being replaced in the
        /// document.  Will be null if the server's TextDocumentSyncKind is 
        /// Full.
        /// </summary>
        public int? RangeLength { get; set; }

        /// <summary>
        /// Gets or sets the new text of the document.
        /// </summary>
        public string Text { get; set; }
    }

    [DebuggerDisplay("Position = {Line}:{Character}")]
    public class Position
    {
        /// <summary>
        /// Gets or sets the zero-based line number.
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// Gets or sets the zero-based column number.
        /// </summary>
        public int Character { get; set; }

        /// <summary>
        /// Overrides the base equality method
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null ||  (obj as Position == null))
            {
                return false;
            }
            Position p = (Position) obj;
            bool result = (Line == p.Line) && (Character == p.Character);
            return result;
        }


        /// <summary>
        /// Overrides the base GetHashCode method
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + Line.GetHashCode();
            hash = hash * 23 + Character.GetHashCode();
            return hash;
        }
    }

    [DebuggerDisplay("Start = {Start.Line}:{Start.Character}, End = {End.Line}:{End.Character}")]
    public struct Range
    {
        /// <summary>
        /// Gets or sets the starting position of the range.
        /// </summary>
        public Position Start { get; set; }

        /// <summary>
        /// Gets or sets the ending position of the range.
        /// </summary>
        public Position End { get; set; }

        /// <summary>
        /// Overrides the base equality method
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
           

            if (obj == null || !(obj is Range))
            {
                return false;
            }
            Range range = (Range) obj;
            bool sameStart = range.Start.Equals(Start);
            bool sameEnd = range.End.Equals(End);
            return (sameStart && sameEnd);
        }

        /// <summary>
        /// Overrides the base GetHashCode method
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + Start.GetHashCode();
            hash = hash * 23 + End.GetHashCode();
            return hash;
        }
    }

    [DebuggerDisplay("Range = {Range.Start.Line}:{Range.Start.Character} - {Range.End.Line}:{Range.End.Character}, Uri = {Uri}")]
    public class Location
    {
        /// <summary>
        /// Gets or sets the URI indicating the file in which the location refers.
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// Gets or sets the Range indicating the range in which location refers.
        /// </summary>
        public Range Range { get; set; }

        /// <summary>
        /// Overrides the base equality method
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null || (obj as Location == null))
            {
                return false;
            }
            Location loc = (Location)obj;
            bool sameUri = string.Equals(loc.Uri, Uri);
            bool sameRange = loc.Range.Equals(Range);
            return (sameUri && sameRange);
        }

        /// <summary>
        /// Overrides the base GetHashCode method
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + Uri.GetHashCode();
            hash = hash * 23 + Range.GetHashCode();
            return hash;
        }
    }

    public enum FileChangeType
    {
        Created = 1,

        Changed,

        Deleted
    }

    public class FileEvent
    {
        public string Uri { get; set; }

        public FileChangeType Type { get; set; }
    }
}

