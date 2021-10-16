//------------------------------------------------------------------------------
// <copyright file="SqlTypeDisplay.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Tools.Schema.SchemaModel;
using Microsoft.Data.Tools.Schema.ScriptDom.Sql;
using Microsoft.Data.Tools.Schema.Sql.SchemaModel;
using Microsoft.Data.Tools.Components.Diagnostics;

namespace Microsoft.Data.Relational.Design.Table
{
    internal abstract class SqlTypeDisplayBase
    {
        private SqlType _sqlType;
        private string _dataTypeDisplayName;

        public SqlTypeDisplayBase(SqlType sqlType)
        {
            _sqlType = sqlType;
            _dataTypeDisplayName = VMUtils.GetDataTypeDisplayName(_sqlType);
        }

        internal virtual void Initialize()
        {
            // Do nothing
        }

        public SqlType SqlType
        {
            get
            {
                return _sqlType;
            }
        }

        public virtual string RepresentName
        {
            get
            {
                return _dataTypeDisplayName;
            }
        }

        public virtual string DisplayName
        {
            get
            {
                return _dataTypeDisplayName;
            }
        }

        public virtual void UpdateWithDesignTimeValue(SqlSimpleColumn sqlSimpleColumn)
        {
            // Do nothing
        }
    }

    internal class SqlTypeDisplayDefault : SqlTypeDisplayBase
    {
        public SqlTypeDisplayDefault(SqlType sqlType)
            : base(sqlType)
        {
        }
    }

    internal class SqlTypeDisplayWithLength : SqlTypeDisplayBase
    {
        private SqlBuiltInType _sqlBuiltInType;
        private string _displayName;

        public SqlTypeDisplayWithLength(SqlBuiltInType sqlBuiltInType)
            : base(sqlBuiltInType)
        {
            _sqlBuiltInType = sqlBuiltInType;
        }

        internal override void Initialize()
        {
            int defaultLength;

            if (SqlTypeDisplayFactory.SqlTypeCanDisplayWithDefaultLengthMap.ContainsKey(_sqlBuiltInType.SqlDataType))
            {
                defaultLength = SqlTypeDisplayFactory.SqlTypeCanDisplayWithDefaultLengthMap[_sqlBuiltInType.SqlDataType];
            }
            else
            {
                SqlTracer.DebugTraceEvent(TraceEventType.Error, SqlTraceId.TableDesigner, "Why sql type can't have length?");
                defaultLength = 1;
            }

            _displayName = this.GetDisplayNameInternal(defaultLength);
        }
        
        public override string DisplayName
        {
            get
            {
                return _displayName;
            }
        }
        
        public override void UpdateWithDesignTimeValue(SqlSimpleColumn sqlSimpleColumn)
        {
            SqlTypeSpecifier typeSpec = sqlSimpleColumn.TypeSpecifier as SqlTypeSpecifier;
            if (typeSpec != null && typeSpec.IsMax == false)
            {
                _displayName = this.GetDisplayNameInternal(typeSpec.Length);
            }
        }

        private string GetDisplayNameInternal(int length)
        {
            return String.Format(CultureInfo.CurrentCulture, "{0}({1})", this.RepresentName, length);
        }
    }

    internal class SqlTypeDisplayWithScale : SqlTypeDisplayBase
    {
        private SqlBuiltInType _sqlBuiltInType;
        private string _displayName;

        public SqlTypeDisplayWithScale(SqlBuiltInType sqlBuiltInType)
            : base(sqlBuiltInType)
        {
            _sqlBuiltInType = sqlBuiltInType;
        }

        internal override void Initialize()
        {
            int defaultScale;

            if (SqlTypeDisplayFactory.SqlTypeCanDisplayWithDefaultScaleMap.ContainsKey(_sqlBuiltInType.SqlDataType))
            {
                defaultScale = SqlTypeDisplayFactory.SqlTypeCanDisplayWithDefaultScaleMap[_sqlBuiltInType.SqlDataType];
            }
            else
            {
                SqlTracer.DebugTraceEvent(TraceEventType.Error, SqlTraceId.TableDesigner, "Why sql type can't have scale?");
                defaultScale = 1;
            }

            _displayName = this.GetDisplayNameInternal(defaultScale);
        }

        public override string DisplayName
        {
            get
            {
                return _displayName;
            }
        }

        public override void UpdateWithDesignTimeValue(SqlSimpleColumn sqlSimpleColumn)
        {
            SqlTypeSpecifier typeSpec = sqlSimpleColumn.TypeSpecifier as SqlTypeSpecifier;
            if (typeSpec != null)
            {
                _displayName = this.GetDisplayNameInternal(typeSpec.Scale);
            }
        }

        private string GetDisplayNameInternal(int scale)
        {
            return String.Format(CultureInfo.CurrentCulture, "{0}({1})", this.RepresentName, scale);
        }
    }

    internal class SqlTypeDisplayWithScaleAndPrecision : SqlTypeDisplayBase
    {
        private SqlBuiltInType _sqlBuiltInType;
        private string _displayName;

        public SqlTypeDisplayWithScaleAndPrecision(SqlBuiltInType sqlBuiltInType)
            : base(sqlBuiltInType)
        {
            _sqlBuiltInType = sqlBuiltInType;
        }

        internal override void Initialize()
        {
            int defaultScale;

            if (SqlTypeDisplayFactory.SqlTypeCanDisplayWithDefaultScaleMap.ContainsKey(_sqlBuiltInType.SqlDataType))
            {
                defaultScale = SqlTypeDisplayFactory.SqlTypeCanDisplayWithDefaultScaleMap[_sqlBuiltInType.SqlDataType];
            }
            else
            {
                SqlTracer.DebugTraceEvent(TraceEventType.Error, SqlTraceId.TableDesigner, "Why sql type can't have scale?");
                defaultScale = 1;
            }

            int defaultPrecision;

            if (SqlTypeDisplayFactory.SqlTypeCanDisplayWithDefaultPrecisionMap.ContainsKey(_sqlBuiltInType.SqlDataType))
            {
                defaultPrecision = SqlTypeDisplayFactory.SqlTypeCanDisplayWithDefaultPrecisionMap[_sqlBuiltInType.SqlDataType];
            }
            else
            {
                SqlTracer.DebugTraceEvent(TraceEventType.Error, SqlTraceId.TableDesigner, "Why sql type can't have Precision?");
                defaultPrecision = 1;
            }

            _displayName = this.GetDisplayNameInternal(defaultPrecision, defaultScale);
        }

        public override string DisplayName
        {
            get
            {
                return _displayName;
            }
        }

        public override void UpdateWithDesignTimeValue(SqlSimpleColumn sqlSimpleColumn)
        {
            SqlTypeSpecifier typeSpec = sqlSimpleColumn.TypeSpecifier as SqlTypeSpecifier;
            if (typeSpec != null)
            {
                _displayName = this.GetDisplayNameInternal(typeSpec.Precision, typeSpec.Scale);
            }
        }

        private string GetDisplayNameInternal(int precision, int scale)
        {
            return String.Format(CultureInfo.CurrentCulture, "{0}({1},{2})", this.RepresentName, precision, scale);
        }
    }

    internal class SqlTypeDisplayWithMaxLength : SqlTypeDisplayBase
    {
        private string _displayName;

        public SqlTypeDisplayWithMaxLength(SqlBuiltInType sqlBuiltInType)
            : base(sqlBuiltInType)
        {
        }

        internal override void Initialize()
        {
            _displayName = this.GetDisplayNameInternal();
        }

        public override string DisplayName
        {
            get
            {
                return _displayName;
            }
        }

        private string GetDisplayNameInternal()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0}({1})", this.RepresentName, CodeGenerationSupporter.Max);
        }
    }

    internal class SqlUnResolvedTypeDisplay : SqlTypeDisplayBase
    {
        private string _displayName;

        public SqlUnResolvedTypeDisplay(SqlTypeSpecifierBase sqlTypeSpecifier)
            : base(null)
        {
            _displayName = this.GetDisplayNameInternal(sqlTypeSpecifier);
        }

        public override string DisplayName
        {
            get
            {
                return _displayName;
            }
        }

        public override string RepresentName
        {
            get
            {
                return _displayName;
            }
        }

        private string GetDisplayNameInternal(SqlTypeSpecifierBase sqlTypeSpecifier)
        {
            // type specifier has not been resolved yet
            IModelSingleRelationship<SqlTypeSpecifierBase, SqlType> relationToType = sqlTypeSpecifier.GetTypeRelationship();
            if (relationToType != null)
            {
                SqlModelBuilderResolvableAnnotation anno =
                    relationToType.GetRelationshipEntry().GetAnnotations<SqlModelBuilderResolvableAnnotation>().FirstOrDefault();
                if (anno != null)
                {
                    return VMUtils.GetDataTypeDisplayName(anno.Name);
                }
            }

            return String.Empty;
        }
    }
}
