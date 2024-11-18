using System;
using System.Collections.Generic;

namespace OrderingAssistSystem_StaffApp.Models
{
    public partial class OrderDetail
    {
        public int OrderDetailId { get; set; }
        public int? Quantity { get; set; }
        public int? MenuItemId { get; set; }
        public int? OrderId { get; set; }
        public bool? Status { get; set; }
        public string? Description { get; set; }
		public string? Sugar { get; set; }
		public string? Ice { get; set; }
		public string? Topping { get; set; }

		public virtual MenuItem? MenuItem { get; set; }
        public virtual Order? Order { get; set; }
    }
}
