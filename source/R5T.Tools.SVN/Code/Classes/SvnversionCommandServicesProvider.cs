using System;
using System.Linq;

using Microsoft.Extensions.Logging;

using R5T.NetStandard;
using R5T.NetStandard.IO.Paths;
using R5T.NetStandard.OS;


namespace R5T.Tools.SVN
{
    public static class SvnversionCommandServicesProvider
    {
        public static int GetLatestRevision(FilePath svnversionExecutableFilePath, DirectoryPath targetDirectoryPath, ILogger logger)
        {
            logger.LogDebug($"Getting latest SVN revision for directory:\n{targetDirectoryPath}");

            var arguments = $@"""{targetDirectoryPath.Value}"" --no-newline --quiet";

            var revisionNumberString = String.Empty;
            var runOptions = new ProcessRunOptions
            {
                Command = svnversionExecutableFilePath.Value,
                Arguments = arguments,
                ReceiveOutputData = (sender, e) =>
                {
                    if (e.Data is null)
                    {
                        return;
                    }

                    var line = e.Data;

                    revisionNumberString = line;
                    if (revisionNumberString.Contains(":"))
                    {
                        // {Revision number of target}:{Latest revision number of any recursive directory or file} - We want the second token.
                        var tokens = line.Split(':');

                        revisionNumberString = tokens[1];
                    }

                    while(!Char.IsNumber(revisionNumberString.Last()))
                    {
                        revisionNumberString = revisionNumberString.ExceptLast();
                    }
                },
                ReceiveErrorData = ProcessRunOptions.DefaultReceiveErrorData,
            };

            ProcessRunner.Run(runOptions);

            var revisionNumber = Int32.Parse(revisionNumberString);

            logger.LogInformation($"Got latest SVN revision for directory:\n{targetDirectoryPath}");

            return revisionNumber;
        }
    }
}
