//------------------------------------------------------------------------------
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------------------------

using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.SqlTools.EditorServices.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection
{

    /// <summary>
    /// Wraps <see cref="IDbCommand"/> objects that could be a <see cref="SqlCommand"/> or
    /// a <see cref="ReliableSqlConnection.ReliableSqlCommand"/>, providing common methods across both.
    /// </summary>
    internal sealed class DbCommandWrapper
    {
        private readonly IDbCommand _command;
        private readonly bool _isReliableCommand;

        public DbCommandWrapper(IDbCommand command)
        {
            Validate.IsNotNull(nameof(command), command);
            if (command is ReliableSqlConnection.ReliableSqlCommand)
            {
                _isReliableCommand = true;
            }
            else if (!(command is SqlCommand))
            {
                throw new InvalidOperationException(Resources.InvalidCommandType);
            }
            _command = command;
        }

        public static bool IsSupportedCommand(IDbCommand command)
        {
            return command is ReliableSqlConnection.ReliableSqlCommand
                || command is SqlCommand;
        }


        public event StatementCompletedEventHandler StatementCompleted
        {
            add
            {
                SqlCommand sqlCommand = GetAsSqlCommand();
                sqlCommand.StatementCompleted += value;
            }
            remove
            {
                SqlCommand sqlCommand = GetAsSqlCommand();
                sqlCommand.StatementCompleted -= value;
            }
        }

        /// <summary>
        /// Gets this as a SqlCommand by casting (if we know it is actually a SqlCommand)
        /// or by getting the underlying command (if it's a ReliableSqlCommand)
        /// </summary>
        private SqlCommand GetAsSqlCommand()
        {
            if (_isReliableCommand)
            {
                return ((ReliableSqlConnection.ReliableSqlCommand) _command).GetUnderlyingCommand();
            }
            return (SqlCommand) _command;
        }
    }
}