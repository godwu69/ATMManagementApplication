using System.ComponentModel.DataAnnotations;

namespace ATMManagementApplication.Models
{
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }
        public required string Name { get; set; }

        [Required]
        public required string Password { get; set; }

        [Required]
        public required string Email { get; set; }

        public decimal Balance { get; set; }
    }
}