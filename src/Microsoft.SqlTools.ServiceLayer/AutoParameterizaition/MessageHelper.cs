//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Globalization;

namespace Microsoft.SqlTools.ServiceLayer.AutoParameterizaition.Helpers
{
    class MessageHelper
    {
        private static readonly string ERROR_MESSAGE_TEMPLATE = "Unable to Convert {0} to a Microsoft.Data.SqlClient.SqlParameter object. The specified literal cannot be converted to {1}(System.Data.SqlDbType).";
        private static readonly string DATE_TIME_ERROR_MESSAGE_TEMPLATE = "Unable to Convert {0} to a Microsoft.Data.SqlClient.SqlParameter object. The specified literal cannot be converted to {1}(System.Data.SqlDbType), as it used an unsupported date/time format. Use one of the supported Date/time formats.";
        private static readonly string BINARY_LITERAL_PREFIX_MISSING_ERROR_TEMPLATE = "Unable to Convert {0} to a Microsoft.Data.SqlClient.SqlParameter object. The specified literal cannot be converted to {1}(System.Data.SqlDbType), as prefix 0x is expected for a binary literals.";

        public static string GetLocalizedMessage(MessageType type, string variableName, string sqlDataType, string literalValue)
        {
            switch (type)
            {
                case MessageType.ERROR_MESSAGE:
                    return SR.ErrorMessage(variableName, sqlDataType, literalValue);
                case MessageType.DATE_TIME_ERROR_MESSAGE:
                    return SR.DateTimeErrorMessage(variableName, sqlDataType, literalValue);
                case MessageType.BINARY_LITERAL_PREFIX_MISSING_ERROR:
                    return SR.BinaryLiteralPrefixMissingError(variableName, sqlDataType, literalValue);
                default:
                    return "";
            }
        }

        public static string GetLocaleInvariantMessage(MessageType type, string variableName, string sqlDataType)
        {
            switch (type)
            {
                case MessageType.ERROR_MESSAGE:
                    return string.Format(CultureInfo.InvariantCulture, ERROR_MESSAGE_TEMPLATE, variableName, sqlDataType);
                case MessageType.DATE_TIME_ERROR_MESSAGE:
                    return string.Format(CultureInfo.InvariantCulture, DATE_TIME_ERROR_MESSAGE_TEMPLATE, variableName, sqlDataType);
                case MessageType.BINARY_LITERAL_PREFIX_MISSING_ERROR:
                    return string.Format(CultureInfo.InvariantCulture, BINARY_LITERAL_PREFIX_MISSING_ERROR_TEMPLATE, variableName, sqlDataType);
                default:
                    return "";
            }
        }

        public enum MessageType
        {
            ERROR_MESSAGE,
            DATE_TIME_ERROR_MESSAGE,
            BINARY_LITERAL_PREFIX_MISSING_ERROR,
        }
    }
}
