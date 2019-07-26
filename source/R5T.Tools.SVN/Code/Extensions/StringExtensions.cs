using System;


namespace R5T.Tools.SVN.Extensions
{
    public static class StringExtensions
    {
        public static UpdateStatus ToUpdateStatus(this string value)
        {
            var updateStatus = Utilities.ToUpdateStatus(value);
            return updateStatus;
        }
    }
}
