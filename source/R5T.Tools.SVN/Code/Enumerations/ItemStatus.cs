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
        /// Indicates an item is either in an ignored or unversioned directory.
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
