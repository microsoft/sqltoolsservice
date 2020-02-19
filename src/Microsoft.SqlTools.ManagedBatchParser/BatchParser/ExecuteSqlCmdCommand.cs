//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    public delegate Task QeSqlCmdMessageFromAppEventHandler(object sender, CommandEventArgs args);

    public class CommandEventArgs : EventArgs
    {
        public string Message { get; set; }

        public bool StdOut { get; set; }
    }

    public class ExecuteSqlCmdCommand : SqlCmdCommand
    {
        private object stateCritSection = new object();
        private object asyncRedirCritSection = new object();
        private Process runningProcess = null;
        private AsyncRedirectedOutputState stdOutRedirState = null;
        private AsyncRedirectedOutputState stdErrorRedirState = null;
        private AsyncCallback readBufferCallback;

        public event QeSqlCmdMessageFromAppEventHandler QeSqlCmdMessageFromApp = null;

        private class AsyncRedirectedOutputState
        {
            private System.Threading.AutoResetEvent doneEvent;
            public const int ReadBufferSizeInBytes = 1024;
            private byte[] readOutputBuffer;

            private AsyncRedirectedOutputState() { }

            public AsyncRedirectedOutputState(Stream s, System.Text.Encoding encoding, bool stdOut)
            {
                this.Stream = s;
                this.Encoding = encoding;
                this.StdOut = stdOut;

                doneEvent = new System.Threading.AutoResetEvent(false);
                readOutputBuffer = new byte[ReadBufferSizeInBytes];
            }

            public Stream Stream;
            /// <summary>
            /// whether it is stdout redirection or stderror
            /// </summary>
            public bool StdOut;

            public System.Text.Encoding Encoding;

            public System.Threading.AutoResetEvent Event
            {
                get
                {
                    return this.doneEvent;
                }
            }

            public byte[] ReadOutputBuffer
            {
                get
                {
                    return this.readOutputBuffer;
                }
            }
        }

        public ExecuteSqlCmdCommand(string command, string commandArgs) : base(LexerTokenType.Execute)
        {
            if (string.IsNullOrEmpty(command))
            {
                throw new ArgumentNullException("command");
            }
            Command = command;
            CommandArgs = commandArgs;
        }

        public string Command { get; private set; }
        public string CommandArgs { get; private set; }

        /// <summary>
        /// executes given command. The command is supposed to contain application name 
        /// along with command line params
        /// </summary>
        /// <param name="token"> cancellation token from query service</param>
        public void ExecuteACommand(CancellationToken token)
        {
            System.Diagnostics.Process pr = null;
            try
            {
                string cmdFullPath = string.Empty;
                string cmdParams = string.Empty;
                pr = new System.Diagnostics.Process();

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    //we'll use windows shell command processer ("cmd /c <command>") for execution
                    cmdFullPath = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}\\cmd.exe", Environment.SystemDirectory);
                    cmdParams = string.Format(System.Globalization.CultureInfo.InvariantCulture, "/C {0}", this.Command);
                }
                else
                {
                    // For now we will throw error
                    throw new SqlCmdException("Execute is only supported on Windows currently");

                    //TODO - enable and validate following
                    //cmdFullPath = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}/bin/bash", Environment.GetEnvironmentVariable("HOME"));
                    //this.Command = this.Command.Replace("\"", "\\\"");
                    //cmdParams = string.Format(System.Globalization.CultureInfo.InvariantCulture, "-c {0}", this.Command);
                }

                if (this.CommandArgs != null && this.CommandArgs.Length > 0)
                {
                    cmdParams += string.Format(System.Globalization.CultureInfo.InvariantCulture, " {0}", this.CommandArgs, System.Globalization.CultureInfo.InvariantCulture);
                }

                ProcessStartInfo si = new ProcessStartInfo(cmdFullPath, cmdParams);
                si.CreateNoWindow = true;
                si.UseShellExecute = false; //NOTE: UseShellExecute MUST be false for redirection to work
                si.RedirectStandardOutput = true;
                si.RedirectStandardInput = true;
                si.RedirectStandardError = true;
                pr.StartInfo = si;
                pr.Start();

                lock (this.stateCritSection)
                {
                    //see if cancel op has been requested by user
                    if (token.IsCancellationRequested)
                    {
                        pr.Kill();
                        pr.Dispose();
                        pr = null;
                        return;
                    }

                    //store in class var in case user decides to cancel later
                    this.runningProcess = pr;
                    pr = null;
                }

                //
                // setup stdout and stderror redirection.
                // we're using the same async callback for both redir categories
                if (this.readBufferCallback == null)
                {
                    this.readBufferCallback = new AsyncCallback(RedirectOutputCallback);
                }

                //setup both states
                Stream stdOutStream;
                Stream stdErrorStream;
                lock (this.asyncRedirCritSection)
                {
                    stdOutStream = this.runningProcess.StandardOutput.BaseStream;
                    this.stdOutRedirState = new AsyncRedirectedOutputState(stdOutStream, this.runningProcess.StandardOutput.CurrentEncoding, true);

                    stdErrorStream = this.runningProcess.StandardError.BaseStream;
                    this.stdErrorRedirState = new AsyncRedirectedOutputState(stdErrorStream, this.runningProcess.StandardError.CurrentEncoding, false);
                }

                //start reading
                stdOutStream.BeginRead(this.stdOutRedirState.ReadOutputBuffer, 0, AsyncRedirectedOutputState.ReadBufferSizeInBytes, this.readBufferCallback, this.stdOutRedirState);
                stdErrorStream.BeginRead(this.stdErrorRedirState.ReadOutputBuffer, 0, AsyncRedirectedOutputState.ReadBufferSizeInBytes, this.readBufferCallback, this.stdErrorRedirState);

                //wait till process is no longer alive
                this.runningProcess.WaitForExit();
                this.stdOutRedirState.Event.WaitOne();
                this.stdErrorRedirState.Event.WaitOne();
            }
            catch (Exception ex)
            {
                //output SqlCmdException 
                throw new SqlCmdException(string.Format("Could not process command : {0}", ex.Message));
            }
            finally
            {
                lock (stateCritSection)
                {
                    if (this.runningProcess != null)
                    {
                        this.runningProcess.Dispose();
                        this.runningProcess = null;
                    }
                }

                lock (this.asyncRedirCritSection)
                {
                    this.stdOutRedirState = null;
                    this.stdErrorRedirState = null;
                }
            }
        }

        /// <summary>
        /// called by .NET framework when we're reading redirected stdout and stderror
        /// from !! process asynchroniosly
        /// </summary>
        /// <param name="ar"></param>
        private void RedirectOutputCallback(IAsyncResult ar)
        {
            //we pass this object in while calling BeginReadf
            AsyncRedirectedOutputState state = ar.AsyncState as AsyncRedirectedOutputState;

            Stream s = state.Stream;

            int numBytesRead = 0;
            try
            {
                numBytesRead = s.EndRead(ar);
            }
            catch (IOException)
            {
                //expected - it will be thrown if the Process object has been disposed
                //It happens if we had called Kill on it when we had to cancel the execution
            }

            //see if our state has changed
            lock (this.stateCritSection)
            {
                if (this.runningProcess.HasExited)
                {

                    state.Event.Set();
                    return;
                }
            }

            //if we read something - notify our host
            if (numBytesRead > 0)
            {
                string msg = state.Encoding.GetString(state.ReadOutputBuffer, 0, numBytesRead);

                OnQeSqlCmdMessageFromApp(msg, state.StdOut);

                s.BeginRead(state.ReadOutputBuffer, 0, AsyncRedirectedOutputState.ReadBufferSizeInBytes, this.readBufferCallback, state);
            }
            else
            {
                state.Event.Set();
            }
        }


        /// <summary>
        /// called when we need to output a message from the !! process
        /// </summary>
        /// <param name="message"></param>
        /// <param name="stdOut"></param>
        private void OnQeSqlCmdMessageFromApp(string message, bool stdOut)
        {
            if (QeSqlCmdMessageFromApp != null)
            {
                //this method can be called from multiple threads, 
                //so we need to serialize the calls so that the string won't be
                //overwriting themselves in the output destination
                lock (asyncRedirCritSection)
                {
                    if (!string.IsNullOrWhiteSpace(message) && !string.IsNullOrEmpty(message))
                    {
                        QeSqlCmdMessageFromApp(this, new CommandEventArgs() { Message = message.Trim(), StdOut = stdOut });
                    }
                }
            }
        }
    }
}
