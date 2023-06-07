using System;
using System.Threading;
using System.Threading.Tasks;
using Company.Namespace.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Company.Namespace
{
    public class ArtistsApi
    {
        /// <summary>
        /// Function with basic name
        /// </summary>
        [FunctionName("WithName")]
        public IActionResult WithName([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "artists")] HttpRequest req)
        {
            throw new NotImplementedException();
        }

        private const string interpolated = "interpolated";

        /// <summary>
        /// Function with interpolated string as name
        /// </summary>
        [FunctionName($"{interpolated}String")]
        public async IActionResult InterpolatedString([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "artists")] Artist body, HttpRequest req)
        {
            throw new NotImplementedException();
        }
    }
}
