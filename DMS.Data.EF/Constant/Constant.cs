using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMS.Data.EF.Constant
{
    public static class DMStatusConstant
    {
        public const long STATUS_NEW = 10;
        public const long STATUS_DRAFT = 100;
        public const long STATUS_SUPT_APPROVAL = 110;
        public const long STATUS_MGR_APPROVAL = 120;
        public const long STATUS_ADDL_MGR_APPROVAL = 130;
        public const long STATUS_GEN_MGR_APPROVAL = 140;
        public const long STATUS_RELEASE = 150;
    }

    public static class ManualNotificationType
    {
        public const string ADD = "Add";
        public const string EDIT = "Edit";
        public const string DELETE = "Delete";
        public const string APPROVE = "Approve";
        public const string PUBLISH = "Publish";
        public const string ASSIGNRIGHTS = "AssignRights";
        public const string FAVOURITE = "Favourite";
    }
}
