using System;

using R5T.NetStandard;


namespace R5T.Tools.SVN.XML
{
    public static class WorkingCopyItemTypeExtensions
    {
        public static ItemStatus ToItemStatus(this WorkingCopyItemType workingCopyItemType)
        {
            switch(workingCopyItemType)
            {
                case WorkingCopyItemType.added:
                    return ItemStatus.Added;

                case WorkingCopyItemType.conflicted:
                    return ItemStatus.Conflicted;

                case WorkingCopyItemType.deleted:
                    return ItemStatus.Deleted;

                case WorkingCopyItemType.external:
                    return ItemStatus.External;

                case WorkingCopyItemType.ignored:
                    return ItemStatus.Ignored;

                case WorkingCopyItemType.incomplete:
                    return ItemStatus.Incomplete;

                case WorkingCopyItemType.merged:
                    return ItemStatus.Merged;

                case WorkingCopyItemType.missing:
                    return ItemStatus.Missing;

                case WorkingCopyItemType.modified:
                    return ItemStatus.Modified;

                case WorkingCopyItemType.none:
                    return ItemStatus.None;

                case WorkingCopyItemType.normal:
                    return ItemStatus.NoModifications;

                case WorkingCopyItemType.obstructed:
                    return ItemStatus.Obstructed;
                   
                case WorkingCopyItemType.replaced:
                    return ItemStatus.Replaced;

                case WorkingCopyItemType.unversioned:
                    return ItemStatus.Unversioned;

                default:
                    throw new ArgumentException(EnumHelper.UnexpectedEnumerationValueMessage(workingCopyItemType), nameof(workingCopyItemType));
            }
        }
    }
}
