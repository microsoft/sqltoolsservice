using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using System;
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
        Task<CompletionItem[]> HandleCompletionAsync(ConnectionInfo connInfo, ScriptDocumentInfo scriptDocumentInfo, CompletionItem[] completions, CancellationToken token);
    }
}
