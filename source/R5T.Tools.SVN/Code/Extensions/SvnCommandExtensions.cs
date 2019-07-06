using System;

using R5T.NetStandard.IO.Paths;


namespace R5T.Tools.SVN
{
    public static class SvnCommandExtensions
    {
        public static void DeleteSvnIgnoreValue(this SvnCommand svnCommand, AbsolutePath path)
        {
            SvnCommandServicesProvider.DeleteSvnIgnoreValue(svnCommand.SvnExecutableFilePath, path, svnCommand.Logger);
        }

        public static void SetSvnIgnoreValue(this SvnCommand svnCommand, AbsolutePath path, string ignoreValue)
        {
            SvnCommandServicesProvider.SetSvnIgnoreValue(svnCommand.SvnExecutableFilePath, path, ignoreValue, svnCommand.Logger);
        }

        public static Version GetVersion(this SvnCommand svnCommand)
        {
            var version = SvnCommandServicesProvider.GetVersion(svnCommand.SvnExecutableFilePath, svnCommand.Logger);
            return version;
        }
    }
}
