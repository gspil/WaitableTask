// <copyright file="WaitableTasksUnitTest.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Unit tests for a  new msbuild task equivalent to Exec, but with the ability to
// ensure no concurrency occurs in an enlistment.
// </copyright>

namespace WaitableTaskUnitTests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Build.Framework.Fakes;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
    using WaitableTask;

    /// <summary>
    /// Unit tests for Wait-able Exec Task.
    /// </summary>
    [TestClass]
    public class WaitableTasksUnitTest
    { 
        /// <summary>
        /// We will have the task wait one minute on mutex.
        /// </summary>
        private const int OneMinute = 60000;

        /// <summary>
        /// The mock console that we can use to test return codes and that the
        /// mutex is not acquirable by the mock console (assuming the task has it.)
        /// </summary>
        private static readonly string MockExe = 
            Path.Combine(
                Directory.GetCurrentDirectory().Replace("WaitableTaskUnitTests", "MockConsole"),
                "MockConsole.exe");

        /// <summary>
        /// The class under test.
        /// </summary>
        private WaitableExec waitableExec;

        /// <summary>
        /// A delegate for Parallel Invoke method.
        /// </summary>
        public static void ActionDelegate()
        {
            ThreadCallback(null);
        }

        /// <summary>
        /// A callback for Threading.
        /// </summary>
        /// <param name="ignored">This is ignored.</param>
        public static void ThreadCallback(object ignored)
        {
            Thread thread = Thread.CurrentThread;
            int expectedExitCode = 0;
            WaitableExec waitableExec = new WaitableExec
            {
                CmdId = "git.status.command",
                TimeOutMilliSecs = OneMinute,
                EnlistmentPath = @"c:\dsmain"
            };
            waitableExec.Command = BuildCommandLine(WaitableTasksUnitTest.MockExe, waitableExec.MutexName, expectedExitCode);
            waitableExec.BuildEngine = new StubIBuildEngine();

            Logger.LogMessage($"Running ThreadFunction on thread {thread.ManagedThreadId}.");

            if (!waitableExec.Execute())
            {
                Logger.LogMessage($"Execute failed on thread {thread.ManagedThreadId } with code {waitableExec.ExitCode}.");
                throw new System.Exception();
            }
            else
            {
                Logger.LogMessage($"Execute succeeded on thread {thread.ManagedThreadId}.");
            }
        }

        /// <summary>
        /// Create an instance of the class under test.
        /// </summary>
        [TestInitialize]
        public void Startup()
        {
            this.waitableExec = new WaitableExec
            {
                CmdId = "git.status.command",
                TimeOutMilliSecs = OneMinute,
                EnlistmentPath = @"d:\dsmain",

                BuildEngine = new StubIBuildEngine()
            };
        }

        /// <summary>
        /// Make sure we fail a null command line.
        /// </summary>
        [TestMethod]
        public void WaitableTaskNullCmd()
        {
            Assert.IsFalse(this.waitableExec.Execute());
        }

        /// <summary>
        /// We expect to not find an nonexistent command.
        /// </summary>
        [TestMethod]
        public void WaitableTaskNoExecutable()
        {
            int fileNotFoundCode = 9009;

            this.waitableExec.Command = WaitableTasksUnitTest.BuildCommandLine("doesnotexists.exe", this.waitableExec.MutexName, 0);
            Assert.IsFalse(this.waitableExec.Execute());
            Assert.AreEqual(fileNotFoundCode, this.waitableExec.ExitCode);
        }

        /// <summary>
        /// Basic tests.
        /// </summary>
        [TestMethod]
        public void WaitableTaskCmd()
        {
            int expectedExitCode = 0;
            this.waitableExec.Command = WaitableTasksUnitTest.BuildCommandLine(WaitableTasksUnitTest.MockExe, this.waitableExec.MutexName, expectedExitCode);

            Assert.IsTrue(this.waitableExec.Execute());
            Assert.AreEqual(expectedExitCode, this.waitableExec.ExitCode);
        }

        /// <summary>
        /// Check that we get back the right error code if it is non zero.
        /// </summary>
        [TestMethod]
        public void WaitableTaskFailureReturnCode()
        {
            int expectedExitCode = 300;
            this.waitableExec.Command = WaitableTasksUnitTest.BuildCommandLine(WaitableTasksUnitTest.MockExe, this.waitableExec.MutexName, expectedExitCode);

            Assert.IsFalse(this.waitableExec.Execute());
            Assert.AreEqual(expectedExitCode, this.waitableExec.ExitCode);
        }

        /// <summary>
        /// Test threading.
        /// </summary>
        [TestMethod]
        public void WaitableTaskThreading()
        {
            for (int i = 0; i < 100; i++)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadCallback));
                Thread thread = new Thread(ThreadCallback);
            }
        }

        /// <summary>
        /// Test a simple small set of parallel tasks.
        /// </summary>
        [TestMethod]
        public void WaitableTaskParallel()
        {
            this.ParallelTest(10);
        }

        /// <summary>
        /// Parallel tasks stress test. Will take about 5 minutes.
        /// </summary>
        [TestMethod]
        public void WaitableTaskParallelStress()
        {
            this.ParallelTest(200);
        }

        /// <summary>
        /// Build the command line.
        /// </summary>
        /// <param name="exe">The path to the executable.</param>
        /// <param name="mutexName">The name of the mutex the task is using.</param>
        /// <param name="exitCode">The exit code we want the test console to return.</param>
        /// <returns>The command line string.</returns>
        private static string BuildCommandLine(string exe, string mutexName, int exitCode)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(exe);
            sb.Append(" ");
            sb.Append(mutexName);
            sb.Append(" ");
            sb.Append(exitCode.ToString());

            return sb.ToString();
        }

        /// <summary>
        /// Parallel testing.
        /// </summary>
        /// <param name="concurrencyCount">The number of instances to pass to Invoke.</param>
        private void ParallelTest(int concurrencyCount)
        {
            List<System.Action> actionList = new List<System.Action>();
            System.Action[] calls = new System.Action[] { };

            for (int i = 0; i < concurrencyCount; i++)
            {
                actionList.Add(ActionDelegate);
            }

            Parallel.Invoke(actionList.ToArray());
        }
    }
}
