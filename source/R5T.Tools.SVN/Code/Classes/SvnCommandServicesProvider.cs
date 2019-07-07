using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using R5T.NetStandard.IO.Paths;
using R5T.NetStandard.OS;

using PathUtilities = R5T.NetStandard.IO.Paths.Utilities;


namespace R5T.Tools.SVN
{
    public static class SvnCommandServicesProvider
    {
        public static bool HasProperty(FilePath svnExecutableFilePath, AbsolutePath path, string propertyName, ILogger logger)
        {
            logger.LogDebug($"Testing existence of SVN property {propertyName} for {path}...");

            var properties = SvnCommandServicesProvider.ListProperties(svnExecutableFilePath, path, logger);

            var hasProperty = properties.Contains(propertyName);

            logger.LogDebug($"Tested existence of SVN property {propertyName} for {path}.");

            return hasProperty;
        }

        public static string[] ListProperties(FilePath svnExecutableFilePath, AbsolutePath path, ILogger logger)
        {
            logger.LogDebug($"Listing SVN properties of {path}...");

            var arguments = $@"proplist ""{path}"" --xml";

            var svnOutputCollector = new StringBuilder();
            var runOptions = new ProcessRunOptions
            {
                Command = svnExecutableFilePath.Value,
                Arguments = arguments,
                ReceiveOutputData = (sender, e) =>
                {
                    if (e.Data is null)
                    {
                        return;
                    }

                    svnOutputCollector.AppendLine(e.Data);
                },
                ReceiveErrorData = ProcessRunOptions.DefaultReceiveErrorData,
            };

            ProcessRunner.Run(runOptions);

            var svnOutput = svnOutputCollector.ToString();
            var document = XDocument.Parse(svnOutput);

            var properties = document.Element("properties");

            if (!properties.Elements().Any())
            {
                return Array.Empty<string>();
            }

            var expectedPath = path.Value; // This method does not convert paths to non-Windows.

            var target = properties.Elements("target").Where(x => x.Attribute("path").Value == expectedPath).Single();

            var output = target.Elements("property").Attributes("name").Select(x => x.Value).ToArray();

            logger.LogInformation($"Listed SVN properties of {path}.");

            return output;
        }

        /// <summary>
        /// Deletes an SVN property from a path.
        /// Operation is idempotent.
        /// </summary>
        public static void DeleteProperty(FilePath svnExecutableFilePath, AbsolutePath path, string propertyName, ILogger logger)
        {
            logger.LogDebug($"Deleting SVN property '{propertyName}' of {path}...");

            var arguments = $@"propdel {propertyName} ""{path}""";

            var runOptions = new ProcessRunOptions
            {
                Command = svnExecutableFilePath.Value,
                Arguments = arguments,
                ReceiveOutputData = (sender, e) =>
                {
                    if (e.Data is null)
                    {
                        return;
                    }

                    var line = e.Data;

                    var expectedLine1 = $"property '{propertyName}' deleted from '{path}'.";
                    var expectedLine2 = $"Attempting to delete nonexistent property '{propertyName}' on '{path}'";
                    if (expectedLine1 != line && expectedLine2 != line)
                    {
                        throw new Exception($"SVN automation failure.\nReceived:\n{line}");
                    }
                },
                ReceiveErrorData = ProcessRunOptions.DefaultReceiveErrorData,
            };

            ProcessRunner.Run(runOptions);

            logger.LogInformation($"Deleted SVN property '{propertyName} of {path}...");
        }

        public static string GetPropertyValue(FilePath svnExecutableFilePath, AbsolutePath path, string propertyName, ILogger logger)
        {
            logger.LogDebug($"Getting value of SVN property {propertyName} for {path}...");

            var arguments = $@"propget {propertyName} ""{path}"" --xml";

            var svnOutputCollector = new StringBuilder();
            var runOptions = new ProcessRunOptions
            {
                Command = svnExecutableFilePath.Value,
                Arguments = arguments,
                ReceiveOutputData = (sender, e) =>
                {
                    if (e.Data is null)
                    {
                        return;
                    }

                    svnOutputCollector.AppendLine(e.Data);
                },
                ReceiveErrorData = ProcessRunOptions.DefaultReceiveErrorData,
            };

            ProcessRunner.Run(runOptions);

            // Parse output.
            var svnOutput = svnOutputCollector.ToString();
            var document = XDocument.Parse(svnOutput);

            var properties = document.Element("properties");

            if (!properties.Elements().Any())
            {
                throw new Exception($"SVN automation failure.\nReceived:\n{svnOutput}");
            }

            var expectedPath = PathUtilities.EnsureNonWindowsDirectorySeparator(path.Value); // Path value is converted to a non-Windows path.

            var property = properties.Elements("target").Where(x => x.Attribute("path").Value == expectedPath).Single().Element("property");

            if (property.Attribute("name").Value != propertyName)
            {
                throw new Exception($"SVN automation failure.\nReceived:\n{svnOutput}");
            }

            var output = property.Value;

            logger.LogInformation($"Got value of SVN property {propertyName} for {path}.");

            return output;
        }

        public static void SetPropertyValue(FilePath svnExecutableFilePath, AbsolutePath path, string propertyName, string ignoreValue, ILogger logger)
        {
            logger.LogDebug($"Setting value of SVN property {propertyName} for {path}...");

            var arguments = $@"propset {propertyName} {ignoreValue} ""{path}""";

            // Test for success.
            var runOptions = new ProcessRunOptions
            {
                Command = svnExecutableFilePath.Value,
                Arguments = arguments,
                ReceiveOutputData = (sender, e) =>
                {
                    if (e.Data is null)
                    {
                        return;
                    }

                    var line = e.Data;

                    var expectedLine = $"property '{propertyName}' set on '{path}'";
                    if (expectedLine != line)
                    {
                        throw new Exception($"SVN automation failure.\nExpected output:\n{expectedLine}\nReceived:\n{line}");
                    }
                },
                ReceiveErrorData = ProcessRunOptions.DefaultReceiveErrorData,
            };

            ProcessRunner.Run(runOptions);

            logger.LogInformation($"Set value of SVN property {propertyName} for {path}.");
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
