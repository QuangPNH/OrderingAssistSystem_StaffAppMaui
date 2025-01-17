using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace OrderingAssistSystem_StaffApp.Models
{
	public partial class OrderDetail : INotifyPropertyChanged
	{
		public int OrderDetailId { get; set; }
		public int? Quantity { get; set; }
		public int? MenuItemId { get; set; }
		public int? OrderId { get; set; }
		public bool? Status { get; set; }
		public string? Description { get; set; }
		public int? FinishedItem { get; set; }
		[JsonIgnore]
		public string? Sugar { get; set; }
		[JsonIgnore]
		public string? Ice { get; set; }
		[JsonIgnore]
		public string? Topping { get; set; }
		[JsonIgnore]
		public bool IsCurrentItem { get; set; }
		[JsonIgnore]
		private bool _isStartEnabled;
		[JsonIgnore]
		private string _statusText;
		[JsonIgnore]
		public bool IsStartEnabled
		{
			get => _isStartEnabled;
			set
			{
				if (_isStartEnabled != value)
				{
					_isStartEnabled = value;
					OnPropertyChanged();
				}
			}
		}
		[JsonIgnore]
		public string StatusText
		{
			get => _statusText;
			set
			{
				if (_statusText != value)
				{
					_statusText = value;
					OnPropertyChanged();
				}
			}
		}
		[JsonIgnore]
		public DateTime? EarliestTime { get; set; }
		[JsonIgnore]
		public DateTime? LatestTime { get; set; }
		public virtual MenuItem? MenuItem { get; set; }
		public virtual Order? Order { get; set; }

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
