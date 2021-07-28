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
        /// Finds all the azure functions in the given file
        /// </summary>
        /// <returns>returns the result with the names of the azure functions in the file</returns>
        public GetAzureFunctionsResult GetAzureFunctions()
        {
            try
            {
                string text = File.ReadAllText(Parameters.filePath);

                SyntaxTree tree = CSharpSyntaxTree.ParseText(text);
                CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

                // look for Azure Function in the file
                var methodWithFunctions = from methodDeclaration in root.DescendantNodes().OfType<MethodDeclarationSyntax>()
                                          where methodDeclaration.AttributeLists.Count > 0
                                          where methodDeclaration.AttributeLists.Where(a => a.Attributes.Where(attr => attr.Name.ToString().Contains(functionAttributeText)).Count() == 1).Count() == 1
                                          select methodDeclaration.AttributeLists.Select(a => a.Attributes.Where(attr => attr.Name.ToString().Contains(functionAttributeText)).First().ArgumentList.Arguments.First());

                // remove quotes from names
                var aFNames = methodWithFunctions.Select(a => a.Select(ab => ab.ToString().Substring(1, ab.ToString().Length-2)).First()).ToArray();

                return new GetAzureFunctionsResult()
                {
                    Success = true,
                    azureFunctions = aFNames
                };
            }
            catch(Exception e)
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

