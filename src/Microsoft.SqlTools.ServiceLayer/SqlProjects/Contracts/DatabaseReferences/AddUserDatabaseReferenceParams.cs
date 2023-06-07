//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    public abstract class AddUserDatabaseReferenceParams : AddDatabaseReferenceParams
    {
        /// <summary>
        /// SQLCMD variable name for specifying the other database this reference is to, if different from that of the current project
        /// </summary>
        public string? DatabaseVariable { get; set; }

        /// <summary>
        /// SQLCMD variable name for specifying the other server this reference is to, if different from that of the current project.
        /// If this is set, DatabaseVariable must also be set.
        /// </summary>
        public string? ServerVariable { get; set; }

        /// <summary>
        /// Throws if either both DatabaseVariable and DatabaseLiteral are set. This only validates
        /// what is necessary for Tools Service.  The DacFx Projects library does comprehensive validation.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        internal void Validate()
        {
            if (!string.IsNullOrWhiteSpace(DatabaseVariable) && !string.IsNullOrWhiteSpace(DatabaseLiteral))
            {
                throw new ArgumentException($"Both {nameof(DatabaseVariable)} and {nameof(DatabaseLiteral)} cannot be set.");
            }
        }
    }
}
