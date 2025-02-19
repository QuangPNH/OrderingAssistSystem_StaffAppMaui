﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace OrderingAssistSystem_StaffApp.Models
{
    public partial class Order
    {
        public Order()
        {
            MemberVouchers = new HashSet<MemberVoucher>();
            OrderDetails = new HashSet<OrderDetail>();
        }

        public int OrderId { get; set; }
        public DateTime? OrderDate { get; set; }
        public int? TableId { get; set; }
        public double? Cost { get; set; }
        public double? Tax { get; set; }
        public int? MemberId { get; set; }
        public bool? Status { get; set; }

		public virtual Member? Member { get; set; }
        public virtual Table? Table { get; set; }
        public virtual ICollection<MemberVoucher> MemberVouchers { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    }
}

