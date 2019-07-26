using System;

using R5T.NetStandard;


namespace R5T.Tools.SVN
{
    public static class Utilities
    {
        public static UpdateStatus ToUpdateStatus(string updateStatusReportingCharacter)
        {
            switch(updateStatusReportingCharacter)
            {
                case Constants.AddedUpdateStatus:
                    return UpdateStatus.Added;

                case Constants.ConflictUpdateStatus:
                    return UpdateStatus.Conflict;

                case Constants.DeletedUpdateStatus:
                    return UpdateStatus.Deleted;

                case Constants.ExistedUpdateStatus:
                    return UpdateStatus.Existed;

                case Constants.MergedUpdateStatus:
                    return UpdateStatus.Merged;

                case Constants.ReplacedUpdateStatus:
                    return UpdateStatus.Replaced;

                case Constants.UpdatedUpdateStatus:
                    return UpdateStatus.Updated;

                default:
                    throw new Exception(EnumHelper.UnrecognizedEnumerationValueMessage<UpdateStatus>(updateStatusReportingCharacter));
            }
        }
    }
}
