using System;

using Microsoft.Extensions.Logging;

using R5T.NetStandard.IO.Paths;


namespace R5T.Tools.SVN
{
    public class SvnCommand
    {
        public FilePath SvnExecutableFilePath { get; }
        public ILogger Logger { get; }


        public SvnCommand(FilePath svnExecutableFilePath, ILogger<SvnCommand> logger)
        {
            this.SvnExecutableFilePath = svnExecutableFilePath;
            this.Logger = logger;
        }
    }
}
