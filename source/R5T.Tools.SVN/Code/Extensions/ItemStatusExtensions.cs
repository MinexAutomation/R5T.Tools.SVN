using System;

using R5T.NetStandard;
using R5T.Tools.SVN.XML;


namespace R5T.Tools.SVN
{
    public static class ItemStatusExtensions
    {
        public static WorkingCopyItemType ToWorkingCopyItemType(this ItemStatus itemStatus)
        {
            switch(itemStatus)
            {
                case ItemStatus.Added:
                    return WorkingCopyItemType.added;

                case ItemStatus.Conflicted:
                    return WorkingCopyItemType.conflicted;

                case ItemStatus.Deleted:
                    return WorkingCopyItemType.deleted;

                case ItemStatus.External:
                    return WorkingCopyItemType.external;

                case ItemStatus.Ignored:
                    return WorkingCopyItemType.ignored;

                case ItemStatus.Incomplete:
                    return WorkingCopyItemType.incomplete;

                case ItemStatus.Merged:
                    return WorkingCopyItemType.merged;

                case ItemStatus.Missing:
                    return WorkingCopyItemType.missing;

                case ItemStatus.Modified:
                    return WorkingCopyItemType.modified;

                case ItemStatus.NoModifications:
                    return WorkingCopyItemType.normal;

                case ItemStatus.None:
                    return WorkingCopyItemType.none;

                case ItemStatus.Obstructed:
                    return WorkingCopyItemType.obstructed;

                case ItemStatus.Replaced:
                    return WorkingCopyItemType.replaced;

                case ItemStatus.Unversioned:
                    return WorkingCopyItemType.unversioned;

                default:
                    throw new ArgumentException(EnumHelper.UnexpectedEnumerationValueMessage(itemStatus), nameof(itemStatus));
            }
        }
    }
}
