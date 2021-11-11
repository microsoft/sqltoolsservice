//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.SqlTools.ServiceLayer.AzureFunctions.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.SqlTools.ServiceLayer.AzureFunctions
{
    /// <summary>
    /// Class to represent inserting a sql binding into an Azure Function
    /// </summary>
    class AddSqlBindingOperation
    {
        public const string GenericClass = "System.Collections.Generic";

        public AddSqlBindingParams Parameters { get; }

        public AddSqlBindingOperation(AddSqlBindingParams parameters)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
        }

        public ResultStatus AddBinding()
        {
            try
            {
                string text = File.ReadAllText(Parameters.filePath);

                SyntaxTree tree = CSharpSyntaxTree.ParseText(text);
                CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

                // look for Azure Function to update
                IEnumerable<MethodDeclarationSyntax> azureFunctionMethods = AzureFunctionsUtils.GetMethodsWithFunctionAttributes(root);
                IEnumerable<MethodDeclarationSyntax> matchingMethods = azureFunctionMethods.Where(md => md.AttributeLists.Where(a => a.Attributes.Where(attr => attr.ArgumentList.Arguments.First().ToString().Equals($"\"{Parameters.functionName}\"")).Any()).Any());

                if (matchingMethods.Count() == 0)
                {
                    return new ResultStatus()
                    {
                        Success = false,
                        ErrorMessage = SR.CouldntFindAzureFunction(Parameters.functionName, Parameters.filePath)
                    };
                }
                else if (matchingMethods.Count() > 1)
                {
                    return new ResultStatus()
                    {
                        Success = false,
                        ErrorMessage = SR.MoreThanOneAzureFunctionWithName(Parameters.functionName, Parameters.filePath)
                    };
                }

                MethodDeclarationSyntax azureFunction = matchingMethods.First();
                var newParam = this.Parameters.bindingType == BindingType.input ? this.GenerateInputBinding() : this.GenerateOutputBinding();

                // Generate updated method with the new parameter
                // normalizewhitespace gets rid of any newline whitespace in the leading trivia, so we add that back
                var updatedMethod = azureFunction.AddParameterListParameters(newParam).NormalizeWhitespace().WithLeadingTrivia(azureFunction.GetLeadingTrivia()).WithTrailingTrivia(azureFunction.GetTrailingTrivia());

                // Replace the node in the tree
                root = root.ReplaceNode(azureFunction, updatedMethod);

                // Check if file has System.Collections.Generic reference, insert it if not
                IEnumerable<UsingDirectiveSyntax> usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>();
                var genericUsingDirective = usingDirectives.Where(usingDirective => usingDirective.Name.ToString() == GenericClass);
                if (genericUsingDirective.Count() == 0)
                {
                    root = root.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(GenericClass)).NormalizeWhitespace().WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed));
                }

                // write updated tree to file
                var workspace = new AdhocWorkspace();
                var syntaxTree = CSharpSyntaxTree.ParseText(root.ToString());
                var formattedNode = CodeAnalysis.Formatting.Formatter.Format(syntaxTree.GetRoot(), workspace);
                StringBuilder sb = new StringBuilder(formattedNode.ToString());
                string content = sb.ToString();
                File.WriteAllText(Parameters.filePath, content);

                return new ResultStatus()
                {
                    Success = true
                };
            }
            catch (Exception ex)
            {
                Logger.Write(TraceEventType.Information, $"Failed to add sql binding. Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Generates a parameter for the sql input binding that looks like
        /// [Sql("select * from [dbo].[table1]", CommandType = System.Data.CommandType.Text, ConnectionStringSetting = "SqlConnectionString")] IEnumerable<Object> result
        /// </summary>
        private ParameterSyntax GenerateInputBinding()
        {
            // Create arguments for the Sql Input Binding attribute
            var argumentList = SyntaxFactory.AttributeArgumentList();
            argumentList = argumentList.AddArguments(SyntaxFactory.AttributeArgument(SyntaxFactory.IdentifierName($"\"select * from {Parameters.objectName}\"")));
            argumentList = argumentList.AddArguments(SyntaxFactory.AttributeArgument(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName("CommandType"), SyntaxFactory.IdentifierName("System.Data.CommandType.Text"))));
            argumentList = argumentList.AddArguments(SyntaxFactory.AttributeArgument(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName("ConnectionStringSetting"), SyntaxFactory.IdentifierName($"\"{Parameters.connectionStringSetting}\""))));

            // Create Sql Binding attribute
            SyntaxList<AttributeListSyntax> attributesList = new SyntaxList<AttributeListSyntax>();
            attributesList = attributesList.Add(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Sql")).WithArgumentList(argumentList))));

            // Create new parameter
            ParameterSyntax newParam = SyntaxFactory.Parameter(attributesList, new SyntaxTokenList(), SyntaxFactory.ParseTypeName("IEnumerable<Object>"), SyntaxFactory.Identifier("result"), null);
            return newParam;
        }

        /// <summary>
        /// Generates a parameter for the sql output binding that looks like
        /// [Sql("[dbo].[table1]", ConnectionStringSetting = "SqlConnectionString")] out Object output
        /// </summary>
        private ParameterSyntax GenerateOutputBinding()
        {
            // Create arguments for the Sql Output Binding attribute
            var argumentList = SyntaxFactory.AttributeArgumentList();
            argumentList = argumentList.AddArguments(SyntaxFactory.AttributeArgument(SyntaxFactory.IdentifierName($"\"{Parameters.objectName}\"")));
            argumentList = argumentList.AddArguments(SyntaxFactory.AttributeArgument(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName("ConnectionStringSetting"), SyntaxFactory.IdentifierName($"\"{Parameters.connectionStringSetting}\""))));
            
            SyntaxList<AttributeListSyntax> attributesList = new SyntaxList<AttributeListSyntax>();
            attributesList = attributesList.Add(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Sql")).WithArgumentList(argumentList))));

            var syntaxTokenList = new SyntaxTokenList();
            syntaxTokenList = syntaxTokenList.Add(SyntaxFactory.Token(SyntaxKind.OutKeyword));

            ParameterSyntax newParam = SyntaxFactory.Parameter(attributesList, syntaxTokenList, SyntaxFactory.ParseTypeName(typeof(Object).Name), SyntaxFactory.Identifier("output"), null);
            return newParam;
        }
    }
}

