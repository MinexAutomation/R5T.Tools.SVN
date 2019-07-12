using System;


namespace R5T.Tools.SVN
{
    public enum PropertiesStatus
    {
        None = 0,

        NoModifications, // a.k.a. "normal"
        Conflicted,
        Modified,
    }
}
