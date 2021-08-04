//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.SqlTools.ServiceLayer.AzureFunctions.Contracts;
using Microsoft.SqlTools.Utility;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.AzureFunctions
{
    /// <summary>
    /// Class to represent getting the Azure Functions in a file
    /// </summary>
    class GetAzureFunctionsOperation
    {
        const string functionAttributeText = "FunctionName";

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
                string text = File.ReadAllText(Parameters.filePath);

                SyntaxTree tree = CSharpSyntaxTree.ParseText(text);
                CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

                // look for Azure Functions in the file
                IEnumerable<AttributeArgumentSyntax> functionNameAttributes = from methodDeclaration in root.DescendantNodes().OfType<MethodDeclarationSyntax>()
                                          where methodDeclaration.AttributeLists.Count > 0
                                          where methodDeclaration.AttributeLists.Where(a => a.Attributes.Where(attr => attr.Name.ToString().Contains(functionAttributeText)).Count() == 1).Count() == 1
                                          select methodDeclaration.AttributeLists.Select(a => a.Attributes.Where(attr => attr.Name.ToString().Contains(functionAttributeText)).First().ArgumentList.Arguments.First()).First();

                // remove quotes from around the names
                var aFNames = functionNameAttributes.Select(ab => ab.ToString().Substring(1, ab.ToString().Length - 2)).ToArray();

                return new GetAzureFunctionsResult()
                {
                    Success = true,
                    azureFunctions = aFNames
                };
            }
            catch (Exception e)
            {
                return new GetAzureFunctionsResult()
                {
                    Success = false,
                    ErrorMessage = e.ToString()
                };
            }
        }
    }
}