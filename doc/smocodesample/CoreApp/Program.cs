using Microsoft.Data.Tools.DataSets;
using Microsoft.SqlServer.Management.Smo;
using System;

namespace SMOSample
{
    // This application demonstrates iterations through the rows and display collation details for default instance of SQL Server.
    public class Program
    {
        public static void Main(string[] args)
        {
            //Connect to the local, default instance of SQL Server.
            Server srv = new Server();
            //Call the EnumCollations method and return collation information to DataTable variable.   
            DataTable d = srv.EnumCollations();
            //Select the returned data into an array of DataRow, then iterate through the rows and display collation details for the instance of SQL Server.   
            foreach (DataRow r in d.Rows)
            {
                Console.WriteLine("=========");
                foreach (DataColumn c in r.Table.Columns)
                {
                    Console.WriteLine(c.ColumnName + " = " + r[c].ToString());
                }
            }
        }
    }
}