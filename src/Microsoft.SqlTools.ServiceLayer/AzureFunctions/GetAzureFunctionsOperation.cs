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
        const string functionAttributeText = "FunctionName";
        const string net5FunctionAttributeText = "Function";

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

                // Look for Azure Functions in the file
                // Get all method declarations
                IEnumerable<MethodDeclarationSyntax> methodDeclarations = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

                // .NET 5 is not currently supported for sql bindings, so an error should be returned if this file has .NET 5 style Azure Functions
                if (this.HasNet5StyleAzureFunctions(methodDeclarations))
                {
                    throw new Exception(SR.SqlBindingsNet5NotSupported);
                }

                // get all the method declarations with the FunctionName attribute
                IEnumerable<MethodDeclarationSyntax> methodsWithFunctionAttributes = methodDeclarations.Where(md => md.AttributeLists.Count > 0).Where(md => md.AttributeLists.Where(a => a.Attributes.Where(attr => attr.Name.ToString().Contains(functionAttributeText)).Count() == 1).Count() == 1);

                // Get FunctionName attributes
                IEnumerable<AttributeSyntax> functionNameAttributes = methodsWithFunctionAttributes.Select(md => md.AttributeLists.Select(a => a.Attributes.Where(attr => attr.Name.ToString().Contains(functionAttributeText)).First()).First());

                // Get the function names in the FunctionName attributes
                IEnumerable<AttributeArgumentSyntax> nameArgs = functionNameAttributes.Select(a => a.ArgumentList.Arguments.First());

                // Remove quotes from around the names
                string[] aFNames = nameArgs.Select(ab => ab.ToString().Trim('\"')).ToArray();

                return new GetAzureFunctionsResult()
                {
                    Success = true,
                    azureFunctions = aFNames
                };
            }
            catch (Exception ex)
            {
                Logger.Write(TraceEventType.Information, $"Failed to get Azure functions. Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Checks if any of the method declarations have .NET 5 style Azure Function attributes
        /// .NET 5 AFs use the Function attribute, while .NET 3.1 AFs use FunctionName attritube
        /// </summary>
        bool HasNet5StyleAzureFunctions(IEnumerable<MethodDeclarationSyntax> methodDeclarations)
        {
            // get all the method declarations with the Function attribute
            IEnumerable<MethodDeclarationSyntax> methodsWithFunctionAttributes = methodDeclarations.Where(md => md.AttributeLists.Count > 0).Where(md => md.AttributeLists.Where(a => a.Attributes.Where(attr => attr.Name.ToString().Equals(net5FunctionAttributeText)).Count() == 1).Count() == 1);
            return methodsWithFunctionAttributes.Any();
        }
    }
}