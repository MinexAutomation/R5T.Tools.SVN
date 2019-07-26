using System;

using R5T.NetStandard.IO.Paths;


namespace R5T.Tools.SVN
{
    public class SvnPathStatus
    {
        public AbsolutePath Path { get; set; }
        public ItemStatus ItemStatus { get; set; }
    }
}
