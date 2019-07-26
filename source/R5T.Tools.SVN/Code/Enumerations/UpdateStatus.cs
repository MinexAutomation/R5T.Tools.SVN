using System;


namespace R5T.Tools.SVN
{
    /// <summary>
    /// Status of file system entry for SVN update and checkout commands.
    /// </summary>
    public enum UpdateStatus
    {
        None,

        Added,
        Conflict,
        Deleted,
        Existed,
        Merged,
        Replaced,
        Updated,
    }
}
