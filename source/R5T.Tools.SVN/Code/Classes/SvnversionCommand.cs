using System;

using Microsoft.Extensions.Logging;

using R5T.NetStandard.IO.Paths;


namespace R5T.Tools.SVN
{
    public class SvnversionCommand
    {
        public FilePath SvnversionFilePath { get; }
        public ILogger Logger { get; }


        public SvnversionCommand(FilePath svnversionFilePath, ILogger<SvnversionCommand> logger)
        {
            this.SvnversionFilePath = svnversionFilePath;
            this.Logger = logger;
        }
    }
}
