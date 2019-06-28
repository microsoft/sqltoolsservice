using Microsoft.SqlTools.ServiceLayer.Connection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion.Extension
{
    public interface ICompletionExtension : IDisposable
    {
        string Name { get; }

        //For extension initialization, TODO: pass in a logger
        Task Initialize(CancellationToken token);

        //Implement the actual completion extension logic
        Task HandleCompletionAsync(ConnectionInfo connInfo, ScriptDocumentInfo scriptDocumentInfo, AutoCompletionResult completions, CancellationToken token);
    }
}
