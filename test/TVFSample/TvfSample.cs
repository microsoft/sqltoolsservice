using System.Collections;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Threading;

public class TvfSample
{
    private struct ReturnValues
    {
        public int Value;
    }

    private static void FillValues(object obj, out SqlInt32 theValue)
    {
        ReturnValues returnValues = (ReturnValues)obj;
        theValue = returnValues.Value;
    }


    [SqlFunction(DataAccess = DataAccessKind.None,
        IsDeterministic = true, IsPrecise = true,
        SystemDataAccess = SystemDataAccessKind.None,
        FillRowMethodName = "FillValues", TableDefinition = "IntValue INT")]
    public static IEnumerable TVF_Streaming(SqlInt32 maxValue, SqlInt32 delayInMilliseconds)
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
            Thread.Sleep((int) delayInMilliseconds); 
            yield return values; // return row per each iteration
        }

        // we do not need to return everything at once
    }
}
