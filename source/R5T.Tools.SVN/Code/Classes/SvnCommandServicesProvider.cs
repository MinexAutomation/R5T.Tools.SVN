using System;

using Microsoft.Extensions.Logging;

using R5T.NetStandard.IO.Paths;
using R5T.NetStandard.OS;


namespace R5T.Tools.SVN
{
    public static class SvnCommandServicesProvider
    {
        public static void DeleteSvnIgnoreValue(FilePath svnExecutableFilePath, AbsolutePath path, ILogger logger)
        {
            var svnIgnorePropertyName = "svn:ignore";

            logger.LogDebug($"Deleting {svnIgnorePropertyName} for {path}...");

            var arguments = $@"propdel {svnIgnorePropertyName} ""{path}""";

            ProcessRunner.Run(svnExecutableFilePath.Value, arguments);

            logger.LogInformation($"Deleted {svnIgnorePropertyName} for {path}...");
        }

        /// <summary>
        /// Sets the value of the svn:ignore property.
        /// </summary>
        public static void SetSvnIgnoreValue(FilePath svnExecutableFilePath, AbsolutePath path, string ignoreValue, ILogger logger)
        {
            var svnIgnorePropertyName = "svn:ignore";

            logger.LogDebug($"Setting {svnIgnorePropertyName} for {path}...");

            var arguments = $@"propset {svnIgnorePropertyName} {ignoreValue} ""{path}""";

            ProcessRunner.Run(svnExecutableFilePath.Value, arguments);

            logger.LogInformation($"Set {svnIgnorePropertyName} for {path}...");
        }

        public static Version GetVersion(FilePath svnExecutableFilePath, ILogger logger)
        {
            logger.LogDebug("Getting svn version...");

            var arguments = "--version --quiet";

            var versionString = String.Empty;
            var runOptions = new ProcessRunOptions
            {
                Command = svnExecutableFilePath.Value,
                Arguments = arguments,
                ReceiveOutputData = (sender, e) =>
                {
                    if(e.Data is null)
                    {
                        return;
                    }

                    var line = e.Data;

                    versionString = line;
                },
                ReceiveErrorData = ProcessRunOptions.DefaultReceiveErrorData,
            };
          
            ProcessRunner.Run(runOptions);

            var version = Version.Parse(versionString);

            logger.LogInformation("Got svn version.");

            return version;
        }
    }
}
