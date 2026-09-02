//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.LanguageService.Scripting
{
    /// <summary>
    /// Hands out the file names used to hold Peek Definition scripts. An object keeps the same
    /// file for the lifetime of the process, so asking for its definition again refreshes the
    /// editor tab that is already open instead of adding another one. Two different objects that
    /// want the same file name are told apart by a numeric suffix.
    /// </summary>
    internal static class PeekDefinitionFileNames
    {
        private const string Extension = ".sql";

        private static readonly object SyncRoot = new object();

        /// <summary>
        /// Device names that Windows reserves even when they have a file extension.
        /// See https://learn.microsoft.com/windows/win32/fileio/naming-a-file.
        /// </summary>
        private static readonly HashSet<string> WindowsReservedDeviceNames =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "CON", "PRN", "AUX", "NUL",
                "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
                "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
            };

        /// <summary>Object identity to the file name assigned to it.</summary>
        private static readonly Dictionary<string, string> NamesByIdentity =
            new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// Every name handed out so far, compared the way the file system compares them so that
        /// two objects differing only by case still get separate files.
        /// </summary>
        private static readonly HashSet<string> AssignedNames =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Returns the file name to use for an object, assigning one the first time it is seen.
        /// </summary>
        /// <param name="identity">Identifies the object, from <see cref="CreateIdentity"/>.</param>
        /// <param name="baseName">The preferred file name, without extension.</param>
        internal static string GetOrAssign(string identity, string baseName)
        {
            Validate.IsNotNull(nameof(identity), identity);
            Validate.IsNotNullOrWhitespaceString(nameof(baseName), baseName);

            lock (SyncRoot)
            {
                if (NamesByIdentity.TryGetValue(identity, out string assigned))
                {
                    return assigned;
                }

                string candidate = baseName + Extension;
                for (int suffix = 2; !AssignedNames.Add(candidate); suffix++)
                {
                    candidate = $"{baseName}_{suffix}{Extension}";
                }

                NamesByIdentity[identity] = candidate;
                return candidate;
            }
        }

        /// <summary>
        /// Builds the value that distinguishes one object from another. The parts are compared
        /// case sensitively: a case sensitive collation can hold both "Foo" and "foo", and giving
        /// them one file would show the wrong definition for one of them.
        /// </summary>
        internal static string CreateIdentity(
            string serverName,
            string databaseName,
            string schemaName,
            string objectName)
        {
            StringBuilder identity = new StringBuilder();
            AppendPart(identity, serverName);
            AppendPart(identity, databaseName);
            AppendPart(identity, schemaName);
            AppendPart(identity, objectName);
            return identity.ToString();
        }

        /// <summary>
        /// Appends one part of an identity. The length prefix keeps the parts unambiguous without
        /// relying on a separator character that a quoted identifier could itself contain.
        /// </summary>
        private static void AppendPart(StringBuilder builder, string part)
        {
            part ??= string.Empty;
            builder.Append(part.Length).Append(':').Append(part);
        }

        /// <summary>
        /// Replaces characters that are legal in a quoted SQL identifier but not in a file name,
        /// and prefixes names reserved by Windows. Names that sanitize to the same base name are
        /// separated by the numeric suffix, so no definition is ever lost.
        /// </summary>
        internal static string SanitizeBaseName(string baseName)
        {
            char[] invalidCharacters = Path.GetInvalidFileNameChars();
            StringBuilder sanitized = new StringBuilder(baseName.Length);
            foreach (char character in baseName)
            {
                sanitized.Append(Array.IndexOf(invalidCharacters, character) >= 0 ? '_' : character);
            }

            string sanitizedBaseName = sanitized.ToString();
            int firstPeriod = sanitizedBaseName.IndexOf('.');
            string firstNamePart = firstPeriod >= 0
                ? sanitizedBaseName.Substring(0, firstPeriod)
                : sanitizedBaseName;

            // Windows applies device-name rules to the portion before the first period, even when
            // the complete file name has an extension (for example, CON.sql).
            if (WindowsReservedDeviceNames.Contains(firstNamePart.TrimEnd(' ', '.')))
            {
                sanitizedBaseName = "_" + sanitizedBaseName;
            }

            return sanitizedBaseName;
        }

        /// <summary>
        /// Forgets every assigned name. For tests only.
        /// </summary>
        internal static void Reset()
        {
            lock (SyncRoot)
            {
                NamesByIdentity.Clear();
                AssignedNames.Clear();
            }
        }
    }
}
