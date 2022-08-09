// <copyright file="Program.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// A simple mock console for testing the Waitable Exec task.
// </copyright>

namespace MockConsole
{
    using System.Threading;

    /// <summary>
    /// A simple mock console for testing the Exec task.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main entry point.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>Exit code, 0 if success.</returns>
        public static int Main(string[] args)
        {
            /// <summary>
            /// Analyze the component (e.g., set of object files) and produce xml report.
            /// </summary>
            const int OneSecond = 1000;

            /// <summary>
            /// If the mutex is not already aquired by the caller we
            /// assume an error.
            /// </summary>
            const int MutexNotLocked = 100;

            /// <summary>
            /// The name of the Mutex to check. 
            /// </summary>
            string mutexName = args[0];

            /// <summary>
            /// The exit code to return. This is passed in by the caller.
            /// We overwrite if the Mutex is aquired since that is unexpected.
            /// </summary>
            int exitCode = int.Parse(args[1]);

            using (var mutex = new Mutex(false, mutexName))
            {
                if (mutex.WaitOne(OneSecond))
                {
                    exitCode = MutexNotLocked;
                }
            }

            return exitCode;
        }
    }
}
