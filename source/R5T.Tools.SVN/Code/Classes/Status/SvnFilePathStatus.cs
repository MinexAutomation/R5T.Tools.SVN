using System;

using R5T.NetStandard.IO.Paths;


namespace R5T.Tools.SVN
{
    public class SvnFilePathStatus
    {
        public FilePath FilePath { get; set; }
        public ItemStatus ItemStatus { get; set; }
    }
}
