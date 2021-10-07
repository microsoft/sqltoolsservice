using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.AzureFunctions
{
    internal static class AzureFunctionsUtils
    {
        public const string functionAttributeText = "FunctionName";
        public const string net5FunctionAttributeText = "Function";

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
            IEnumerable<MethodDeclarationSyntax> methodsWithFunctionAttributes = methodDeclarations.Where(md => md.AttributeLists.Where(a => a.Attributes.Where(attr => attr.Name.ToString().Equals(functionAttributeText)).Any()).Any());

            return methodsWithFunctionAttributes;
        }

        /// <summary>
        /// Checks if any of the method declarations have .NET 5 style Azure Function attributes
        /// .NET 5 AFs use the Function attribute, while .NET 3.1 AFs use FunctionName attritube
        /// </summary>
        public static bool HasNet5StyleAzureFunctions(IEnumerable<MethodDeclarationSyntax> methodDeclarations)
        {
            // get all the method declarations with the Function attribute
            return methodDeclarations.Any(md => md.AttributeLists.Any(al => al.Attributes.Any(attr => attr.Name.ToString().Equals(net5FunctionAttributeText))));
        }
    }
}
