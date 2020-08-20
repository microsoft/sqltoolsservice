using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;

namespace Microsoft.Kusto.ServiceLayer.LanguageServices
{
    public interface IAutoCompleteHelper
    {
        /// <summary>
        /// Create a completion item from the default item text since VS Code expects CompletionItems
        /// </summary>
        /// <param name="label"></param>
        /// <param name="detail"></param>
        /// <param name="insertText"></param>
        /// <param name="kind"></param>
        /// <param name="row"></param>
        /// <param name="startColumn"></param>
        /// <param name="endColumn"></param>
        CompletionItem CreateCompletionItem(
            string label,
            string detail,
            string insertText,
            CompletionItemKind kind,
            int row,
            int startColumn,
            int endColumn);

        /// <summary>
        /// Converts QuickInfo object into a VS Code Hover object
        /// </summary>
        /// <param name="quickInfo"></param>
        /// <param name="language"></param>
        /// <param name="row"></param>
        /// <param name="startColumn"></param>
        /// <param name="endColumn"></param>
        Hover ConvertQuickInfoToHover(
            string quickInfoText,
            string language,
            int row,
            int startColumn,
            int endColumn);
    }
}