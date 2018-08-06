//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.DataProtocol.Contracts.Utilities;
using Newtonsoft.Json;

namespace Microsoft.SqlTools.DataProtocol.Contracts.Common
{
    [Flags]
    [JsonConverter(typeof(FlagsIntConverter))]
    public enum CompletionItemKinds
    {
        [FlagsIntConverter.SerializeValue(1)]
        Text = 1 << 0,
        
        [FlagsIntConverter.SerializeValue(2)]
        Method = 1 << 1,
        
        [FlagsIntConverter.SerializeValue(3)]
        Function = 1 << 2,
        
        [FlagsIntConverter.SerializeValue(4)]
        Constructor = 1 << 3,
        
        [FlagsIntConverter.SerializeValue(5)]
        Field = 1 << 4,
        
        [FlagsIntConverter.SerializeValue(6)]
        Variable = 1 << 5,
        
        [FlagsIntConverter.SerializeValue(7)]
        Class = 1 << 6,
        
        [FlagsIntConverter.SerializeValue(8)]
        Interface = 1 << 7,
        
        [FlagsIntConverter.SerializeValue(9)]
        Module = 1 << 8,
        
        [FlagsIntConverter.SerializeValue(10)]
        Property = 1 << 9,
        
        [FlagsIntConverter.SerializeValue(11)]
        Unit = 1 << 10,
        
        [FlagsIntConverter.SerializeValue(12)]
        Value = 1 << 11,
        
        [FlagsIntConverter.SerializeValue(13)]
        Enum = 1 << 12,
        
        [FlagsIntConverter.SerializeValue(14)]
        Keyword = 1 << 13,
        
        [FlagsIntConverter.SerializeValue(15)]
        Snippet = 1 << 14,
        
        [FlagsIntConverter.SerializeValue(16)]
        Color = 1 << 15,
        
        [FlagsIntConverter.SerializeValue(17)]
        File = 1 << 16,
        
        [FlagsIntConverter.SerializeValue(18)]
        Reference = 1 << 17,
        
        [FlagsIntConverter.SerializeValue(19)]
        Folder = 1 << 18,
        
        [FlagsIntConverter.SerializeValue(20)]
        EnumMember = 1 << 19,
        
        [FlagsIntConverter.SerializeValue(21)]
        Constant = 1 << 20,
        
        [FlagsIntConverter.SerializeValue(22)]
        Struct = 1 << 21,
        
        [FlagsIntConverter.SerializeValue(23)]
        Event = 1 << 22,
        
        [FlagsIntConverter.SerializeValue(24)]
        Operator = 1 << 23,
        
        [FlagsIntConverter.SerializeValue(25)]
        TypeParameter = 1 << 24
    }
}