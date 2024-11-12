using System;
using System.Collections.Generic;

namespace OrderingAssistSystem_StaffApp.Models
{
    public partial class Table
    {
        public Table()
        {
            Orders = new HashSet<Order>();
        }

        public int TableId { get; set; }
        public string? Qr { get; set; }
        public bool? Status { get; set; }
        public int? EmployeeId { get; set; }

        public virtual ICollection<Order> Orders { get; set; }
    }
}
