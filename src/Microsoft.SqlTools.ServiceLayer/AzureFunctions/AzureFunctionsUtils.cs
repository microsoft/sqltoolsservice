//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.SqlTools.ServiceLayer.AzureFunctions
{
    internal static class AzureFunctionsUtils
    {
        private const string FUNCTION_NAME_ATTRIBUTE_NAME = "FunctionName";
        private const string NET5_FUNCTION_ATTRIBUTE_NAME = "Function";
        private const string HTTP_TRIGGER_ATTRIBUTE_NAME = "HttpTrigger";
        private const string ROUTE_ARGUMENT_NAME = "Route";

        /// <summary>
        /// Gets all the methods in the syntax tree with an Azure Function attribute
        /// </summary>
        public static IEnumerable<MethodDeclarationSyntax> GetMethodsWithFunctionAttributes(CompilationUnitSyntax root)
        {
            // Look for Azure Functions in the file
            // Get all method declarations
            IEnumerable<MethodDeclarationSyntax> methodDeclarations = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

            // .NET 5 is not currently supported for sql bindings, so an error should be returned if this file has .NET 5 style Azure Functions
            if (HasNet5StyleAzureFunctions(methodDeclarations))
            {
                throw new Exception(SR.SqlBindingsNet5NotSupported);
            }

            // get all the method declarations with the FunctionName attribute
            IEnumerable<MethodDeclarationSyntax> methodsWithFunctionAttributes = methodDeclarations.Where(md => md.AttributeLists.Where(a => a.Attributes.Where(attr => attr.Name.ToString().Equals(FUNCTION_NAME_ATTRIBUTE_NAME)).Any()).Any());

            return methodsWithFunctionAttributes;
        }

        /// <summary>
        /// Gets the route from an HttpTrigger attribute if specified
        /// </summary>
        /// <param name="m">The method</param>
        /// <returns>The name of the route, or null if no route is specified (or there isn't an HttpTrigger binding)</returns>
        public static string? GetHttpRoute(this MethodDeclarationSyntax m)
        {
            return m
                .ParameterList
                .Parameters // Get all the parameters for the method
                .SelectMany(p =>
                    p.AttributeLists // Get a list of all attributes on any of the parameters
                    .SelectMany(al => al.Attributes)
                ).Where(a => a.Name.ToString().Equals(HTTP_TRIGGER_ATTRIBUTE_NAME) // Find any HttpTrigger attributes
                ).FirstOrDefault() // Get the first one available - there should only ever be 0 or 1
                ?.ArgumentList // Get all the arguments for the attribute
                ?.Arguments
                .Where(a => a.ChildNodes().OfType<NameEqualsSyntax>().Where(nes => nes.Name.ToString().Equals(ROUTE_ARGUMENT_NAME)).Any()) // Find the Route argument - it should always be a named argument
                .FirstOrDefault()
                ?.ChildNodes()
                .OfType<ExpressionSyntax>() // Find the child identifier node with our value
                .Where(le => le.Kind() != SyntaxKind.NullLiteralExpression) // Skip the null expressions so they aren't ToString()'d into "null"
                .FirstOrDefault()
                ?.ToString()
                .TrimStart('$') // Remove $ from interpolated string, since this will always be outside the quotes we can just trim
                .Trim('\"'); // Trim off the quotes from the string value - additional quotes at the beginning and end aren't valid syntax so it's fine to trim
        }

        /// <summary>
        /// Gets the function name from the FunctionName attribute on a method
        /// </summary>
        /// <param name="m">The method</param>
        /// <returns>The function name, or an empty string if the name attribute doesn't exist</returns>
        public static string GetFunctionName(this MethodDeclarationSyntax m)
        {
            // Note that we return an empty string as the default because a null name isn't valid - every function
            // should have a name. So we should never actually hit that scenario, but just to be safe we return the
            // empty string as the default in case we hit some unexpected edge case. 
            return m
                .AttributeLists // Get all the attribute lists on the method
                .Select(a =>
                    a.Attributes.Where(
                        attr => attr.Name.ToString().Equals(AzureFunctionsUtils.FUNCTION_NAME_ATTRIBUTE_NAME) // Find any FunctionName attributes
                    ).FirstOrDefault() // Get the first one available - there should only ever be 0 or 1
                ).Where(a => // Filter out any that didn't have a FunctionName attribute
                    a != null
                ).FirstOrDefault() // Get the first one available - there should only ever be 0 or 1
                ?.ArgumentList // Get all the arguments for the attribute
                ?.Arguments
                .FirstOrDefault() // The first argument is the function name
                ?.ToString()
                .TrimStart('$') // Remove $ from interpolated string, since this will always be outside the quotes we can just trim
                .Trim('\"') ?? ""; // Trim off the quotes from the string value - additional quotes at the beginning and end aren't valid syntax so it's fine to trim
        }

        /// <summary>
        /// Checks if any of the method declarations have .NET 5 style Azure Function attributes
        /// .NET 5 AFs use the Function attribute, while .NET 3.1 AFs use FunctionName attribute
        /// </summary>
        public static bool HasNet5StyleAzureFunctions(IEnumerable<MethodDeclarationSyntax> methodDeclarations)
        {
            // get all the method declarations with the Function attribute
            return methodDeclarations.Any(md => md.AttributeLists.Any(al => al.Attributes.Any(attr => attr.Name.ToString().Equals(NET5_FUNCTION_ATTRIBUTE_NAME))));
        }
    }
}
