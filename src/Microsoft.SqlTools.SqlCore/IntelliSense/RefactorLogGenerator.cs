//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable enable

using System;
using System.Globalization;
using System.Xml.Linq;

namespace Microsoft.SqlTools.SqlCore.IntelliSense
{
    /// <summary>
    /// Generates the contents of a SQL project refactor log (.refactorlog) file.
    /// </summary>
    /// <remarks>
    /// The refactor log records rename/refactor operations so that DacFx deployment can preserve
    /// them instead of dropping and recreating affected objects. This helper only produces the file
    /// content as a string; it never reads from or writes to disk. The caller is responsible for
    /// persisting the returned content (for example, through a VS Code workspace edit so the write
    /// participates in the rename preview's apply/discard).
    /// </remarks>
    public static class RefactorLogGenerator
    {
        // Serialization schema used by the .refactorlog file. Matches the format produced by SSDT / Visual Studio.
        private static readonly XNamespace RefactorNamespace = "http://schemas.microsoft.com/sqlserver/dac/Serialization/2012/02";
        private const string OperationsElement = "Operations";
        private const string OperationElement = "Operation";
        private const string PropertyElement = "Property";
        private const string VersionAttribute = "Version";
        private const string NameAttribute = "Name";
        private const string KeyAttribute = "Key";
        private const string ValueAttribute = "Value";
        private const string ChangeDateTimeAttribute = "ChangeDateTime";
        private const string OperationsVersion = "1.0";
        private const string RenameOperationName = "Rename Refactor";
        private const string ChangeDateTimeFormat = "MM/dd/yyyy HH:mm:ss";

        /// <summary>
        /// Returns refactor log content with a new rename operation appended.
        /// </summary>
        /// <param name="existingContent">
        /// Current content of the .refactorlog file, or <see langword="null"/>/empty if the project does not have one yet.
        /// </param>
        /// <param name="elementName">Fully-bracketed name of the renamed element, e.g. <c>[dbo].[Customers]</c>.</param>
        /// <param name="elementType">DacFx element type of the renamed element, e.g. <c>SqlTable</c>.</param>
        /// <param name="parentElementName">Fully-bracketed name of the parent element, e.g. <c>[dbo]</c>.</param>
        /// <param name="parentElementType">DacFx element type of the parent element, e.g. <c>SqlSchema</c>.</param>
        /// <param name="newName">New (unbracketed) name of the element.</param>
        /// <returns>The full .refactorlog file content to be written by the caller.</returns>
        public static string GenerateRenameDocument(
            string? existingContent,
            string elementName,
            string elementType,
            string parentElementName,
            string parentElementType,
            string newName)
        {
            ThrowIfNullOrWhiteSpace(elementName, nameof(elementName));
            ThrowIfNullOrWhiteSpace(elementType, nameof(elementType));
            ThrowIfNullOrWhiteSpace(parentElementName, nameof(parentElementName));
            ThrowIfNullOrWhiteSpace(parentElementType, nameof(parentElementType));
            ThrowIfNullOrWhiteSpace(newName, nameof(newName));

            XDocument document = LoadOrCreateDocument(existingContent);

            XElement operation = new XElement(RefactorNamespace + OperationElement,
                new XAttribute(NameAttribute, RenameOperationName),
                new XAttribute(KeyAttribute, Guid.NewGuid().ToString()),
                new XAttribute(ChangeDateTimeAttribute, DateTime.UtcNow.ToString(ChangeDateTimeFormat, CultureInfo.InvariantCulture)),
                CreateProperty("ElementName", elementName),
                CreateProperty("ElementType", elementType),
                CreateProperty("ParentElementName", parentElementName),
                CreateProperty("ParentElementType", parentElementType),
                CreateProperty("NewName", newName));

            XElement operationsRoot = document.Root ?? throw new InvalidOperationException("Refactorlog document is missing its root element.");
            operationsRoot.Add(operation);

            // Only prefix the declaration (and its trailing newline) when one is present; a parsed
            // document without a declaration would otherwise produce a leading blank line.
            return document.Declaration != null
                ? document.Declaration + Environment.NewLine + document.ToString()
                : document.ToString();
        }

        private static XElement CreateProperty(string name, string value)
            => new XElement(RefactorNamespace + PropertyElement,
                new XAttribute(NameAttribute, name),
                new XAttribute(ValueAttribute, value));

        private static XDocument LoadOrCreateDocument(string? existingContent)
        {
            if (!string.IsNullOrWhiteSpace(existingContent))
            {
                try
                {
                    XDocument existing = XDocument.Parse(existingContent);

                    if (existing.Root != null && existing.Root.Name == RefactorNamespace + OperationsElement)
                    {
                        return existing;
                    }
                }
                catch (System.Xml.XmlException)
                {
                    // The supplied .refactorlog content is missing, truncated, or otherwise invalid
                    // XML. Fall through and create a fresh document rather than failing the rename.
                }
            }

            return new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(RefactorNamespace + OperationsElement,
                    new XAttribute(VersionAttribute, OperationsVersion)));
        }

        private static void ThrowIfNullOrWhiteSpace(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be null, empty, or whitespace.", paramName);
            }
        }
    }
}
