using System;
using System.Text;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common.Extensions
{
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Builds a string containing the exception messages and all messages of child InnerExceptions.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static string BuildRecursiveErrorMessage(this Exception e)
        {
            var msg = new StringBuilder();
            while (e != null)
            {
                msg.AppendLine(e.Message);
                e = e.InnerException;
            }

            return msg.ToString();
        }
    }
}
