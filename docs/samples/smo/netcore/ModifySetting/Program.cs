
using Microsoft.SqlServer.Management.Smo;
using System;

namespace Microsoft.SqlServer.Management.SmoSdkSamples
{
    // This example displays information about the instance of SQL Server in Information and Settings, and modifies settings in Settings and UserOptionsobject properties.
    public class Program
    {
        public static void Main(string[] args)
        {
                //Connect to the local, default instance of SQL Server.
                Microsoft.SqlServer.Management.Smo.Server srv = new Microsoft.SqlServer.Management.Smo.Server();
                //Display all the configuration options.   
                foreach (ConfigProperty p in srv.Configuration.Properties)
                {
                    Console.WriteLine(p.DisplayName);
                }
                Console.WriteLine("There are " + srv.Configuration.Properties.Count.ToString() + " configuration options.");
                //Display the maximum and minimum values for ShowAdvancedOptions.                   
                int min = srv.Configuration.ShowAdvancedOptions.Minimum;
                int max = srv.Configuration.ShowAdvancedOptions.Maximum;
                Console.WriteLine("Minimum and Maximum values are " + min + " and " + max + ".");
                int configvalue = srv.Configuration.ShowAdvancedOptions.ConfigValue;
                //Modify the value of ShowAdvancedOptions and run the Alter method.   
                srv.Configuration.ShowAdvancedOptions.ConfigValue = 0;
                srv.Configuration.Alter();
                //Display when the change takes place according to the IsDynamic property.   
                if (srv.Configuration.ShowAdvancedOptions.IsDynamic == true)
                {
                    Console.WriteLine("Configuration option has been updated.");
                }
                else
                {
                    Console.WriteLine("Configuration option will be updated when SQL Server is restarted.");
                }
                // Recover setting value
                srv.Configuration.ShowAdvancedOptions.ConfigValue = configvalue;
                srv.Configuration.Alter();
        }
    }
}