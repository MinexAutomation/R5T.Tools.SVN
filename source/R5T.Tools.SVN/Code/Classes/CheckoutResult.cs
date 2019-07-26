using System;


namespace R5T.Tools.SVN
{
    public class CheckoutResult
    {
        public int RevisionNumber { get; set; }
        public EntryUpdateStatus[] Statuses { get; set; }
    }
}
