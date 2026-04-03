using System.ComponentModel.DataAnnotations;

namespace QuickServe.Models
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }
        public int WalletId { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
