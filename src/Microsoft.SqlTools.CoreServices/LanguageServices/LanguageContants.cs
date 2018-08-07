//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.CoreServices.LanguageServices
{
    public static class LanguageContants {
        private const int OneSecond = 1000;

        internal const int DiagnosticParseDelay = 750;

        internal const int HoverTimeout = 500;

        internal const int BindingTimeout = 500;

        internal const int OnConnectionWaitTimeout = 300 * OneSecond;

        internal const int PeekDefinitionTimeout = 10 * OneSecond;

    }
}
