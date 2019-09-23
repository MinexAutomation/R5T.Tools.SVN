using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Extensions.Logging;

using R5T.Neapolis;
using R5T.NetStandard.Extensions;
using R5T.NetStandard.IO;
using R5T.NetStandard.IO.Paths;
using R5T.NetStandard.IO.Paths.Extensions;
using R5T.NetStandard.IO.Serialization;
using R5T.NetStandard.OS;

using R5T.Tools.SVN.Extensions;
using R5T.Tools.SVN.XML;

using PathUtilities = R5T.NetStandard.IO.Paths.Utilities;


namespace R5T.Tools.SVN
{
    public static class SvnCommandExtensions
    {
        #region Common

        /// <summary>
        /// Allows direct access to run command.
        /// </summary>
        public static ProcessOutputCollector Run(this SvnCommand svnCommand, string arguments, bool throwIfAnyError = true)
        {
            var output = SvnCommandServicesProvider.Run(svnCommand.SvnExecutableFilePath, arguments, throwIfAnyError);
            return output;
        }

        public static ProcessOutputCollector Run(this SvnCommand svnCommand, IArgumentsBuilder arguments, bool throwIfAnyError = true)
        {
            var output = SvnCommandServicesProvider.Run(svnCommand.SvnExecutableFilePath, arguments, throwIfAnyError);
            return output;
        }

        public static void Add(this SvnCommand svnCommand, AbsolutePath path)
        {
            SvnCommandServicesProvider.Add(svnCommand.SvnExecutableFilePath, path, svnCommand.Logger);
        }

        public static CheckoutResult Checkout(this SvnCommand svnCommand, string repositoryUrl, string localDirectoryPath)
        {
            svnCommand.Logger.LogDebug($"SVN checkout of '{repositoryUrl}' to '{localDirectoryPath}'...");

            // Need to ensure the local directory path is NOT directory indicated (does NOT end with a directory separator).
            var correctedLocalDirectoryPath = PathUtilities.EnsureFilePathNotDirectoryIndicated(localDirectoryPath);

            var arguments = SvnCommandServicesProvider.GetCheckoutArguments(repositoryUrl, correctedLocalDirectoryPath);

            var commandOutput = SvnCommandServicesProvider.Run(svnCommand.SvnExecutableFilePath, arguments);

            var lines = commandOutput.GetOutputLines().ToList();

            var lastLine = lines.Last();
            var lastLineTokens = lastLine.Split(' ');
            var revisionNumberToken = lastLineTokens.Last().TrimEnd('.');
            var revisionNumber = Int32.Parse(revisionNumberToken);

            var entryUpdateLines = lines.ExceptLast();
            var statuses = new List<EntryUpdateStatus>();
            foreach (var line in entryUpdateLines)
            {
                var lineTokens = line.Split(new[] { " " }, 2, StringSplitOptions.RemoveEmptyEntries);

                var statusToken = lineTokens[0];
                var relativePath = lineTokens[1];

                var updateStatus = statusToken.ToUpdateStatus();

                var status = new EntryUpdateStatus
                {
                    UpdateStatus = updateStatus,
                    RelativePath = relativePath,
                };
                statuses.Add(status);
            }

            var result = new CheckoutResult
            {
                RevisionNumber = revisionNumber,
                Statuses = statuses.ToArray(),
            };

            svnCommand.Logger.LogInformation($"SVN checkout of '{repositoryUrl}' to '{localDirectoryPath}' complete.");

            return result;
        }

        /// <summary>
        /// Commits changes to the specified path and returns the revision number.
        /// For directory paths, to commit only changes to the directory (for example, changes to SVN properties of the directory) and not changes within the directory, set the include all changes within path input to false.
        /// </summary>
        public static int Commit(this SvnCommand svnCommand, AbsolutePath path, string message, bool includeAllChangesWithinPath = true)
        {
            var revision = SvnCommandServicesProvider.Commit(svnCommand.SvnExecutableFilePath, path, message, svnCommand.Logger, includeAllChangesWithinPath);
            return revision;
        }

        public static void Delete(this SvnCommand svnCommand, AbsolutePath path, bool force = false)
        {
            SvnCommandServicesProvider.Delete(svnCommand.SvnExecutableFilePath, path, svnCommand.Logger, force);
        }

        /// <summary>
        /// Determine if a path is under source control.
        /// Note: non-existent paths are NOT under source control.
        /// </summary>
        public static bool IsUnderSourceControl(this SvnCommand svnCommand, AbsolutePath path)
        {
            var status = svnCommand.StatusRobust(path);

            switch(status.ItemStatus)
            {
                case ItemStatus.NotFound:
                    throw new Exception("Ambigous status found. (Should never happen with use of robust status method.)");

                case ItemStatus.None: // Non-existent path.
                case ItemStatus.Ignored:
                case ItemStatus.NotWorkingCopy:
                case ItemStatus.Unversioned:
                    return false;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Determine if a path is under source control.
        /// Note: non-existent paths are NOT under source control.
        /// </summary>
        public static bool IsUnderSourceControl(this SvnCommand svnCommand, string path)
        {
            var absolutePath = path.AsAbsolutePath();

            var output = svnCommand.IsUnderSourceControl(absolutePath);
            return output;
        }

        public static bool HasProperty(this SvnCommand svnCommand, AbsolutePath path, string propertyName)
        {
            var output = SvnCommandServicesProvider.HasProperty(svnCommand.SvnExecutableFilePath, path, propertyName, svnCommand.Logger);
            return output;
        }

        public static string[] ListProperties(this SvnCommand svnCommand, AbsolutePath path)
        {
            var output = SvnCommandServicesProvider.ListProperties(svnCommand.SvnExecutableFilePath, path, svnCommand.Logger);
            return output;
        }

        public static void DeleteProperty(this SvnCommand svnCommand, AbsolutePath path, string propertyName)
        {
            SvnCommandServicesProvider.DeleteProperty(svnCommand.SvnExecutableFilePath, path, propertyName, svnCommand.Logger);
        }

        /// <summary>
        /// Gets the value of an SVN property.
        /// If the property does not exist, an exception is thrown.
        /// </summary>
        public static string GetPropertyValue(this SvnCommand svnCommand, AbsolutePath path, string propertyName)
        {
            var output = SvnCommandServicesProvider.GetPropertyValue(svnCommand.SvnExecutableFilePath, path, propertyName, svnCommand.Logger);
            return output;
        }

        /// <summary>
        /// Gets the value of an SVN property.
        /// If the property does not exist, the <see cref="SvnCommand.NonValue"/> is returned.
        /// </summary>
        public static string GetPropertyValueIfExists(this SvnCommand svnCommand, AbsolutePath path, string propertyName)
        {
            if (!svnCommand.HasSvnIgnoreProperty(path))
            {
                return SvnCommand.NonValue;
            }

            var output = svnCommand.GetPropertyValue(path, propertyName);
            return output;
        }

        /// <summary>
        /// Many SVN properties have values that are sets of new-line separated strings.
        /// This method gets the values in the property value set.
        /// </summary>
        public static string[] GetPropertyValues(this SvnCommand svnCommand, AbsolutePath path, string propertyName)
        {
            var value = svnCommand.GetPropertyValueIfExists(path, propertyName);
            if (SvnCommand.IsValue(value))
            {
                var output = value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                return output;
            }
            else
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Many SVN properties have values that are lists of new-line separated strings.
        /// This method tests if a list property has a given value as an element.
        /// </summary>
        public static bool HasListPropertyValue(this SvnCommand svnCommand, AbsolutePath path, string propertyName, string value)
        {
            var propertyValues = svnCommand.GetPropertyValues(path, propertyName);

            var output = propertyValues.Contains(value);
            return output;
        }

        /// <summary>
        /// Sets the value for an SVN property.
        /// </summary>
        public static void SetPropertyValue(this SvnCommand svnCommand, AbsolutePath path, string propertyName, string value)
        {
            SvnCommandServicesProvider.SetPropertyValue(svnCommand.SvnExecutableFilePath, path, propertyName, value, svnCommand.Logger);
        }

        /// <summary>
        /// Many SVN properties have values that are sets of new-line separated strings.
        /// This method sets the values in the property value set.
        /// </summary>
        public static void SetPropertyValues(this SvnCommand svnCommand, AbsolutePath path, string propertyName, params string[] values)
        {
            var value = String.Join("\n", values);

            svnCommand.SetPropertyValue(path, propertyName, value);
        }

        /// <summary>
        /// Many SVN properties have values that are sets of new-line separated strings.
        /// This method adds a value to that set.
        /// This method will not add the same value twice, as it checks if the value is already present in the set of values.
        /// </summary>
        public static void AddPropertyValue(this SvnCommand svnCommand, AbsolutePath path, string propertyName, string value)
        {
            var values = new List<string>(svnCommand.GetPropertyValues(path, propertyName));

            // Only add the value once.
            if (!values.Contains(value))
            {
                values.Add(value);

                svnCommand.SetPropertyValues(path, propertyName, values.ToArray());
            }
        }

        /// <summary>
        /// Many SVN properties have values that are sets of new-line separated strings.
        /// This method removes a value from that set.
        /// Idempotent, can be called mulitple times.
        /// If, after removing the specified value, no values remain, the property will be deleted.
        /// </summary>
        public static void RemovePropertyValue(this SvnCommand svnCommand, AbsolutePath path, string propertyName, string value)
        {
            var values = new List<string>(svnCommand.GetSvnIgnoreValues(path));

            values.Remove(value);

            if (values.IsEmpty())
            {
                svnCommand.DeleteProperty(path, propertyName);
            }
            else
            {
                svnCommand.SetSvnIgnoreValues(path, values.ToArray());
            }
        }

        public static void Revert(this SvnCommand svnCommand, AbsolutePath path)
        {
            SvnCommandServicesProvider.Revert(svnCommand.SvnExecutableFilePath, path, svnCommand.Logger);
        }

        public static int Update(this SvnCommand svnCommand, AbsolutePath path)
        {
            var revision = SvnCommandServicesProvider.Update(svnCommand.SvnExecutableFilePath, path, svnCommand.Logger);
            return revision;
        }

        public static int Update(this SvnCommand svnCommand, string path)
        {
            var absolutePath = path.AsAbsolutePath();

            var output = svnCommand.Update(absolutePath);
            return output;
        }

        public static Version GetVersion(this SvnCommand svnCommand)
        {
            var version = SvnCommandServicesProvider.GetVersion(svnCommand.SvnExecutableFilePath, svnCommand.Logger);
            return version;
        }

        #endregion

        #region Status

        /// <summary>
        /// Get the SVN status results for a file or directory.
        /// If the specified path is a file, a single result will be returned.
        /// If the specified path is a directory, possibly many results will be returned.
        /// If the path does not exist, zero results will be returned.
        /// </summary>
        public static SvnStringPathStatus[] Statuses(this SvnCommand svnCommand, AbsolutePath path)
        {
            svnCommand.Logger.LogDebug($"Getting all SVN status results for path {path}...");

            var arguments = SvnCommandServicesProvider.GetStatusVerbose(path);

            var statuses = SvnCommandServicesProvider.GetStatuses(svnCommand.SvnExecutableFilePath, arguments);

            svnCommand.Logger.LogInformation($"Got all SVN status results for path {path} ({statuses.Count()} results).");

            return statuses;
        }

        public static SvnStringPathStatus[] Statuses(this SvnCommand svnCommand, IArgumentsBuilder argumentsBuilder)
        {
            svnCommand.Logger.LogDebug($"Getting all SVN status results...");

            var statuses = SvnCommandServicesProvider.GetStatuses(svnCommand.SvnExecutableFilePath, argumentsBuilder);

            svnCommand.Logger.LogInformation($"Got all SVN status results ({statuses.Count()} results).");

            return statuses;
        }

        public static SvnStringPathStatus Status(this SvnCommand svnCommand, FilePath filePath)
        {
            svnCommand.Logger.LogDebug($"Getting SVN status of file path {filePath}...");

            var arguments = SvnCommandServicesProvider.GetStatusVerboseForInstanceOnly(filePath);

            var statuses = SvnCommandServicesProvider.GetStatuses(svnCommand.SvnExecutableFilePath, arguments);

            var status = statuses.Count() < 1
                ? new SvnStringPathStatus { Path = filePath.Value, ItemStatus = ItemStatus.None }
                : statuses.Single() // Should be only 1.
                ;

            svnCommand.Logger.LogDebug($"Got SVN status of file path {filePath}.");

            return status;
        }

        private static SvnStringPathStatus StatusRobust_Internal(this SvnCommand svnCommand, AbsolutePath absolutePath)
        {
            var arguments = SvnCommandServicesProvider.GetStatusVerboseForInstanceOnly(absolutePath)
                .AddXml(); // Get XML.

            var outputCollector = SvnCommandServicesProvider.Run(svnCommand.SvnExecutableFilePath, arguments, false);

            if (outputCollector.AnyError)
            {
                var errorText = outputCollector.GetErrorText().Trim();

                var notWorkingCopyText = $"svn: warning: W155007: '{absolutePath}' is not a working copy";
                if (errorText == notWorkingCopyText)
                {
                    var output = new SvnStringPathStatus { Path = absolutePath.Value, ItemStatus = ItemStatus.NotWorkingCopy };
                    return output;
                }

                var notFoundText = $"svn: warning: W155010: The node '{absolutePath}' was not found.";
                if (errorText == notFoundText)
                {
                    var output = new SvnStringPathStatus { Path = absolutePath.Value, ItemStatus = ItemStatus.NotFound };
                    return output;
                }

                throw new Exception($"Unknown SVN error:\n{errorText}");
            }

            var xmlText = outputCollector.GetOutputText();

            using (var stream = StreamHelper.FromString(xmlText))
            {
                var xmlStatusType = XmlStreamSerializer.Deserialize<StatusType>(stream, SvnXml.DefaultNamespace);

                var statuses = SvnCommandServicesProvider.GetStatuses(xmlStatusType);

                var status = statuses.Count() < 1
                    ? new SvnStringPathStatus { Path = absolutePath.Value, ItemStatus = ItemStatus.None }
                    : statuses.Single() // Should be only 1.
                    ;

                return status;
            }
        }

        /// <summary>
        /// Robustly determines the SVN status of a path using the output of the SVN status command.
        /// Handles both files and directories, in the case of a directory the status of the directory only, and not all the file-system entries within it.
        /// Handles warnings by turning them into into the associated <see cref="ItemStatus"/> value.
        /// Resolves the ambiguous <see cref="ItemStatus.NotFound"/> item status by walking up the path hierarchy until an items status is found.
        /// </summary>
        public static SvnStringPathStatus StatusRobust(this SvnCommand svnCommand, AbsolutePath path)
        {
            var nonDirectoryIndicatedPath = PathUtilities.EnsurePathIsNotDirectoryIndicated(path.Value).AsAbsolutePath();

            var status = svnCommand.StatusRobust_Internal(nonDirectoryIndicatedPath);

            if (status.ItemStatus != ItemStatus.NotFound)
            {
                return status;
            }

            // Determine whether the item is in 1) an ignored directory or 2) an unversioned directory by walking up the path hierarchy until 
            var parentItemStatus = ItemStatus.None;
            var parentPath = path;
            do
            {
                parentPath = PathUtilities.GetParentDirectoryPath(parentPath);

                var parentStatus = svnCommand.StatusRobust_Internal(parentPath);

                parentItemStatus = parentStatus.ItemStatus;
            }
            while (parentItemStatus == ItemStatus.NotFound);

            var output = new SvnStringPathStatus { Path = path.Value, ItemStatus = parentItemStatus };
            return output;
        }

        /// <summary>s
        /// Get only the SVN status of a specific directory, and not its children.
        /// If the directory path does not exist, returns a result with status <see cref="ItemStatus.None"/>.
        /// </summary>
        public static SvnStringPathStatus Status(this SvnCommand svnCommand, DirectoryPath directoryPath)
        {
            svnCommand.Logger.LogDebug($"Getting SVN status of directory path {directoryPath}...");

            var arguments = SvnCommandServicesProvider.GetStatusVerboseForInstanceOnly(directoryPath);

            var statuses = SvnCommandServicesProvider.GetStatuses(svnCommand.SvnExecutableFilePath, arguments);

            var status = statuses.Count() < 1
                ? new SvnStringPathStatus { Path = directoryPath.Value, ItemStatus = ItemStatus.None }
                : statuses.Single() // Should be only 1.
                ;

            svnCommand.Logger.LogDebug($"Got SVN status of directory path {directoryPath}.");

            return status;
        }

        /// <summary>
        /// The default SVN status method.
        /// </summary>
        public static SvnStringPathStatus[] StatusesDefault(this SvnCommand svnCommand, DirectoryPath directoryPath)
        {
            svnCommand.Logger.LogDebug($"Getting all SVN status results for directory path {directoryPath}...");

            var arguments = SvnCommandServicesProvider.GetStatusVerbose(directoryPath);

            var statuses = SvnCommandServicesProvider.GetStatuses(svnCommand.SvnExecutableFilePath, arguments);

            svnCommand.Logger.LogInformation($"Got all SVN status results for directroy path {directoryPath} ({statuses.Count()} results).");

            return statuses;
        }

        /// <summary>
        /// Gets the status of a a directory, and all directory contents recursively.
        /// Note: this is the same as <see cref="StatusesDefault(SvnCommand, DirectoryPath)"/> since the default path depth options is infinity.
        /// </summary>
        public static SvnStringPathStatus[] StatusesInfinity(this SvnCommand svnCommand, DirectoryPath directoryPath)
        {
            svnCommand.Logger.LogDebug($"Getting all SVN status results for directory path {directoryPath}...");

            var arguments = SvnCommandServicesProvider.GetStatusVerboseDepthInfinity(directoryPath);

            var statuses = SvnCommandServicesProvider.GetStatuses(svnCommand.SvnExecutableFilePath, arguments);

            svnCommand.Logger.LogInformation($"Got all SVN status results for directroy path {directoryPath} ({statuses.Count()} results).");

            return statuses;
        }

        /// <summary>
        /// Gets SVN status values for all directory contents recursively, disregarding any SVN ignore properties and the global SVN ignore values.
        /// </summary>
        public static SvnStringPathStatus[] StatusesInfinityNoIgnore(this SvnCommand svnCommand, DirectoryPath directoryPath)
        {
            svnCommand.Logger.LogDebug($"Getting all SVN status results for directory path {directoryPath}...");

            var arguments = SvnCommandServicesProvider.GetStatusVerboseDepthInfinityNoIgnore(directoryPath);

            var statuses = SvnCommandServicesProvider.GetStatuses(svnCommand.SvnExecutableFilePath, arguments);

            svnCommand.Logger.LogInformation($"Got all SVN status results for directroy path {directoryPath} ({statuses.Count()} results).");

            return statuses;
        }

        /// <summary>
        /// Get only the SVN status of a specific file or directory (and for directories, only the SVN status of the directory itself, and not its children).
        /// If the path does not exist, returns a result with status <see cref="ItemStatus.None"/>.
        /// </summary>
        public static SvnPathStatus Status(this SvnCommand svnCommand, AbsolutePath path)
        {
            // Allow for possibility that the path is a directory path. Find the entry matching the input path.
            var statuses = svnCommand.Statuses(path);
            if (statuses.Count() < 1)
            {
                var output = new SvnPathStatus { Path = new GeneralAbsolutePath(path.Value), ItemStatus = ItemStatus.None };
                return output;
            }

            var status = statuses.Where(x => x.Path == path.Value).Select(x => new SvnPathStatus { Path = new GeneralAbsolutePath(x.Path), ItemStatus = x.ItemStatus }).Single(); // There should be at least one result.
            return status;
        }

        /// <summary>
        /// The default method to get uncommitted changes in a directory.
        /// Note: uses the <see cref="GetUncommittedChangesWithGitHubExceptions(SvnCommand, DirectoryPath)"/> method.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<SvnStringPathStatus> GetUncommittedChanges(this SvnCommand svnCommand, DirectoryPath directoryPath)
        {
            var uncommittedChangesWithGitHubExceptions = svnCommand.GetUncommittedChangesWithGitHubExceptions(directoryPath);
            return uncommittedChangesWithGitHubExceptions;
        }

        /// <summary>
        /// GitHub does not allow empty directories to be checked in (nor directories that only contain empty directories, i.e. recursively-empty directories).
        /// Thus many times there are recursively empty unversioned diretories that will be flagged as an uncommitted change, but should not be considered as uncommitted simply because they can't be committed!
        /// </summary>
        public static IEnumerable<SvnStringPathStatus> GetUncommittedChangesWithGitHubExceptions(this SvnCommand svnCommand, DirectoryPath directoryPath)
        {
            var allUncommittedStatuses = svnCommand.GetAllUncommittedChanges(directoryPath);
            foreach (var uncommittedStatus in allUncommittedStatuses)
            {
                // Unless it's an unversioned, recursively empty directory!
                var isUnversioned = uncommittedStatus.ItemStatus == ItemStatus.Unversioned;
                if (isUnversioned)
                {
                    // Is the path a directory?
                    var isDirectory = PathUtilities.IsDirectory(uncommittedStatus.Path);
                    if (isDirectory)
                    {
                        // Is the directory recursively empty?
                        var isDirectoryRecursivelyEmpty = PathUtilities.IsDirectoryRecursivelyEmpty(uncommittedStatus.Path);
                        if (isDirectoryRecursivelyEmpty)
                        {
                            continue; // Nothing to see here.
                        }
                    }
                }

                yield return uncommittedStatus;
            }
        }

        /// <summary>
        /// Any directory contents with an <see cref="ItemStatus"/> other than <see cref="ItemStatus.NoModifications"/> is an uncommited change.
        /// </summary>
        public static IEnumerable<SvnStringPathStatus> GetAllUncommittedChanges(this SvnCommand svnCommand, DirectoryPath directoryPath)
        {
            var statuses = svnCommand.StatusesDefault(directoryPath);

            foreach (var status in statuses)
            {
                // Anything with an SVN status other than no-modifications is an uncommitted change.
                var anythingOtherThanNoModifications = status.ItemStatus != ItemStatus.NoModifications;
                if (anythingOtherThanNoModifications)
                {
                    yield return status;
                }
            }
        }

        public static bool HasUncommittedChanges(this SvnCommand svnCommand, DirectoryPath directoryPath, out IEnumerable<SvnStringPathStatus> uncommittedChanges)
        {
            uncommittedChanges = svnCommand.GetUncommittedChanges(directoryPath);

            var output = uncommittedChanges.Count() > 1;
            return output;
        }

        #endregion

        #region SVN Ignore

        public static bool HasSvnIgnoreProperty(this SvnCommand svnCommand, AbsolutePath path)
        {
            var output = svnCommand.HasProperty(path, SvnCommand.SvnIgnorePropertyName);
            return output;
        }

        /// <summary>
        /// Sets the whole value of the SVN ignore property.
        /// </summary>
        public static void SetSvnIgnoreValue(this SvnCommand svnCommand, AbsolutePath path, string value)
        {
            SvnCommandServicesProvider.SetPropertyValue(svnCommand.SvnExecutableFilePath, path, SvnCommand.SvnIgnorePropertyName, value, svnCommand.Logger);
        }

        /// <summary>
        /// Gets the whole value of the SVN ignore property, or <see cref="SvnCommand.NonValue"/> if not.
        /// </summary>
        public static string GetSvnIgnoreValue(this SvnCommand svnCommand, AbsolutePath path)
        {
            var value = svnCommand.GetPropertyValueIfExists(path, SvnCommand.SvnIgnorePropertyName);
            return value;
        }

        /// <summary>
        /// Deletes the SVN ignore property.
        /// Idempotent.
        /// </summary>
        public static void DeleteSvnIgnore(this SvnCommand svnCommand, AbsolutePath path)
        {
            svnCommand.DeleteProperty(path, SvnCommand.SvnIgnorePropertyName);
        }

        public static void SetSvnIgnoreValues(this SvnCommand svnCommand, AbsolutePath path, params string[] values)
        {
            svnCommand.SetPropertyValues(path, SvnCommand.SvnIgnorePropertyName, values);
        }

        /// <summary>
        /// The SVN ignore property value is a set of new-line separated strings.
        /// Gets the set of values of the SVN ignore property.
        /// </summary>
        public static string[] GetSvnIgnoreValues(this SvnCommand svnCommand, AbsolutePath path)
        {
            var output = svnCommand.GetPropertyValues(path, SvnCommand.SvnIgnorePropertyName);
            return output;
        }

        /// <summary>
        /// The SVN ignore property value is a set of new-line separated strings.
        /// This method tests if the set of SVN ignore values containes the specified value.
        /// </summary>
        public static bool HasSvnIgnoreValue(this SvnCommand svnCommand, AbsolutePath path, string value)
        {
            var output = svnCommand.HasListPropertyValue(path, SvnCommand.SvnIgnorePropertyName, value);
            return output;
        }

        /// <summary>
        /// The SVN ignore property value is a set of new-line separated strings.
        /// This method adds a value to the set.
        /// Idempotent, will only add a valice once since you can only ignore a value once.
        /// </summary>
        public static void AddSvnIgnoreValue(this SvnCommand svnCommand, AbsolutePath path, string value)
        {
            svnCommand.AddPropertyValue(path, SvnCommand.SvnIgnorePropertyName, value);
        }

        /// <summary>
        /// The SVN ignore property value is a set of new-line separated strings.
        /// This method allows removing one of those strings while keeping the rest.
        /// </summary>
        public static void RemoveSvnIgnoreValue(this SvnCommand svnCommand, AbsolutePath path, string value)
        {
            svnCommand.RemovePropertyValue(path, SvnCommand.SvnIgnorePropertyName, value);
        }

        #endregion
    }
}
