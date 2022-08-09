// <copyright file="WaitableExec.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// A new msbuild task equivalent to Exec, but with the ability to
// ensure no concurrency occurs in an enlistment.
// </copyright>

namespace WaitableTask
{
    using System;
    using System.Text;
    using System.Threading;
    using Microsoft.Build.Framework;

    /// <summary>
    /// A wait-able execute task for MSBuild.
    /// </summary>
    public class WaitableExec : Microsoft.Build.Tasks.Exec
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WaitableExec" /> class.
        /// </summary>
        public WaitableExec() : base()
        {
        }

        /// <summary>
        /// Gets or sets the enlistment path this is running in.
        /// </summary>
        [Required]
        public string EnlistmentPath { get; set; }

        /// <summary>
        /// Gets or sets a unique id for the command (e.g. GUID, etc.)
        /// </summary>
        [Required]
        public string CmdId { get; set; }

        /// <summary>
        /// Gets or sets the timeout for acquiring the Mutex.
        /// </summary>
        [Required]
        public int TimeOutMilliSecs { get; set; }

        /// <summary>
        /// Gets the mutex name, constructed from Enlistment Path and Command Id.
        /// </summary>
        public string MutexName
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(@"Local\");
                sb.Append(this.EnlistmentPath.Replace(":", "_").Replace("\\", "_"));
                sb.Append(".");
                sb.Append(this.CmdId);

                return sb.ToString();
            }
        }

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <returns>True if successful, else false.</returns>
        public override bool Execute()
        {
            bool execReturn = true;

            using (var mutex = new Mutex(false, this.MutexName))
            {
                var acquiredMutex = false;

                try
                {
                    acquiredMutex = mutex.WaitOne(this.TimeOutMilliSecs, false);
                }
                catch (AbandonedMutexException)
                {
                    // If the mutex is abandoned that is OK.
                    acquiredMutex = true;
                }
                catch (Exception ex)
                {
                    Log.LogMessage($"WaitableExec Task mutex.WaitOne() failed with exception message: {ex.Message}");
                }
                finally
                {
                    if (acquiredMutex)
                    {
                        Log.LogMessage("WaitableExec Task executing command: " + this.Command);
                        execReturn = base.Execute();
                        mutex.ReleaseMutex();
                    }
                    else
                    {
                        Log.LogError("WaitableExec Task failed to obtain mutex: " + this.MutexName);
                    }
                }
            }

            return execReturn;
        }
    }
}
