using System;

using R5T.NetStandard.IO.Paths;


namespace R5T.Tools.SVN
{
    public static class SvnversionCommandExtensions
    {
        public static int GetLatestRevision(this SvnversionCommand svnversionCommand, DirectoryPath directoryPath)
        {
            var revision = SvnversionCommandServicesProvider.GetLatestRevision(svnversionCommand.SvnversionFilePath, directoryPath, svnversionCommand.Logger);
            return revision;
        }
    }
}
