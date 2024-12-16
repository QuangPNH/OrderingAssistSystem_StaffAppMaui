namespace AzzanOrder.Data.Models
{
    public class StaffNotiChannel
    {
        public int id { get; set; }
        public string? TableQR { get; set; }
        public int ManagerId { get; set; }
        public string? Message { get; set; }
        public DateTime DateAdded { get; set; }
        public bool IsSent { get; set; }

    }
}
