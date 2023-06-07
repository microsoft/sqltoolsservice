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
        private ILogger<ArtistsApi> _logger;

        /// <summary> Initializes a new instance of ArtistsApi. </summary>
        /// <param name="logger"> Class logger. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="logger"/> is null. </exception>
        public ArtistsApi(ILogger<ArtistsApi> logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger;
        }

        /// <summary> Returns a list of artists. </summary>
        /// <param name="req"> Raw HTTP Request. </param>
        /// <param name="cancellationToken"> The cancellation token provided on Function shutdown. </param>
        [FunctionName("GetArtists_get")]
        public IActionResult GetArtists([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "artists")] HttpRequest req)
        {
            _logger.LogInformation("HTTP trigger function processed a request.");
            // TODO: Handle Documented Responses.
            // Spec Defines: HTTP 200
            // Spec Defines: HTTP 400
            throw new NotImplementedException();
        }

        /// <summary> Lets a user post a new artist. </summary>
        /// <param name="body"> The Artist to use. </param>
        /// <param name="req"> Raw HTTP Request. </param>
        /// <param name="cancellationToken"> The cancellation token provided on Function shutdown. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="body"/> is null. </exception>
        [FunctionName("NewArtist_post")]
        public IActionResult NewArtist([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "artists")] Artist body, HttpRequest req)
        {
            _logger.LogInformation("HTTP trigger function processed a request.");
            // TODO: Handle Documented Responses.
            // Spec Defines: HTTP 200
            // Spec Defines: HTTP 400
            throw new NotImplementedException();
        }

        /// <summary> Lets a user post new artists. </summary>
        /// <param name="body"> The Artists to use. </param>
        /// <param name="req"> Raw HTTP Request. </param>
        /// <param name="cancellationToken"> The cancellation token provided on Function shutdown. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="body"/> is null. </exception>
        [FunctionName("NewArtists_post")]
        public async IActionResult NewArtists([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "artists")] Artist body, HttpRequest req)
        {
            _logger.LogInformation("HTTP trigger function processed a request.");
            // TODO: Handle Documented Responses.
            // Spec Defines: HTTP 200
            // Spec Defines: HTTP 400
            throw new NotImplementedException();
        }
    }
}
