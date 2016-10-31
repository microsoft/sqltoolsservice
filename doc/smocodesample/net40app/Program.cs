using System;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace SMOSample
{
    // This application demostrates how to script out a sample database without dependencies and iterate through the list to display the results.
    public class Program
    {
        public static void Main(string[] args)
        {
            //Connect to the local, default instance of SQL Server.
            Server srv = new Server();
	    // database name 
            string dbName = "AdventureWorks2012"; 
            // Reference the database.    
            Database db = srv.Databases[dbName];

            // Define a Scripter object and set the required scripting options.   
            Scripter scrp = new Scripter(srv);
            scrp.Options.ScriptDrops = false;
            scrp.Options.Indexes = true;   // To include indexes  
            scrp.Options.DriAllConstraints = true;   // to include referential constraints in the script  

            // Iterate through the tables in database and script each one. Display the script.     
            foreach (Table tb in db.Tables)
            {
                // check if the table is not a system table  
                if (tb.IsSystemObject == false)
                {
                    Console.WriteLine("-- Scripting for table " + tb.Name);

                    // Generating script for table tb  
                    System.Collections.Specialized.StringCollection sc = scrp.Script(new Urn[] { tb.Urn });
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