using System;


namespace R5T.Tools.SVN
{
    public class EntryUpdateStatus
    {
        public UpdateStatus UpdateStatus { get; set; }
        /// <summary>
        /// Unsure of what this path is relative to.
        /// </summary>
        public string RelativePath { get; set; }
    }
}
