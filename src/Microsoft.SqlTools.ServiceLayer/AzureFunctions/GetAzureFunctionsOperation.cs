//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.SqlTools.ServiceLayer.AzureFunctions.Contracts;
using Microsoft.SqlTools.Utility;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.SqlTools.ServiceLayer.AzureFunctions
{
    /// <summary>
    /// Class to represent getting the Azure Functions in a file
    /// </summary>
    class GetAzureFunctionsOperation
    {
        public GetAzureFunctionsParams Parameters { get; }

        public GetAzureFunctionsOperation(GetAzureFunctionsParams parameters)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
        }

        /// <summary>
        /// Gets the names of all the azure functions in a file
        /// </summary>
        /// <returns>the result of trying to get the names of all the Azure functions in a file</returns>
        public GetAzureFunctionsResult GetAzureFunctions()
        {
            try
            {
                string text = File.ReadAllText(Parameters.FilePath);

                SyntaxTree tree = CSharpSyntaxTree.ParseText(text);
                CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

                // get all the method declarations with the FunctionName attribute
                IEnumerable<MethodDeclarationSyntax> methodsWithFunctionAttributes = AzureFunctionsUtils.GetMethodsWithFunctionAttributes(root);

                var azureFunctions = methodsWithFunctionAttributes.Select(m => new AzureFunction(m.GetFunctionName(), m.GetHttpRoute())).ToArray();

                return new GetAzureFunctionsResult(azureFunctions);
            }
            catch (Exception ex)
            {
                Logger.Write(TraceEventType.Information, $"Failed to get Azure functions. Error: {ex.Message}");
                throw;
            }
        }
    }
}