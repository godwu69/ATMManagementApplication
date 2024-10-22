using System.ComponentModel.DataAnnotations;

namespace ATMManagementApplication.Models{
    public class TransactionHistory{
        [Key]
        public int TransactionHistoryId { get; set; }
        public int CustomerId { get; set; }
        public int TransactionId { get; set; }
        public required string TransactionType { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsSuccessful { get; set; }
    }
}