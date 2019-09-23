using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using R5T.Neapolis;
using R5T.NetStandard;
using R5T.NetStandard.IO;
using R5T.NetStandard.IO.Paths;
using R5T.NetStandard.IO.Paths.Extensions;
using R5T.NetStandard.IO.Serialization;
using R5T.NetStandard.OS;

using R5T.Tools.SVN.XML;

using PathUtilities = R5T.NetStandard.IO.Paths.Utilities;


namespace R5T.Tools.SVN
{
    public static class SvnCommandServicesProvider
    {
        public static ProcessOutputCollector Run(FilePath svnExecutableFilePath, string arguments, bool throwIfAnyError = true)
        {
            var outputCollector = new ProcessOutputCollector();
            var runOptions = new ProcessRunOptions
            {
                Command = svnExecutableFilePath.Value,
                Arguments = arguments,
                ReceiveOutputData = outputCollector.ReceiveOutputData,
                ReceiveErrorData = outputCollector.ReceiveErrorData,
            };

            ProcessRunner.Run(runOptions);

            if (throwIfAnyError && outputCollector.AnyError)
            {
                throw new Exception($"SVN automation failure.\nReceived:\n{outputCollector.GetErrorText()}");
            }

            return outputCollector;
        }

        public static ProcessOutputCollector Run(FilePath svnExecutableFilePath, IArgumentsBuilder argumentsBuilder, bool throwIfAnyError = true)
        {
            var arguments = argumentsBuilder.Build();

            var outputCollector = SvnCommandServicesProvider.Run(svnExecutableFilePath, arguments, throwIfAnyError);
            return outputCollector;
        }

        public static string RunGetText(FilePath svnExecutableFilePath, IArgumentsBuilder argumentsBuilder, bool throwIfAnyError = true)
        {
            var outputCollector = SvnCommandServicesProvider.Run(svnExecutableFilePath, argumentsBuilder, throwIfAnyError);

            var text = outputCollector.GetOutputText();
            return text;
        }

        public static string RunGetXmlText(FilePath svnExecutableFilePath, IArgumentsBuilder argumentsBuilder, bool throwIfAnyError = true)
        {
            argumentsBuilder
                .AddXml();
                ;

            var textXml = SvnCommandServicesProvider.RunGetText(svnExecutableFilePath, argumentsBuilder, throwIfAnyError);
            return textXml;
        }

        public static TXmlType RunGetXmlType<TXmlType>(FilePath svnExecutableFilePath, IArgumentsBuilder argumentsBuilder, bool throwIfAnyError = true)
        {
            var xmlText = SvnCommandServicesProvider.RunGetXmlText(svnExecutableFilePath, argumentsBuilder, throwIfAnyError);

            using (var stream = StreamHelper.FromString(xmlText))
            {
                var xmlType = XmlStreamSerializer.Deserialize<TXmlType>(stream, SvnXml.DefaultNamespace);
                return xmlType;
            }
        }

        public static void Add(FilePath svnExecutableFilePath, AbsolutePath path, ILogger logger)
        {
            logger.LogDebug($"Adding changes for {path}...");

            var arguments = $@"add ""{path}""";

            var outputCollector = SvnCommandServicesProvider.Run(svnExecutableFilePath, arguments);

            var svnOutput = outputCollector.GetOutputText();

            var expectedOutput = $"A         {path}\r\n";
            if(svnOutput != expectedOutput)
            {
                throw new Exception($"SVN automation failure.\nReceived:\n{svnOutput}");
            }

            logger.LogInformation($"Added changes for {path}.");
        }

        #region Checkout

        public static IArgumentsBuilder GetCheckoutArguments(string repositoryUrl, string localDirectoryPath)
        {
            var argumentsBuilder = ArgumentsBuilder.New()
                .AddVerb("checkout", verbBuilder =>
                {
                    verbBuilder
                        .AddValue(repositoryUrl)
                        .AddPath(localDirectoryPath)
                        ;
                });
            return argumentsBuilder;
        }

        public static IArgumentsBuilder GetCheckoutArguments(Uri repositoryUrl, AbsolutePath localDirectoryPath)
        {
            var argumentsBuilder = SvnCommandServicesProvider.GetCheckoutArguments(repositoryUrl.ToString(), localDirectoryPath.Value);
            return argumentsBuilder;
        }

        #endregion

        #region Commit

        /// <summary>
        /// Commits changes to the specified path and returns the revision number.
        /// For directory paths, to commit only changes to the directory (for example, changes to SVN properties of the directory) and not changes within the directory, set the include all changes within path input to false.
        /// </summary>
        public static int Commit(FilePath svnExecutableFilePath, AbsolutePath path, string message, ILogger logger, bool includeAllChangesWithinPath = true)
        {
            logger.LogDebug($"Committing changes for {path}...");

            var arguments = $@"commit ""{path}"" --message ""{message}""";

            // For directory paths, you can commit ONLY the directory (and not changes within the directory) using the empty depth option.
            if(!includeAllChangesWithinPath)
            {
                arguments.Append(" --depth empty");
            }

            var outputCollector = SvnCommandServicesProvider.Run(svnExecutableFilePath, arguments);

            var lines = outputCollector.GetOutputLines().ToArray();
            if(lines.Length < 1)
            {
                return -1;
            }

            var lastLine = lines.Last();
            var trimmedLastLine = lastLine.TrimEnd('.');

            var tokens = trimmedLastLine.Split(' ');
            if(tokens[0] != "Committed" || tokens[1] != "revision")
            {
                throw new Exception($"SVN automation failure.\nReceived:\n{lastLine}");
            }

            var revisionString = tokens[2];

            var revision = Int32.Parse(revisionString);

            logger.LogInformation($"Committed changes for {path}.");

            return revision;
        }

        #endregion

        #region Info

        public static IArgumentsBuilder GetInfoArguments(string path)
        {
            var argumentsBuilder = ArgumentsBuilder.New()
                .AddVerb("info", verbBuilder =>
                {
                    verbBuilder
                        .AddPath(path)
                        ;
                });
            return argumentsBuilder;
        }

        public static IArgumentsBuilder GetInfoArguments(AbsolutePath path)
        {
            var argumentsBuilder = SvnCommandServicesProvider.GetInfoArguments(path);
            return argumentsBuilder;
        }

        #endregion

        public static bool HasProperty(FilePath svnExecutableFilePath, AbsolutePath path, string propertyName, ILogger logger)
        {
            logger.LogDebug($"Testing existence of SVN property {propertyName} for {path}...");

            var properties = SvnCommandServicesProvider.ListProperties(svnExecutableFilePath, path, logger);

            var hasProperty = properties.Contains(propertyName);

            logger.LogInformation($"Tested existence of SVN property {propertyName} for {path}.");

            return hasProperty;
        }

        public static string[] ListProperties(FilePath svnExecutableFilePath, AbsolutePath path, ILogger logger)
        {
            logger.LogDebug($"Listing SVN properties of {path}...");

            var arguments = $@"proplist ""{path}"" --xml";

            var outputCollector = SvnCommandServicesProvider.Run(svnExecutableFilePath, arguments);

            var svnOutput = outputCollector.GetOutputText();
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

        public static void Delete(FilePath svnExecutableFilePath, AbsolutePath path, ILogger logger, bool force = false)
        {
            logger.LogDebug($"SVN deleting path {path}...");

            var arguments = $@"delete ""{path}""";

            if(force)
            {
                arguments = arguments.Append(" --force");
            }

            var outputCollector = SvnCommandServicesProvider.Run(svnExecutableFilePath, arguments);

            bool success = false;
            var expectedOutput = $"D         {path}";
            using (var reader = outputCollector.GetOutputReader())
            {
                while (!reader.ReadLineIsEnd(out var line))
                {
                    if (expectedOutput == line)
                    {
                        success = true;
                        break;
                    }
                }
            }

            if(!success)
            {
                throw new Exception($"SVN automation failure.\nReceived:\n{outputCollector.GetOutputText()}");
            }

            logger.LogInformation($"SVN deleted path {path}.");
        }

        /// <summary>
        /// Deletes an SVN property from a path.
        /// Operation is idempotent.
        /// </summary>
        public static void DeleteProperty(FilePath svnExecutableFilePath, AbsolutePath path, string propertyName, ILogger logger)
        {
            logger.LogDebug($"Deleting SVN property '{propertyName}' of {path}...");

            var arguments = $@"propdel {propertyName} ""{path}""";

            var outputCollector = SvnCommandServicesProvider.Run(svnExecutableFilePath, arguments);

            var line = outputCollector.GetOutputTextTrimmed();

            var expectedLine1 = $"property '{propertyName}' deleted from '{path}'.";
            var expectedLine2 = $"Attempting to delete nonexistent property '{propertyName}' on '{path}'";
            if (expectedLine1 != line && expectedLine2 != line)
            {
                throw new Exception($"SVN automation failure.\nReceived:\n{line}");
            }

            logger.LogInformation($"Deleted SVN property '{propertyName} of {path}.");
        }

        public static string GetPropertyValue(FilePath svnExecutableFilePath, AbsolutePath path, string propertyName, ILogger logger)
        {
            logger.LogDebug($"Getting value of SVN property {propertyName} for {path}...");

            var arguments = $@"propget {propertyName} ""{path}"" --xml";

            var outputCollector = SvnCommandServicesProvider.Run(svnExecutableFilePath, arguments);

            // Parse output.
            var svnOutput = outputCollector.GetOutputText();

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

            var outputCollector = SvnCommandServicesProvider.Run(svnExecutableFilePath, arguments);

            // Test for success.
            var line = outputCollector.GetOutputTextTrimmed();

            var expectedLine = $"property '{propertyName}' set on '{path}'";
            if (expectedLine != line)
            {
                throw new Exception($"SVN automation failure.\nExpected output:\n{expectedLine}\nReceived:\n{line}");
            }

            logger.LogInformation($"Set value of SVN property {propertyName} for {path}.");
        }

        public static string GetNewSvnArguments()
        {
            var output = String.Empty;
            return output;
        }

        public static string AddToken(string arguments, string token)
        {
            var appendix = $" {token}"; // Note beginning space.

            var output = arguments.Append(appendix);
            return output;
        }

        public static string AddStatus(string svnArguments, string path)
        {
            var token = "status";

            var output = SvnCommandServicesProvider.AddToken(svnArguments, token);
            output = SvnCommandServicesProvider.AddPath(svnArguments, path);
            return output;
        }

        public static string AddStatus(string svnArguments, AbsolutePath path)
        {
            var output = SvnCommandServicesProvider.AddStatus(svnArguments, path.Value);
            return output;
        }

        public static string AddPath(string svnArguments, string path)
        {
            var token = $@"""{path}""";

            var output = SvnCommandServicesProvider.AddToken(svnArguments, token);
            return output;
        }

        public static string AddPath(string svnArguments, AbsolutePath path)
        {
            var output = SvnCommandServicesProvider.AddPath(svnArguments, path.Value);
            return output;
        }

        #region Status

        public static string GetStatusXmlText(FilePath svnExecutableFilePath, AbsolutePath path, ILogger logger)
        {
            logger.LogDebug($"Getting SVN status XML text for path {path}...");

            var arguments = $@"status ""{path}"" -v --xml";

            var outputCollector = SvnCommandServicesProvider.Run(svnExecutableFilePath, arguments);

            logger.LogInformation($"Got SVN status XML text of path {path}.");

            var output = outputCollector.GetOutputText();
            return output;
        }

        public static StatusType GetStatusXmlType(FilePath svnExecutableFilePath, AbsolutePath path, ILogger logger)
        {
            //var statusXmlText = SvnCommandServicesProvider.GetStatusXmlText(svnExecutableFilePath, path, logger);
            var statusXmlText = SvnCommandServicesProvider.GetStatusXmlText2(svnExecutableFilePath, path, logger);

            using (var stream = StreamHelper.FromString(statusXmlText))
            {
                var statusXmlType = XmlStreamSerializer.Deserialize<StatusType>(stream, SvnXml.DefaultNamespace);
                return statusXmlType;
            }
        }

        public static SvnStringPathStatus[] GetStatus(FilePath svnExecutableFilePath, AbsolutePath path, ILogger logger)
        {
            var statusXmlType = SvnCommandServicesProvider.GetStatusXmlType(svnExecutableFilePath, path, logger);

            var output = statusXmlType.target
                .Where(x => x.entry != null)
                .SelectMany(x => x.entry)
                    .Select(x => new SvnStringPathStatus { Path = x.path, ItemStatus = x.wcstatus.item.ToItemStatus() })
                    .ToArray();
            return output;
        }

        public static IArgumentsBuilder GetStatus(AbsolutePath path)
        {
            var argumentsBuilder = ArgumentsBuilder.New()
                .AddVerb("status", verbBuilder =>
                {
                    verbBuilder
                        .AddPath(path)
                        ;
                });
            return argumentsBuilder;
        }

        public static IArgumentsBuilder GetStatusVerbose(AbsolutePath path)
        {
            var argumentsBuilder = SvnCommandServicesProvider.GetStatus(path)
                .AddVerbose();

            return argumentsBuilder;
        }

        public static IArgumentsBuilder GetStatusVerboseForInstanceOnly(AbsolutePath path)
        {
            var argumentsBuilder = SvnCommandServicesProvider.GetStatusVerbose(path)
                .ForInstanceOnly();
                
            return argumentsBuilder;
        }

        public static IArgumentsBuilder GetStatusVerboseDepthInfinity(AbsolutePath path)
        {
            var argumentsBuilder = SvnCommandServicesProvider.GetStatusVerbose(path)
                .SetDepth("infinity");

            return argumentsBuilder;
        }

        /// <summary>
        /// Sets command flags to get an infinite-depth SVN status command that includes ignored items (does not respect svn:ignore and global ignore properties).
        /// </summary>
        public static IArgumentsBuilder GetStatusVerboseDepthInfinityNoIgnore(AbsolutePath path)
        {
            var argumentsBuilder = SvnCommandServicesProvider.GetStatusVerboseDepthInfinity(path)
                .AddFlagFull("no-ignore");

            return argumentsBuilder;
        }

        public static StatusType GetXmlStatusType(FilePath svnExecutableFilePath, IArgumentsBuilder argumentsBuilder)
        {
            var status = SvnCommandServicesProvider.RunGetXmlType<StatusType>(svnExecutableFilePath, argumentsBuilder);
            return status;
        }

        public static SvnStringPathStatus[] GetStatuses(StatusType xmlStatusType)
        {
            var statuses = xmlStatusType.target
                .Where(x => x.entry != null)
                .SelectMany(x => x.entry)
                    .Select(x => new SvnStringPathStatus { Path = x.path, ItemStatus = x.wcstatus.item.ToItemStatus() })
                    .ToArray();

            return statuses;
        }

        /// <summary>
        /// The default SVN status method.
        /// </summary>
        public static SvnStringPathStatus[] GetStatuses(FilePath svnExecutableFilePath, IArgumentsBuilder argumentsBuilder)
        {
            var xmlStatusType = SvnCommandServicesProvider.GetXmlStatusType(svnExecutableFilePath, argumentsBuilder);

            var statuses = SvnCommandServicesProvider.GetStatuses(xmlStatusType);
            return statuses;
        }

        public static string GetStatusXmlText2(FilePath svnExecutableFilePath, AbsolutePath path, ILogger logger)
        {
            logger.LogDebug($"Getting SVN status XML text for path {path}...");

            var arguments = SvnCommandServicesProvider.GetStatusVerboseForInstanceOnly(path)
                .AddFlagFull("xml")
                .Build();

            var outputCollector = SvnCommandServicesProvider.Run(svnExecutableFilePath, arguments);

            logger.LogInformation($"Got SVN status XML text of path {path}.");

            var output = outputCollector.GetOutputText();
            return output;
        }

        #endregion

        public static Version GetVersion(FilePath svnExecutableFilePath, ILogger logger)
        {
            logger.LogDebug("Getting svn version...");

            var arguments = "--version --quiet";

            var outputCollector = SvnCommandServicesProvider.Run(svnExecutableFilePath, arguments);

            var line = outputCollector.GetOutputText();

            var versionString = line;

            var version = Version.Parse(versionString);

            logger.LogInformation("Got svn version.");

            return version;
        }

        public static void Revert(FilePath svnExecutableFilePath, AbsolutePath path, ILogger logger)
        {
            logger.LogDebug($"SVN reverting {path}...");

            var arguments = $@"revert ""{path}""";

            var outputCollector = SvnCommandServicesProvider.Run(svnExecutableFilePath, arguments);

            // Check for success.
            var line = outputCollector.GetOutputTextTrimmed();

            var expectedLine = $"Reverted '{path}'";
            if(expectedLine != line)
            {
                throw new Exception($"SVN automation failure.\nReceived:\n{outputCollector.GetOutputText()}");
            }

            logger.LogInformation($"SVN reverted {path}.");
        }

        public static int Update(FilePath svnExecutableFilePath, AbsolutePath path, ILogger logger)
        {
            var nonDirectoryIndicatedPath = PathUtilities.EnsurePathIsNotDirectoryIndicated(path.Value).AsAbsolutePath();

            logger.LogDebug($"SVN updating {path}..."); // Use the specified path.

            var arguments = $@"update ""{nonDirectoryIndicatedPath}"""; // Use the non-directory indicated path.

            var outputCollector = SvnCommandServicesProvider.Run(svnExecutableFilePath, arguments);

            var lines = new List<string>();
            using (var reader = outputCollector.GetOutputReader())
            {
                while (!reader.ReadLineIsEnd(out var line))
                {
                    lines.Add(line);
                }
            }

            var lastLine = lines.Last();
            var trimmedLastLine = lastLine.TrimEnd('.');
            var tokens = trimmedLastLine.Split(' ');
            var revisionNumber = tokens[tokens.Length - 1];

            var revision = Int32.Parse(revisionNumber);

            logger.LogInformation($"SVN updated {path}.\nRevision: {revision}");

            return revision;
        }
    }
}
