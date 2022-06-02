using System;
using System.Threading;
using System.Threading.Tasks;
using Company.Namespace.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Company.Namespace
{
    public class AzureFunctionsRoute
    {
        /// <summary>
        /// Tests binding with a route specified
        /// </summary>
        [FunctionName("WithRoute")]
        public IActionResult WithRoute([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "withRoute")] HttpRequest req, [Sql("select * from [dbo].[table1]", CommandType = System.Data.CommandType.Text, ConnectionStringSetting = "SqlConnectionString")] IEnumerable<Object> result)
        {
            throw new NotImplementedException();
        }

        private const string interpolated = "interpolated";
        /// <summary>
        /// Tests binding with a route specified using an interpolated string
        /// </summary>
        [FunctionName("InterpolatedString")]
        public IActionResult InterpolatedString([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{interpolated}String")] HttpRequest req, [Sql("select * from [dbo].[table1]", CommandType = System.Data.CommandType.Text, ConnectionStringSetting = "SqlConnectionString")] IEnumerable<Object> result)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tests binding with a route specified that has $'s on the beginning and end
        /// </summary>
        [FunctionName("WithDollarSigns")]
        public IActionResult WithDollarSigns([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "$withDollarSigns$")] HttpRequest req, [Sql("select * from [dbo].[table1]", CommandType = System.Data.CommandType.Text, ConnectionStringSetting = "SqlConnectionString")] IEnumerable<Object> result)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tests binding with a route specified and no spaces between tokens
        /// </summary>
        [FunctionName("WithRouteNoSpaces")]
        public IActionResult WithRouteNoSpaces([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route="withRouteNoSpaces")] HttpRequest req, [Sql("select * from [dbo].[table1]", CommandType = System.Data.CommandType.Text, ConnectionStringSetting = "SqlConnectionString")] IEnumerable<Object> result)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tests binding with a route specified and no spaces between tokens
        /// </summary>
        [FunctionName("WithRouteExtraSpaces")]
        public IActionResult WithRouteExtraSpaces([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route    =    "withRouteExtraSpaces")] HttpRequest req, [Sql("select * from [dbo].[table1]", CommandType = System.Data.CommandType.Text, ConnectionStringSetting = "SqlConnectionString")] IEnumerable<Object> result)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tests binding with a null route specified
        /// </summary>
        [FunctionName("WithNullRoute")]
        public IActionResult WithNullRoute([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, [Sql("select * from [dbo].[table1]", CommandType = System.Data.CommandType.Text, ConnectionStringSetting = "SqlConnectionString")] IEnumerable<Object> result)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tests binding with a null route specified
        /// </summary>
        [FunctionName("NoRoute")]
        public IActionResult NoRoute([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req, [Sql("select * from [dbo].[table1]", CommandType = System.Data.CommandType.Text, ConnectionStringSetting = "SqlConnectionString")] IEnumerable<Object> result)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tests binding with an empty route specified
        /// </summary>
        [FunctionName("EmptyRoute")]
        public IActionResult EmptyRoute([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "")] HttpRequest req, [Sql("select * from [dbo].[table1]", CommandType = System.Data.CommandType.Text, ConnectionStringSetting = "SqlConnectionString")] IEnumerable<Object> result)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tests binding without an HttpBinding
        /// </summary>
        [FunctionName("NoHttpBinding")]
        public IActionResult WithRoute([Sql("select * from [dbo].[table1]", CommandType = System.Data.CommandType.Text, ConnectionStringSetting = "SqlConnectionString")] IEnumerable<Object> result)
        {
            throw new NotImplementedException();
        }
    }
}
