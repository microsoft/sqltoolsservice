//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.CoreServices.Connection.ReliableConnection
{
    /// <summary>
    /// Constants for SQL Error numbers
    /// </summary>
    internal static class SqlErrorNumbers
    {
        // Database XYZ already exists. Choose a different database name.
        internal const int DatabaseAlreadyExistsErrorNumber = 1801;

        // Cannot drop the database 'x', because it does not exist or you do not have permission.
        internal const int DatabaseAlreadyDroppedErrorNumber = 3701;

        // Database 'x' was created\altered successfully, but some properties could not be displayed.
        internal const int DatabaseCrudMetadataUpdateErrorNumber = 45166;

        // Violation of PRIMARY KEY constraint 'x'. 
        // Cannot insert duplicate key in object 'y'. The duplicate key value is (z).
        internal const int PrimaryKeyViolationErrorNumber = 2627;

        // There is already an object named 'x' in the database.
        internal const int ObjectAlreadyExistsErrorNumber = 2714;

        // Cannot drop the object 'x', because it does not exist or you do not have permission.
        internal const int ObjectAlreadyDroppedErrorNumber = 3701;
    }
}
