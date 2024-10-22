using Microsoft.EntityFrameworkCore;
using ATMManagementApplication.Models;

namespace ATMManagementApplication.Data{
    public class ATMContext : DbContext{
        public ATMContext(DbContextOptions<ATMContext> options):base(options){}
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionHistory> TransactionHistories { get; set; }
    }
}