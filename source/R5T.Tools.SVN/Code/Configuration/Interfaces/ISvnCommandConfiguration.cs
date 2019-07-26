using System;

using R5T.NetStandard.IO.Paths;


namespace R5T.Tools.SVN.Configuration
{
    public interface ISvnCommandConfiguration
    {
        FilePath SvnExecutableFilePath { get; }
    }
}
