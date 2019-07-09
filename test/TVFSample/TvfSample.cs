using System;
using System.Collections;
using System.Data.SqlTypes;
using System.Text;
using Microsoft.SqlServer.Server;
using System.Threading;
/// <summary>
/// TVF for clr stored procedure with following definition:
///
/// use master;  
///    -- Replace SQL_Server_logon with your SQL Server user credentials.
///    GRANT EXTERNAL ACCESS ASSEMBLY TO [redmond\arvran];   
///-- Modify the following line to specify a different database.
///ALTER DATABASE master SET TRUSTWORTHY ON;  
///--
///RECONFIGURE;
///GO
///sp_configure 'clr enabled', 1;
///GO
///RECONFIGURE;
///GO
///sp_configure 'network packet size', 512;
///GO
///RECONFIGURE;
///GO

///-- Modify the next line to use the appropriate database.
///CREATE ASSEMBLY MyTVfs
///FROM 'D:\src\sqltoolsservice\test\TVFSample\bin\Release\TVFSample.dll'   
///WITH PERMISSION_SET = EXTERNAL_ACCESS;
///GO
///CREATE FUNCTION StreamingTvf(@numRows int , @delayInMs int, @messageSize int= 4000)
///RETURNS TABLE
///(rowNumber int, msg nvarchar(max))
///AS
///EXTERNAL NAME MyTVfs.TvfSample.TVF_Streaming;
///GO
/// </summary>
public class TvfSample
{
    private struct ReturnValues
    {
        public int Value;
        public string Message;
    }

    private static void FillValues(object obj, out SqlInt32 theValue, out SqlChars message)
    {
        ReturnValues returnValues = (ReturnValues)obj;
        theValue = returnValues.Value;
        message = new SqlChars(returnValues.Message);
    }

    private static string RandomString(int size)
    {
        StringBuilder builder = new StringBuilder();
        Random random = new Random();
        for (int i = 0; i < size; i++)
        {
            char ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
            builder.Append(ch);
        }
        return builder.ToString();
    }

    [SqlFunction(DataAccess = DataAccessKind.None,
        IsDeterministic = true, IsPrecise = true,
        SystemDataAccess = SystemDataAccessKind.None,
        FillRowMethodName = "FillValues", TableDefinition = "IntValue INT, Message nvarchar(max) ")]
    public static IEnumerable TVF_Streaming(SqlInt32 maxValue, SqlInt32 delayInMilliseconds, SqlInt32 messageSize)
    {
        if (maxValue.IsNull)
        {
            yield break; // return no rows
        }

        // we do not need the Generic List of <ReturnValues>
        ReturnValues values = new ReturnValues(); // each row

        for (int index = 1; index <= maxValue.Value; index++)
        {
            values.Value = index;
            values.Message = RandomString((int)messageSize);
            yield return values; // return row per each iteration
            Thread.Sleep((int)delayInMilliseconds);
        }

        // we do not need to return everything at once
    }
}
