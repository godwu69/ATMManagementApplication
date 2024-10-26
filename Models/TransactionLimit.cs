using System.ComponentModel.DataAnnotations;

namespace ATMManagementApplication.Models
{
    public class TransactionLimit
    {
        [Key]
        public int LimitId { get; set; }
        
        [Required]
        public required string TransactionType { get; set; }
        
        [Required]
        public decimal DailyLimit { get; set; }
        
        [Required]
        public decimal SingleTransactionLimit { get; set; }
    }
}
