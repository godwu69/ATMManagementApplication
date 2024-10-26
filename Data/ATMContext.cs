using Microsoft.EntityFrameworkCore;
using ATMManagementApplication.Models;

namespace ATMManagementApplication.Data{
    public class ATMContext : DbContext{
        public ATMContext(DbContextOptions<ATMContext> options):base(options){}
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionLimit> TransactionLimits { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TransactionLimit>().HasData(
                new TransactionLimit { LimitId = 1, TransactionType = "Withdraw", DailyLimit = 100000, SingleTransactionLimit = 10000 },
                new TransactionLimit { LimitId = 2, TransactionType = "Deposit", DailyLimit = 100000, SingleTransactionLimit = 10000 },
                new TransactionLimit { LimitId = 3, TransactionType = "Transfer", DailyLimit = 80000, SingleTransactionLimit = 8000 }
            );
        }
    }
}