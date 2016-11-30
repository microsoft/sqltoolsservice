using System;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.SmoSdkSamples
{
    // This application demonstrates how to script out a sample database without dependencies and iterate through the list to display the results.
    public class Program
    {
        public static void Main(string[] args)
        {
            // Connect to the local, default instance of SQL Server.
            Smo.Server srv = new Smo.Server();
	        // database name
	    	Console.WriteLine("Enter database name for scripting:"); 
            string dbName = Console.ReadLine(); 
            // Reference the database.    
            Database db = srv.Databases[dbName];
            // Define a Scripter object and set the required scripting options.   
            Scripter scripter = new Scripter(srv);
            scripter.Options.ScriptDrops = false;
            // To include indexes  
            scripter.Options.Indexes = true;
            // to include referential constraints in the script 
            scripter.Options.DriAllConstraints = true;    
            // Iterate through the tables in database and script each one. Display the script.     
            foreach (Table tb in db.Tables)
            {
                // check if the table is not a system table  
                if (tb.IsSystemObject == false)
                {
                    Console.WriteLine("-- Scripting for table " + tb.Name);
                    // Generating script for table tb  
                    System.Collections.Specialized.StringCollection sc = scripter.Script(new Urn[] { tb.Urn });
                    foreach (string st in sc)
                    {
                        Console.WriteLine(st);
                    }
                    Console.WriteLine("--");
                }
            }

        }
    }
}