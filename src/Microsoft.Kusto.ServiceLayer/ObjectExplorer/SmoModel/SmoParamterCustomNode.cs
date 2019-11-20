//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Globalization;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.SmoModel
{
    /// <summary>
    /// Custom name for parameters
    /// </summary>
    internal partial class TableValuedFunctionParametersChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeCustomName(object smoObject, SmoQueryContext smoContext)
        {
            return ParameterCustomeNodeHelper.GetCustomLabel(smoObject, smoContext);
        }

        public override string GetNodeSubType(object smoObject, SmoQueryContext smoContext)
        {
            return ParameterCustomeNodeHelper.GetSubType(smoObject);
        }
    }

    /// <summary>
    /// Custom name for parameters
    /// </summary>
    internal partial class ScalarValuedFunctionParametersChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeCustomName(object smoObject, SmoQueryContext smoContext)
        {
            return ParameterCustomeNodeHelper.GetCustomLabel(smoObject, smoContext);
        }
        public override string GetNodeSubType(object smoObject, SmoQueryContext smoContext)
        {
            return ParameterCustomeNodeHelper.GetSubType(smoObject);
        }
    }

    /// <summary>
    /// Custom name for parameters
    /// </summary>
    internal partial class AggregateFunctionParametersChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeCustomName(object smoObject, SmoQueryContext smoContext)
        {
            return ParameterCustomeNodeHelper.GetCustomLabel(smoObject, smoContext);
        }
        public override string GetNodeSubType(object smoObject, SmoQueryContext smoContext)
        {
            return ParameterCustomeNodeHelper.GetSubType(smoObject);
        }
    }

    /// <summary>
    /// Custom name for parameters
    /// </summary>
    internal partial class StoredProcedureParametersChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeCustomName(object smoObject, SmoQueryContext smoContext)
        {
            return ParameterCustomeNodeHelper.GetCustomLabel(smoObject, smoContext);
        }
        public override string GetNodeSubType(object smoObject, SmoQueryContext smoContext)
        {
            return ParameterCustomeNodeHelper.GetSubType(smoObject);
        }
    }

    static class ParameterCustomeNodeHelper
    {
        internal static string GetSubType(object context)
        {
            Parameter parameter = context as Parameter;
            if (parameter != null)
            {
                StoredProcedureParameter stordProcedureParameter = parameter as StoredProcedureParameter;
                if (stordProcedureParameter != null && stordProcedureParameter.IsOutputParameter)
                {
                    return "Output";
                }
                return "Input";
                //TODO return parameters
            }
            return string.Empty;

        }

        internal static string GetCustomLabel(object context, SmoQueryContext smoContext)
        {
            Parameter parameter = context as Parameter;
            if (parameter != null)
            {
                return GetParameterCustomLabel(parameter);
            }

            return string.Empty;
        }

        internal static string GetParameterCustomLabel(Parameter parameter)
        {
            string label = parameter.Name;
            string defaultString = SR.SchemaHierarchy_SubroutineParameterNoDefaultLabel;
            string inputOutputString = SR.SchemaHierarchy_SubroutineParameterInputLabel;
            string typeName = parameter.DataType.ToString();

            if (parameter.DefaultValue != null &&
                !string.IsNullOrEmpty(parameter.DefaultValue))
            {
                defaultString = SR.SchemaHierarchy_SubroutineParameterDefaultLabel;
            }

            StoredProcedureParameter stordProcedureParameter = parameter as StoredProcedureParameter;
            if (stordProcedureParameter != null && stordProcedureParameter.IsOutputParameter)
            {
                inputOutputString = SR.SchemaHierarchy_SubroutineParameterInputOutputLabel;
                if (parameter.IsReadOnly)
                {
                    inputOutputString = SR.SchemaHierarchy_SubroutineParameterInputOutputReadOnlyLabel;
                }
            }
            else if (parameter.IsReadOnly)
            {
                inputOutputString = SR.SchemaHierarchy_SubroutineParameterInputReadOnlyLabel;
            }

            return string.Format(CultureInfo.InvariantCulture,
                                         SR.SchemaHierarchy_SubroutineParameterLabelFormatString,
                                         label,
                                         typeName,
                                         inputOutputString,
                                         defaultString);
        }
    }
}
