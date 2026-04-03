using System.ComponentModel.DataAnnotations;

namespace QuickServe.Models
{
    public class ServiceRequest
    {
        [Key]
        public int RequestId { get; set; }
        public int UserId { get; set; }
        public string ServiceType { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
