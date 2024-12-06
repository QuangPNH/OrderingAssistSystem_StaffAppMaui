using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace OrderingAssistSystem_StaffApp.Models
{
    public partial class MenuItem
    {

		

		private MenuItem()
        {
            MenuCategories = new HashSet<MenuCategory>();
            OrderDetails = new HashSet<OrderDetail>();
            AvailableToppings = new ObservableCollection<MenuItem>();
            Sugar = "Normal";
            Ice = "Normal";
            Quantity = 1;
        }

        public int MenuItemId { get; set; }
        public string? ItemName { get; set; }
        public double? Price { get; set; }
        public string? Description { get; set; }
        public double? Discount { get; set; }
        public string? Image { get; set; }
        public bool? IsAvailable { get; set; }
        public int? EmployeeId { get; set; }


		// Cart
		[JsonIgnore]
		public int? Quantity { get; set; }
		[JsonIgnore]
		public string? Sugar { get; set; }
		[JsonIgnore]
		public string? Ice { get; set; }
		[JsonIgnore]
		public ObservableCollection<MenuItem> AvailableToppings { get; set; }
        [JsonIgnore]
        public bool IsSelected { get; set; } // Add this property to track selection

        public virtual Employee? Employee { get; set; }
        public virtual ICollection<MenuCategory> MenuCategories { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    }
}