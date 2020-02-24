using System;
using System.Diagnostics;
using System.Fabric;
using System.Fabric.Description;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Logging;

namespace ServiceStack.Webhooks.Azure.IntTests
{
    public abstract class FabricIntegrationTestBase
    {
        private const string ServiceFabricClusterProcessName = @"ServiceFabricLocalClusterManager";
        private const string ServiceFabricClusterManagementToolSettingName = @"FabricIntegrationTestBase.ClusterManagementTool";
        private const string ServiceFabricImageStoreConnectionStringSettingName = @"FabricIntegrationTestBase.ImageStoreConnectionString";
        private const string AzureStorageToolSettingName = @"CloudServiceIntegrationTestBase.AzureStorageTool";
        private const string StorageResetArgumentsSettingName = @"CloudServiceIntegrationTestBase.AzureStorageResetArguments";
        private const string StorageStartArgumentsSettingName = @"CloudServiceIntegrationTestBase.AzureStorageStartArguments";
        private const string StorageStopArgumentsSettingName = @"CloudServiceIntegrationTestBase.AzureStorageStopArguments";
        private const string TestDeploymentConfigurationDir = @"..\..\..\TestContent\Fabric\bin";
        private const string EventRelayServicePackagePathSettingName = @"FabricIntegrationTestBase.EventRelayServicePackagePath";
        private const string EventRelayServiceApplicationTypeNameSettingName = @"FabricIntegrationTestBase.EventRelayServiceApplicationTypeName";
        private static ILog logger;
        private static readonly IAppSettings Settings = new AppSettings();
        private static string imageStoreConnectionString;
        private static string packagePath;
        private static string appTypeName;
        private static readonly string appTypeVersion = "1.0.0";
        private static Uri serviceName;

        [OneTimeSetUp]
        public void InitializeAllTests()
        {
            LogManager.LogFactory = new ConsoleLogFactory();
            logger = LogManager.GetLogger(typeof(CloudServiceIntegrationTestBase));
            logger.Debug("Initialization of local service fabric cluster environment for testing");

            imageStoreConnectionString = Settings.GetString(ServiceFabricImageStoreConnectionStringSettingName);
            packagePath = CommandLineHelper.SubstitutePaths(Settings.GetString(EventRelayServicePackagePathSettingName));
            appTypeName = Settings.GetString(EventRelayServiceApplicationTypeNameSettingName);
            var serviceVersion = ((AssemblyInformationalVersionAttribute) typeof(FabricIntegrationTestBase).Assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute))).InformationalVersion;
            serviceName = new Uri($"fabric:/{appTypeName}/{serviceVersion}");

            VerifyTestEnvironment();
            StartTestEnvironment();
        }

        [OneTimeTearDown]
        public void CleanupAllTests()
        {
            logger.Debug("Cleanup of local service fabric cluster environment after testing");
            KillTestEnvironment();
        }

        private static void VerifyTestEnvironment()
        {
            logger.Debug(@"Verifying local service fabric cluster test environment");

            // Ensure we have a packaged Fabric application
            var testDir = TestContext.CurrentContext.TestDirectory;
            var fabricConfigDir = Path.Combine(testDir, TestDeploymentConfigurationDir);

            if (!Directory.Exists(fabricConfigDir))
            {
                throw new InvalidOperationException(@"The local service fabric cluster test environment is not configured correctly! 
The 'Fabric' project needs to have been packaged for the current configuration, and copied (deployed) for integration testing (by the 'CopyFabricResources' MSBUILD Target).

Check the following: 
    (1) That the 'Fabric' project has been manually 'packaged', 
    (2) that the integration test project (Webhooks.Azure.IntTests) has been configured (by the 'CopyFabricResources' MSBUILD Target) to copy the packaged fabric files into its own project directory (i.e. under TestContent\Fabric\bin) on every build,
    (3) that the integration test project (Webhooks.Azure.IntTests) has a build dependency (in solution settings) on the fabric project (Fabric).");
            }
        }

        protected static void StartTestEnvironment()
        {
            // Cleanup old instances
            if (Process.GetProcessesByName(ServiceFabricClusterProcessName).Any())
            {
                CleanupTestEnvironment();
            }

            StartupStorageEmulator();
            StartupLocalFabricClusterAndDeploy();
        }

        protected static void CleanupTestEnvironment()
        {
            if (Process.GetProcessesByName(ServiceFabricClusterProcessName).Any())
            {
                ShutdownLocalFabricCluster();
                ShutdownStorageEmulator();
            }
        }

        protected static void KillTestEnvironment()
        {
            CleanupTestEnvironment();
            if (Process.GetProcessesByName(ServiceFabricClusterProcessName).Any())
            {
                CommandLineHelper.KillProcesses(ServiceFabricClusterProcessName);
            }
        }

        protected static void StartupLocalFabricClusterAndDeploy()
        {
            logger.Debug("Starting up the local service fabric cluster environment");
            var toolPath = Settings.GetString(ServiceFabricClusterManagementToolSettingName);
            if (!Process.GetProcessesByName(ServiceFabricClusterProcessName).Any())
            {
                CommandLineHelper.RunCommand(logger, toolPath, null, false);
            }

            using (var cluster = new FabricClient())
            {
                cluster.DeployPackage(imageStoreConnectionString, packagePath, appTypeName);
                cluster.ProvisionApplicationTypeAsync(appTypeName, appTypeVersion).GetAwaiter().GetResult();
                cluster.CreateApplicationInstanceAsync(serviceName, appTypeName, appTypeVersion).GetAwaiter().GetResult();
            }
        }

        protected static void ShutdownLocalFabricCluster()
        {
            logger.Debug("Shutting down the local service fabric cluster environment");
            using (var cluster = new FabricClient())
            {
                try
                {
                    cluster.DeleteApplicationTypeInstancesAsync(appTypeName).GetAwaiter().GetResult();
                    cluster.ApplicationManager.UnprovisionApplicationAsync(appTypeName, appTypeVersion).GetAwaiter().GetResult();
                    cluster.ApplicationManager.RemoveApplicationPackage(imageStoreConnectionString, appTypeName);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to undeploy and shutdown local service fabric cluster environment");
                }
            }
        }

        protected static void StartupStorageEmulator()
        {
            logger.Debug("Starting up the azure emulator storage environment");
            var toolPath = Settings.GetString(AzureStorageToolSettingName);
            var storageArgs = Settings.GetString(StorageResetArgumentsSettingName);

            CommandLineHelper.RunCommandSilentlyAndWait(logger, toolPath, storageArgs);

            storageArgs = Settings.GetString(StorageStartArgumentsSettingName);

            CommandLineHelper.RunCommandSilentlyAndWait(logger, toolPath, storageArgs);
        }

        protected static void ShutdownStorageEmulator()
        {
            logger.Debug("Shutting down azure emulator storage environment");
            var toolPath = Settings.GetString(AzureStorageToolSettingName);
            var storageArgs = Settings.GetString(StorageStopArgumentsSettingName);

            CommandLineHelper.RunCommandSilentlyAndWait(logger, toolPath, storageArgs);
        }
    }

    internal static class FabricClientExtensions
    {
        public static void DeployPackage(this FabricClient cluster, string imageStoreConnectionString, string packagePath, string appTypeName)
        {
            cluster.ApplicationManager.RemoveApplicationPackage(imageStoreConnectionString, appTypeName);
            cluster.ApplicationManager.CopyApplicationPackage(imageStoreConnectionString, packagePath, appTypeName);
        }

        public static async Task ProvisionApplicationTypeAsync(this FabricClient cluster, string appTypeName, string appTypeVersion)
        {
            try
            {
                await cluster.ApplicationManager.ProvisionApplicationAsync(appTypeName);
            }
            catch (FabricElementAlreadyExistsException)
            {
                await cluster.UnprovisionApplicationTypeAsync(appTypeName, appTypeVersion);
                await cluster.ApplicationManager.ProvisionApplicationAsync(appTypeName);
            }
        }

        public static async Task UnprovisionApplicationTypeAsync(this FabricClient cluster, string appTypeName, string appTypeVersion)
        {
            try
            {
                await cluster.ApplicationManager.UnprovisionApplicationAsync(appTypeName, appTypeVersion);
            }
            catch (FabricException ex)
            {
                if (ex.ErrorCode == FabricErrorCode.ApplicationTypeInUse)
                {
                    await cluster.DeleteApplicationTypeInstancesAsync(appTypeName);
                    await cluster.ApplicationManager.UnprovisionApplicationAsync(appTypeName, appTypeVersion);
                }
            }
        }

        public static async Task CreateApplicationInstanceAsync(this FabricClient cluster, Uri serviceName, string appTypeName, string appTypeVersion)
        {
            var applicationDescription = new ApplicationDescription(serviceName, appTypeName, appTypeVersion);
            await cluster.ApplicationManager.CreateApplicationAsync(applicationDescription);
        }

        public static async Task DeleteApplicationTypeInstancesAsync(this FabricClient cluster, string appTypeName)
        {
            var apps = await cluster.QueryManager.GetApplicationListAsync();
            if (apps.Any())
            {
                foreach (var app in apps)
                    if (app.ApplicationTypeName == appTypeName)
                    {
                        await cluster.ApplicationManager.DeleteApplicationAsync(new DeleteApplicationDescription(app.ApplicationName));
                    }
            }
        }
    }
}