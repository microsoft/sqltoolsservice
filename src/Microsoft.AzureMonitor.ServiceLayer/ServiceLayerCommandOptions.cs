using System.Globalization;
using Microsoft.SqlTools.Hosting.Localization;
using Microsoft.SqlTools.Hosting.Utility;

namespace Microsoft.AzureMonitor.ServiceLayer
{
    public class ServiceLayerCommandOptions : CommandOptions
    {
        private const string ServiceLayerServiceName = "MicrosoftAzureMonitorServiceLayer.exe";

        public ServiceLayerCommandOptions(string[] args) : base(args, ServiceLayerServiceName)
        {
        }

        public override void SetLocale(string locale)
        {
            try
            {
                LocaleSetter(locale);

                // Setting our internal SR culture to our global culture
                sr.Culture = CultureInfo.CurrentCulture;
            }
            catch (CultureNotFoundException)
            {
                // Ignore CultureNotFoundException since it only is thrown before Windows 10.  Windows 10,
                // along with macOS and Linux, pick up the default culture if an invalid locale is passed
                // into the CultureInfo constructor.
            }
        }
    }
}