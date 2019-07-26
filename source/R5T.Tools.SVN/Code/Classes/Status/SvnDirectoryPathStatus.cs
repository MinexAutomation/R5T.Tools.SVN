using System;

using R5T.NetStandard.IO.Paths;


namespace R5T.Tools.SVN
{
    public class SvnDirectoryPathStatus
    {
        public DirectoryPath DirectoryPath { get; set; }
        public ItemStatus ItemStatus { get; set; }
    }
}
