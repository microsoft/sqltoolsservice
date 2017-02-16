//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{
    internal abstract class ASTNodeFormatterFactory
    {
        public abstract Type SupportedNodeType { get; }
        public abstract ASTNodeFormatter Create(FormatterVisitor visitor, SqlCodeObject codeObject);
    }

    internal abstract class ASTNodeFormatterFactoryT<T> : ASTNodeFormatterFactory
        where T : SqlCodeObject
    {
        public override Type SupportedNodeType
        {
            get
            {
                return typeof(T);
            }
        }

        public override ASTNodeFormatter Create(FormatterVisitor visitor, SqlCodeObject codeObject)
        {
            Validate.IsNotNull(nameof(visitor), visitor);
            Validate.IsNotNull(nameof(codeObject), codeObject);
            
            return DoCreate(visitor, codeObject as T);
        }

        protected abstract ASTNodeFormatter DoCreate(FormatterVisitor visitor, T codeObject);
    }
}
