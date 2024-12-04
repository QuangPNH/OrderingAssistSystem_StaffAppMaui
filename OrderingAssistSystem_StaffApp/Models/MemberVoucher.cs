using System;
using System.Collections.Generic;

namespace OrderingAssistSystem_StaffApp.Models
{
    public partial class MemberVoucher
    {
        public int MemberVoucherId { get; set; }
        public int? MemberId { get; set; }
        public int? VoucherDetailId { get; set; }
        public int? OrderId { get; set; }
        public bool? IsActive { get; set; }
        public int? Quantity { get; set; }

        public virtual Member? Member { get; set; }
        public virtual Order? Order { get; set; }
        public virtual VoucherDetail? VoucherDetail { get; set; }
    }
}