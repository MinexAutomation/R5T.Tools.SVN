using System;

using Microsoft.Extensions.Logging;

using R5T.NetStandard.IO.Paths;

using R5T.Tools.SVN.Configuration;


namespace R5T.Tools.SVN
{
    public class SvnCommand
    {
        public const string NonValue = null;
        public const string SvnIgnorePropertyName = "svn:ignore";


        #region Static

        public static bool IsValue(string value)
        {
            var output = value != SvnCommand.NonValue;
            return output;
        }

        #endregion


        public FilePath SvnExecutableFilePath { get; }
        public ILogger Logger { get; }


        public SvnCommand(FilePath svnExecutableFilePath, ILogger<SvnCommand> logger)
        {
            this.SvnExecutableFilePath = svnExecutableFilePath;
            this.Logger = logger;
        }

        public SvnCommand(ISvnCommandConfiguration svnCommandConfiguration, ILogger<SvnCommand> logger)
            : this(svnCommandConfiguration.SvnExecutableFilePath, logger)
        {
        }
    }
}
