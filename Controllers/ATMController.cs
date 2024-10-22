using Microsoft.AspNetCore.Mvc;
using ATMManagementApplication.Models;
using ATMManagementApplication.Data;
using System.Linq;
using System.Net.Mail;
using System.Net;

namespace ATMManagementApplication.Controllers
{
    [ApiController]
    [Route("api/atm")]
    public class ATMController : ControllerBase
    {
        private readonly ATMContext _context;
        public ATMController(ATMContext context)
        {
            _context = context;
        }

        [HttpGet("balance/{customerId}")]
        public IActionResult GetBalance(int customerId)
        {
            var customer = _context.Customers.Find(customerId);
            if (customer == null)
            {
                return NotFound("Customer not found");
            }
            else
            {
                return Ok(new { balance = customer.Balance });
            }
        }

        [HttpGet("transaction/{customerId}")]
        public IActionResult GetTranscationHistory(int customerId)
        {
            var customer = _context.Customers.Find(customerId);
            if (customer == null)
            {
                return NotFound("Customer not found");
            }
            var transactionHistory = _context.TransactionHistories.Where(th => th.CustomerId == customerId).OrderByDescending(th => th.Timestamp).ToList();
            return Ok(transactionHistory);
        }

        [HttpPost("withdraw")]
        public IActionResult Withdraw([FromBody] WithdrawRequest request)
        {
            var customer = _context.Customers.Find(request.CustomerId);
            if (customer == null) return NotFound("Customer not found");
            if (customer.Balance < request.Amount) return BadRequest("Insufficient balance");

            customer.Balance -= request.Amount;

            var transaction = CreateTransaction(request.CustomerId, request.Amount);
            _context.Transactions.Add(transaction);

            var transactionHistory = CreateTransactionHistory(transaction.TransactionId, request.CustomerId, "Withdraw", request.Amount);
            _context.TransactionHistories.Add(transactionHistory);

            _context.SaveChanges();

            SendEmail(customer.Email, "Withdraw Confirmation", $"Dear {customer.Name}, you have successfully withdrawn {request.Amount}. Your new balance is {customer.Balance}.");

            return Ok(new { message = "Withdraw successful", newBalance = customer.Balance });

        }

        [HttpPost("deposit")]
        public IActionResult Deposit([FromBody] DepositRequest request)
        {
            var customer = _context.Customers.Find(request.CustomerId);
            if (customer == null) return NotFound("Customer not found");
            if (request.Amount <= 0) return BadRequest("Deposit amount should be greater than 0");

            customer.Balance += request.Amount;

            var transaction = CreateTransaction(request.CustomerId, request.Amount);
            _context.Transactions.Add(transaction);

            var transactionHistory = CreateTransactionHistory(transaction.TransactionId, request.CustomerId, "Deposit", request.Amount);
            _context.TransactionHistories.Add(transactionHistory);

            _context.SaveChanges();

            SendEmail(customer.Email, "Deposit Confirmation", $"Dear {customer.Name}, you have successfully deposit {request.Amount}. Your new balance is {customer.Balance}.");

            return Ok(new { message = "Deposit successful", newBalance = customer.Balance });

        }

        [HttpPost("transfer")]
        public IActionResult Transfer([FromBody] TransferRequest request)
        {
            var sendCustomer = _context.Customers.Find(request.SendId);
            var receiveCustomer = _context.Customers.Find(request.ReceiveId);

            if (sendCustomer == null) return NotFound("Send customer not found");
            if (receiveCustomer == null) return NotFound("Receive customer not found");
            if (request.Amount > sendCustomer.Balance) return BadRequest("Insufficient balance");
            if (request.Amount <= 0) return BadRequest("Transfer amount should be greater than 0");

            sendCustomer.Balance -= request.Amount;
            receiveCustomer.Balance += request.Amount;

            var sendTransaction = CreateTransaction(request.SendId, request.Amount);
            var receiveTransaction = CreateTransaction(request.ReceiveId, request.Amount);
            _context.Transactions.AddRange(sendTransaction, receiveTransaction);

            var sendTransactionHistory = CreateTransactionHistory(sendTransaction.TransactionId, request.SendId, "Send", request.Amount);
            var receiveTransactionHistory = CreateTransactionHistory(receiveTransaction.TransactionId, request.ReceiveId, "Receive", request.Amount);
            _context.TransactionHistories.AddRange(sendTransactionHistory, receiveTransactionHistory);

            _context.SaveChanges();

            SendEmail(sendCustomer.Email, "Transfer Confirmation", $"Dear {sendCustomer.Name}, you have successfully send {request.Amount} to {receiveCustomer.Name}. Your new balance is {sendCustomer.Balance}.");
            SendEmail(receiveCustomer.Email, "Transfer Confirmation", $"Dear {receiveCustomer.Name}, you have successfully receive {request.Amount} from {sendCustomer.Name}. Your new balance is {receiveCustomer.Balance}.");

            return Ok(new { message = "Transfer successful" });
        }

        private Transaction CreateTransaction(int customerId, decimal amount)
        {
            return new Transaction
            {
                CustomerId = customerId,
                Amount = amount,
                Timestamp = DateTime.Now,
                IsSuccessful = true
            };
        }

        private TransactionHistory CreateTransactionHistory(int transactionId, int customerId, string transactionType, decimal amount)
        {
            return new TransactionHistory
            {
                TransactionId = transactionId,
                CustomerId = customerId,
                TransactionType = transactionType,
                Amount = amount,
                Timestamp = DateTime.Now,
                IsSuccessful = true
            };
        }

        private void SendEmail(string toEmail, string subject, string body)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("hoangntth2306017@fpt.edu.vn", "ieof czsw qicc awcs"),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("hoangntth2306017@fpt.edu.vn"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(toEmail);

            smtpClient.Send(mailMessage);
        }

    }
    public class WithdrawRequest
    {
        public int CustomerId { get; set; }
        public decimal Amount { get; set; }
    }

    public class DepositRequest
    {
        public int CustomerId { get; set; }
        public decimal Amount { get; set; }
    }

    public class TransferRequest
    {
        public int SendId { get; set; }
        public int ReceiveId { get; set; }
        public decimal Amount { get; set; }
    }
}