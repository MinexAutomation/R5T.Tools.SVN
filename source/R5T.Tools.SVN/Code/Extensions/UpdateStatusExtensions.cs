using System;

using R5T.NetStandard;


namespace R5T.Tools.SVN
{
    public static class UpdateStatusExtensions
    {
        public static string ToStringReportingCharacter(this UpdateStatus updateStatus)
        {
            switch(updateStatus)
            {
                case UpdateStatus.Added:
                    return Constants.AddedUpdateStatus;

                case UpdateStatus.Conflict:
                    return Constants.ConflictUpdateStatus;

                case UpdateStatus.Deleted:
                    return Constants.DeletedUpdateStatus;

                case UpdateStatus.Existed:
                    return Constants.ExistedUpdateStatus;

                case UpdateStatus.Merged:
                    return Constants.MergedUpdateStatus;

                case UpdateStatus.Replaced:
                    return Constants.ReplacedUpdateStatus;

                case UpdateStatus.Updated:
                    return Constants.UpdatedUpdateStatus;

                default:
                    throw new Exception(EnumHelper.UnexpectedEnumerationValueMessage(updateStatus));
            }
        }
    }
}
