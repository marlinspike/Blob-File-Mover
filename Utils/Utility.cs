using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;


namespace BlobMover.Utils {
    public static class Utility {

        public enum NextHop {
            File_In = 1,
            Queue = 2,
            Blob_Out = 3
        }
        public static IConfigurationRoot GetConfigurationRoot(ExecutionContext context) {
            var config = new ConfigurationBuilder()
            .SetBasePath(context.FunctionAppDirectory)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            return config;

        }

        public static string GetConfigurationItem(ExecutionContext context, string configurationName) {
            var config = GetConfigurationRoot(context);
            return config[configurationName];
        }
    }
}
