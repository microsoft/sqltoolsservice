//------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------------------------

using System;
using System.IO;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    internal interface ICommandHandler
    {
        BatchParserAction Go(TextBlock batch, int repeatCount);
        BatchParserAction OnError(Token token, OnErrorAction action);
        BatchParserAction Include(TextBlock filename, out TextReader stream, out string newFilename);
    }
}
