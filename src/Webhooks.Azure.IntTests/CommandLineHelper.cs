using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Logging;

namespace ServiceStack.Webhooks.Azure.IntTests
{
    internal static class CommandLineHelper
    {
        private const string TestOutputDirectorySubstitution = @"%TestOutDir%";

        internal static void RunCommand(ILog logger, string toolPath, string args, bool elevated = true)
        {
            var toolPathResolved = Environment.ExpandEnvironmentVariables(toolPath);
            var startInfo = new ProcessStartInfo(toolPathResolved, args)
            {
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            };

            if (elevated)
            {
                startInfo.Verb = "runas";
                startInfo.UseShellExecute = true;
            }

            using (var proc = new Process {StartInfo = startInfo})
            {
                try
                {
                    proc.Start();
                }
                catch (Exception ex)
                {
                    logger.Debug(ex, "Failed to run command: {0} {1}".Fmt(toolPath, args));

                    //ignore issue
                }
            }
        }

        internal static void RunCommandSilentlyAndWait(ILog logger, string toolPath, string args, bool elevated = true)
        {
            var toolPathResolved = Environment.ExpandEnvironmentVariables(toolPath);
            var startInfo = new ProcessStartInfo(toolPathResolved, args)
            {
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            if (elevated)
            {
                startInfo.Verb = "runas";
                startInfo.UseShellExecute = true;
            }

            using (var proc = new Process {StartInfo = startInfo})
            {
                try
                {
                    proc.Start();
                    proc.WaitForExit();
                }
                catch (Exception ex)
                {
                    logger.Debug(ex, "Failed to run command: {0} {1}".Fmt(toolPath, args));

                    //ignore issue
                }
            }
        }

        internal static void KillProcesses(string processName)
        {
            try
            {
                // Kill remaining process(es)
                Process.GetProcessesByName(processName).ToList().ForEach(p =>
                {
                    p.Kill();
                    p.WaitForExit();
                });
            }
            catch (Exception)
            {
                //ignore problem
            }
        }

        internal static string SubstitutePaths(string path)
        {
            var currentDirectory = TestContext.CurrentContext.TestDirectory;
            return path.Replace(TestOutputDirectorySubstitution, currentDirectory);
        }
    }
}