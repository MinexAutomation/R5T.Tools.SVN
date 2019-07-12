using System;


namespace R5T.Tools.SVN
{
    /// <summary>
    /// Represents the SVN working copy status of file or directory.
    /// </summary>
    public enum ItemStatus
    {
        None = 0,

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
        Unversioned,
    }
}
