using System;


namespace R5T.Tools.SVN
{
    /// <summary>
    /// Represents the SVN working copy status of file or directory.
    /// </summary>
    public enum ItemStatus
    {
        /// <summary>
        /// Does not exist.
        /// </summary>
        None = 0,
        /// <summary>
        /// An item that is outside of a working copy directory hierarchy.
        /// </summary>
        NotWorkingCopy,
        /// <summary>
        /// An ambiguous status indicating an item is either in:
        /// 1) an ignored directory (<see cref="ItemStatus.Ignored"/>) or 
        /// 2) an unversioned directory (<see cref="ItemStatus.Unversioned"/>) within the working copy hierarchy.
        /// Extra work is required to resolve which <see cref="ItemStatus"/> is actually the case.
        /// </summary>
        NotFound,

        NoModifications, // a.k.a. "normal"

        Added,
        Conflicted,
        Deleted,
        External,
        Ignored,
        Incomplete,
        Merged,
        Missing,
        Modified,
        Obstructed,
        Replaced,
        /// <summary>
        /// Not under source control.
        /// </summary>
        Unversioned,
    }
}
