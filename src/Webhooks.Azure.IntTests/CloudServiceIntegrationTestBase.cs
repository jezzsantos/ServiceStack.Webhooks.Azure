using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Logging;

namespace ServiceStack.Webhooks.Azure.IntTests
{
    public abstract class CloudServiceIntegrationTestBase
    {
        private const string AzureEmulatorServiceProcessName = @"DFService";
        private const string AzureDeployToolSettingName = @"CloudServiceIntegrationTestBase.AzureDeployTool";
        private const string AzureStorageToolSettingName = @"CloudServiceIntegrationTestBase.AzureStorageTool";
        private const string StorageResetArgumentsSettingName = @"CloudServiceIntegrationTestBase.AzureStorageResetArguments";
        private const string StorageStartArgumentsSettingName = @"CloudServiceIntegrationTestBase.AzureStorageStartArguments";
        private const string StorageStopArgumentsSettingName = @"CloudServiceIntegrationTestBase.AzureStorageStopArguments";
        private const string ComputeStartupArgumentsSettingName = @"CloudServiceIntegrationTestBase.AzureComputeStartupArguments";
        private const string ComputeShutdownArgumentsSettingName = @"CloudServiceIntegrationTestBase.AzureComputeShutdownArguments";
        private const string TestDeploymentConfigurationDir = @"..\..\..\TestContent\Cloud\bin";
        private static ILog logger;
        private static readonly IAppSettings Settings = new AppSettings();

        [OneTimeSetUp]
        public void InitializeAllTests()
        {
            LogManager.LogFactory = new ConsoleLogFactory();
            logger = LogManager.GetLogger(typeof(CloudServiceIntegrationTestBase));
            logger.Debug("Initialization of azure cloud service emulated environment for testing");
            VerifyTestEnvironment();
            StartTestEnvironment();
        }

        [OneTimeTearDown]
        public void CleanupAllTests()
        {
            logger.Debug("Cleanup of azure environment after testing");
            KillTestEnvironment();
        }

        private static void VerifyTestEnvironment()
        {
            logger.Debug(@"Verifying cloud service emulated test environment");

            // Ensure we have a packaged Azure application
            var testDir = TestContext.CurrentContext.TestDirectory;
            var cloudConfigDir = Path.Combine(testDir, TestDeploymentConfigurationDir);
            var cloudConfigDirCsx = Path.Combine(cloudConfigDir, @"csx");

            if (!Directory.Exists(cloudConfigDirCsx))
            {
                throw new InvalidOperationException(@"The azure cloud service emulated test environment is not configured correctly! 
The 'Cloud' project needs to have been packaged for the current configuration (Local|Debug), and copied (deployed) for integration testing (by the 'CopyCloudResources' MSBUILD Target).

Check the following: 
    (1) That the 'Cloud' project has been manually 'packaged' in Debug|Local mode, 
    (2) that the integration test project (Webhooks.Azure.IntTests) has been configured (by the 'CopyCloudResources' MSBUILD Target) to copy the packaged cloud configuration files (def, csx, rcf directories) into its own project directory (i.e. under TestContent\Cloud\bin) on every build,
    (3) that the integration test project (Webhooks.Azure.IntTests) has a build dependency (in solution settings) on the cloud project (Cloud).");
            }
        }

        protected static void StartTestEnvironment()
        {
            // Cleanup old instances
            if (Process.GetProcessesByName(AzureEmulatorServiceProcessName).Any())
            {
                CleanupTestEnvironment();
            }

            StartupStorageEmulator();
            StartupComputeAndDeploy();
        }

        protected static void CleanupTestEnvironment()
        {
            if (Process.GetProcessesByName(AzureEmulatorServiceProcessName).Any())
            {
                ShutdownComputeEmulator();
                ShutdownStorageEmulator();
            }
        }

        protected static void KillTestEnvironment()
        {
            CleanupTestEnvironment();
            if (Process.GetProcessesByName(AzureEmulatorServiceProcessName).Any())
            {
                CommandLineHelper.KillProcesses(AzureEmulatorServiceProcessName);
            }
        }

        protected static void StartupComputeAndDeploy()
        {
            logger.Debug("Starting up the azure cloud service emulated compute environment");
            var toolPath = Settings.GetString(AzureDeployToolSettingName);
            var computeArgs = CommandLineHelper.SubstitutePaths(Settings.GetString(ComputeStartupArgumentsSettingName));

            CommandLineHelper.RunCommandSilentlyAndWait(logger, toolPath, computeArgs);
        }

        protected static void ShutdownComputeEmulator()
        {
            logger.Debug("Shutting down the azure cloud service emulated compute environment");
            var toolPath = Settings.GetString(AzureDeployToolSettingName);
            var computeArgs = Settings.GetString(ComputeShutdownArgumentsSettingName);

            CommandLineHelper.RunCommandSilentlyAndWait(logger, toolPath, computeArgs);
        }

        protected static void StartupStorageEmulator()
        {
            logger.Debug("Starting up the azure cloud service emulated storage environment");
            var toolPath = Settings.GetString(AzureStorageToolSettingName);
            var storageArgs = Settings.GetString(StorageResetArgumentsSettingName);

            CommandLineHelper.RunCommandSilentlyAndWait(logger, toolPath, storageArgs);

            storageArgs = Settings.GetString(StorageStartArgumentsSettingName);

            CommandLineHelper.RunCommandSilentlyAndWait(logger, toolPath, storageArgs);
        }

        protected static void ShutdownStorageEmulator()
        {
            logger.Debug("Shutting down azure cloud service emulated storage environment");
            var toolPath = Settings.GetString(AzureStorageToolSettingName);
            var storageArgs = Settings.GetString(StorageStopArgumentsSettingName);

            CommandLineHelper.RunCommandSilentlyAndWait(logger, toolPath, storageArgs);
        }
    }
}