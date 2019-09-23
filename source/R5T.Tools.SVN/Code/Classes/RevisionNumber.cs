using System;


namespace R5T.Tools.SVN
{
    public class RevisionNumber
    {
        public const int Invalid = -1;


        #region Static

        public static bool IsInvalid(int revisionNumber)
        {
            var output = revisionNumber == RevisionNumber.Invalid;
            return output;
        }

        #endregion
    }
}
