//------------------------------------------------------------------------------
// <copyright file="SqlSequencePickerItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.Relational.Design.Controls;
using Microsoft.Data.Tools.Design.Core.Context;
using Microsoft.Data.Tools.Schema.ScriptDom.Sql;
using Microsoft.Data.Tools.Schema.Sql.SchemaModel;

namespace Microsoft.Data.Relational.Design.Table
{
    internal class SqlSequencePickerItem : PickerItemBase
    {
        public SqlSequencePickerItem(SqlSequence sequence)
            : base(String.Empty, String.Empty)
        {
            this.SqlSequence = sequence;

            if (sequence != null)
            {
                StringBuilder name = new StringBuilder();
                this.FullyQualifiedName = sequence.GetName(Tools.Schema.ElementNameStyle.FullyQualifiedName);
                name.Append(FullyQualifiedName);

                name.Append(CodeGenerationSupporter.LeftParenthesis);
                name.Append(VMUtils.GetSequenceDataTypeForDisplay(sequence));
                name.Append(CodeGenerationSupporter.RightParenthesis);

                this.Name = name.ToString();
            }
        }

        public SqlSequence SqlSequence
        {
            get;
            protected set;
        }

        public string FullyQualifiedName
        {
            get;
            protected set;
        }
    }

    internal class NullSqlSequencePickerItem : SqlSequencePickerItem
    {
        public NullSqlSequencePickerItem()
            : base(null)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(CodeGenerationSupporter.LeftParenthesis);
            sb.Append(ViewModelResources.None);
            sb.Append(CodeGenerationSupporter.RightParenthesis);

            this.Name = sb.ToString();
            this.FullyQualifiedName = this.Name;
        }
    }

    internal class AddNewSqlSequencePickerItem : SqlSequencePickerItem
    {
        public AddNewSqlSequencePickerItem()
            : base(null)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(CodeGenerationSupporter.LeftParenthesis);
            sb.Append(ViewModelResources.AddNew);
            sb.Append(CodeGenerationSupporter.RightParenthesis);
            this.Name = sb.ToString();
            this.FullyQualifiedName = this.Name;
        }

        public delegate bool? SelectionAction(EditingContext context, string suggestedName, out string newName);

        public static SelectionAction GetNewSequenceName;
    }

    internal class SqlSequenceComparer : IComparer<SqlSequencePickerItem>
    {
        public int Compare(SqlSequencePickerItem x, SqlSequencePickerItem y)
        {
            int comparison = 0;

            if (x == y)
            {
                return 0;
            }
            else if (y is AddNewSqlSequencePickerItem)
            {
                return 1;
            }
            else if (x is AddNewSqlSequencePickerItem)
            {
                return -1;
            }
            else if (x is NullSqlSequencePickerItem)
            {
                return -1;
            }
            else if (y is NullSqlSequencePickerItem)
            {
                return 1;
            }
            else
            {
                comparison = string.Compare(x.Name, y.Name, StringComparison.CurrentCulture);

                return comparison;
            }
        }
    }

}