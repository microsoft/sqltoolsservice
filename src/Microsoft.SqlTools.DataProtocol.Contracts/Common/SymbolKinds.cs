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
    public enum SymbolKinds
    {
        [FlagsIntConverter.SerializeValue(1)]
        File = 1 << 0,
        
        [FlagsIntConverter.SerializeValue(2)]
        Module = 1 << 1,
        
        [FlagsIntConverter.SerializeValue(3)]
        Namespace = 1 << 2,
        
        [FlagsIntConverter.SerializeValue(4)]
        Package = 1 << 3,
        
        [FlagsIntConverter.SerializeValue(5)]
        Class = 1 << 4,
        
        [FlagsIntConverter.SerializeValue(6)]
        Method = 1 << 5,
        
        [FlagsIntConverter.SerializeValue(7)]
        Property = 1 << 6,
        
        [FlagsIntConverter.SerializeValue(8)]
        Field = 1 << 7,
        
        [FlagsIntConverter.SerializeValue(9)]
        Constructor = 1 << 8,
        
        [FlagsIntConverter.SerializeValue(10)]
        Enum = 1 << 9,
        
        [FlagsIntConverter.SerializeValue(11)]
        Interface = 1 << 10,
        
        [FlagsIntConverter.SerializeValue(12)]
        Function = 1 << 11,
        
        [FlagsIntConverter.SerializeValue(13)]
        Variable = 1 << 12,
        
        [FlagsIntConverter.SerializeValue(14)]
        Constant = 1 << 13,
        
        [FlagsIntConverter.SerializeValue(15)]
        String = 1 << 14,
        
        [FlagsIntConverter.SerializeValue(16)]
        Number = 1 << 15,
        
        [FlagsIntConverter.SerializeValue(17)]
        Boolean = 1 << 16,
        
        [FlagsIntConverter.SerializeValue(18)]
        Array = 1 << 17,
        
        [FlagsIntConverter.SerializeValue(19)]
        Object = 1 << 18,
        
        [FlagsIntConverter.SerializeValue(20)]
        Key = 1 << 19,
        
        [FlagsIntConverter.SerializeValue(21)]
        Null = 1 << 20,
        
        [FlagsIntConverter.SerializeValue(22)]
        EnumMember = 1 << 21,
        
        [FlagsIntConverter.SerializeValue(23)]
        Struct = 1 << 22,
        
        [FlagsIntConverter.SerializeValue(24)]
        Event = 1 << 23,
        
        [FlagsIntConverter.SerializeValue(25)]
        Operator = 1 << 24,
        
        [FlagsIntConverter.SerializeValue(26)]
        TypeParameter = 1 << 25
    }
}