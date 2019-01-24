//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    public interface ICommandHandler
    {
        BatchParserAction Go(TextBlock batch, int repeatCount);
        BatchParserAction OnError(Token token, OnErrorAction action);
        BatchParserAction Include(TextBlock filename, out TextReader stream, out string newFilename);
    }
}
