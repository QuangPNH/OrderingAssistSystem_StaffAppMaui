namespace AzzanOrder.Data.Models
{
    public class NotiChange
    {
        public int id { get; set; }
        public string tableName { get; set; }
        public string message { get; set; }
        public bool isSent { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;
    }
}
