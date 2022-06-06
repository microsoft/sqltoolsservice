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
        /// Tests binding with a single operation and a route specified
        /// </summary>
        [FunctionName("SingleWithRoute")]
        public IActionResult SingleWithRoute([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "route")] HttpRequest req, [Sql("select * from [dbo].[table1]", CommandType = System.Data.CommandType.Text, ConnectionStringSetting = "SqlConnectionString")] IEnumerable<Object> result)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tests binding with multiple operations and a route specified
        /// </summary>
        [FunctionName("MultipleWithRoute")]
        public IActionResult MultipleWithRoute([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "route")] HttpRequest req, [Sql("select * from [dbo].[table1]", CommandType = System.Data.CommandType.Text, ConnectionStringSetting = "SqlConnectionString")] IEnumerable<Object> result)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tests binding with no operations and a route specified
        /// </summary>
        [FunctionName("NoOperationsWithRoute")]
        public IActionResult MultipleWithRoute([HttpTrigger(AuthorizationLevel.Anonymous, Route = "route")] HttpRequest req, [Sql("select * from [dbo].[table1]", CommandType = System.Data.CommandType.Text, ConnectionStringSetting = "SqlConnectionString")] IEnumerable<Object> result)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tests binding with no operations and no route
        /// </summary>
        [FunctionName("NoOperationsNoRoute")]
        public IActionResult MultipleWithRoute([HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req, [Sql("select * from [dbo].[table1]", CommandType = System.Data.CommandType.Text, ConnectionStringSetting = "SqlConnectionString")] IEnumerable<Object> result)
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
