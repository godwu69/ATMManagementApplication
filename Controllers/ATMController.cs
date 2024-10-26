using Microsoft.AspNetCore.Mvc;
using ATMManagementApplication.Models;
using ATMManagementApplication.Data;
using System.Linq;
using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace ATMManagementApplication.Controllers
{
    [ApiController]
    [Route("api/atm")]
    [Authorize]
    public class ATMController : ControllerBase
    {
        private readonly ATMContext _context;
        private readonly OtpService _otpService;
        private const decimal TransactionFeePercentage = 0.02m;
        private const decimal MonthlyInterestRate = 0.005m;
        public ATMController(ATMContext context, OtpService otpService)
        {
            _context = context;
            _otpService = otpService;
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
                return Ok(new { balance = customer.Balance, name = customer.Name });
            }
        }

        [HttpGet("transaction/{customerId}")]
        public IActionResult GetTransactionHistory(int customerId)
        {
            var customer = _context.Customers.Find(customerId);
            if (customer == null)
            {
                return NotFound("Customer not found");
            }
            var transactionHistory = _context.Transactions
                                              .Where(th => th.CustomerId == customerId)
                                              .OrderByDescending(th => th.Timestamp)
                                              .Select(th => new
                                              {
                                                  th.TransactionType,
                                                  th.Amount,
                                                  th.Timestamp,
                                                  th.IsSuccessful
                                              }).ToList();

            return Ok(transactionHistory);
        }

        [HttpPost("withdraw")]
        public IActionResult Withdraw([FromBody] WithdrawRequest request)
        {
            var customer = _context.Customers.Find(request.CustomerId);
            if (customer == null) return NotFound(new { message = "Customer not found" });

            if (!_otpService.ValidateOtp(customer.CustomerId, request.otp))
            {
                return BadRequest(new { message = "Invalid OTP" });
            }

            var limit = _context.TransactionLimits.FirstOrDefault(l => l.TransactionType == "Withdraw");
            if (limit == null) return StatusCode(500, "Transaction limit not set for withdrawals.");

            decimal transactionFee = request.Amount * TransactionFeePercentage;
            decimal totalAmount = request.Amount + transactionFee;

            if (totalAmount > limit.SingleTransactionLimit)
            {
                return BadRequest(new { message = $"Exceeds single transaction limit of {limit.SingleTransactionLimit}" });
            }

            var totalDailyWithdrawn = _context.Transactions
                .Where(t => t.CustomerId == request.CustomerId && t.TransactionType == "Withdraw" && t.Timestamp.Date == DateTime.Now.Date)
                .Sum(t => t.Amount);

            if (totalDailyWithdrawn + totalAmount > limit.DailyLimit)
            {
                return BadRequest(new { message = $"Exceeds daily withdrawal limit of {limit.DailyLimit}" });
            }
            if (customer.Balance < totalAmount) return BadRequest(new { message = "Insufficient balance" });

            customer.Balance -= totalAmount;

            var transaction = CreateTransaction(request.CustomerId, "Withdraw", request.Amount);
            _context.Transactions.Add(transaction);

            _context.SaveChanges();

            SendEmail(customer.Email, "Withdraw Confirmation", $"Dear {customer.Name}, you have successfully withdrawn {request.Amount} (fee: {transactionFee}). Your new balance is {customer.Balance}.");

            _otpService.ClearOtp(customer.CustomerId);

            return Ok(new { message = "Withdraw successful", newBalance = customer.Balance });
        }




        [HttpPost("deposit")]
        public IActionResult Deposit([FromBody] DepositRequest request)
        {
            var customer = _context.Customers.Find(request.CustomerId);
            if (customer == null) return NotFound(new { message = "Customer not found" });

            if (!_otpService.ValidateOtp(customer.CustomerId, request.otp))
            {
                return BadRequest(new { message = "Invalid OTP" });
            }

            if (request.Amount <= 0) return BadRequest(new { message = "Deposit amount should be greater than 0" });


            customer.Balance += request.Amount;

            var transaction = CreateTransaction(request.CustomerId, "Deposit", request.Amount);
            _context.Transactions.Add(transaction);


            _context.SaveChanges();

            SendEmail(customer.Email, "Deposit Confirmation", $"Dear {customer.Name}, you have successfully deposit {request.Amount}. Your new balance is {customer.Balance}.");

            _otpService.ClearOtp(customer.CustomerId);

            return Ok(new { message = "Deposit successful", newBalance = customer.Balance });

        }

        [HttpPost("transfer")]
        public IActionResult Transfer([FromBody] TransferRequest request)
        {
            var sendCustomer = _context.Customers.Find(request.SendId);
            var receiveCustomer = _context.Customers.Find(request.ReceiveId);

            if (sendCustomer == null) return NotFound(new { message = "Send customer not found" });
            if (receiveCustomer == null) return NotFound(new { message = "Receive customer not found" });

            if (!_otpService.ValidateOtp(sendCustomer.CustomerId, request.otp))
            {
                return BadRequest(new { message = "Invalid OTP" });
            }

            var limit = _context.TransactionLimits.FirstOrDefault(l => l.TransactionType == "Transfer");
            if (limit == null) return StatusCode(500, "Transaction limit not set for transfers.");

            decimal transactionFee = request.Amount * TransactionFeePercentage;
            decimal totalAmount = request.Amount + transactionFee;

            if (totalAmount > limit.SingleTransactionLimit)
            {
                return BadRequest(new { message = $"Exceeds single transaction limit of {limit.SingleTransactionLimit}" });
            }

            var totalDailyTransfer = _context.Transactions
                .Where(t => t.CustomerId == request.SendId && t.TransactionType == "Transfer" && t.Timestamp.Date == DateTime.Now.Date)
                .Sum(t => t.Amount);

            if (totalDailyTransfer + totalAmount > limit.DailyLimit)
            {
                return BadRequest(new { message = $"Exceeds daily withdrawal limit of {limit.DailyLimit}" });
            }

            if (totalAmount > sendCustomer.Balance) return BadRequest(new { message = "Insufficient balance" });
            if (request.Amount <= 0) return BadRequest(new { message = "Transfer amount should be greater than 0" });

            sendCustomer.Balance -= totalAmount;
            receiveCustomer.Balance += request.Amount;

            var sendTransaction = CreateTransaction(request.SendId, "Send", request.Amount);
            var receiveTransaction = CreateTransaction(request.ReceiveId, "Receive", request.Amount);
            _context.Transactions.AddRange(sendTransaction, receiveTransaction);

            _context.SaveChanges();

            SendEmail(sendCustomer.Email, "Transfer Confirmation", $"Dear {sendCustomer.Name}, you have successfully sent {request.Amount} (fee: {transactionFee}) to {receiveCustomer.Name}. Your new balance is {sendCustomer.Balance}.");
            SendEmail(receiveCustomer.Email, "Transfer Confirmation", $"Dear {receiveCustomer.Name}, you have successfully received {request.Amount} from {sendCustomer.Name}. Your new balance is {receiveCustomer.Balance}.");

            _otpService.ClearOtp(sendCustomer.CustomerId);

            return Ok(new { message = "Transfer successful" });
        }


        [HttpPost("request-otp")]
        public IActionResult RequestOtp([FromBody] GetCodeRequest request)
        {
            var customer = _context.Customers.Find(request.CustomerId);
            if (customer == null) return NotFound(new { message = "Customer not found" });

            string otp = _otpService.GenerateOtp(request.CustomerId);
            SendEmail(customer.Email, "Your OTP Code", $"Your OTP for the transaction is: {otp}");

            return Ok(new { message = "OTP sent to your email." });
        }


        private Transaction CreateTransaction(int customerId, string transactionType, decimal amount)
        {
            return new Transaction
            {
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
        public required string otp { get; set; }
    }

    public class DepositRequest
    {
        public int CustomerId { get; set; }
        public decimal Amount { get; set; }
        public required string otp { get; set; }
    }

    public class TransferRequest
    {
        public int SendId { get; set; }
        public int ReceiveId { get; set; }
        public decimal Amount { get; set; }
        public required string otp { get; set; }
    }

    public class GetCodeRequest
    {
        public int CustomerId { get; set; }
    }

    public class OtpService
    {
        private readonly Dictionary<int, string> _otpStore = new Dictionary<int, string>();
        private readonly Random _random = new Random();

        public string GenerateOtp(int customerId)
        {
            string otp = _random.Next(100000, 999999).ToString();
            _otpStore[customerId] = otp;
            return otp;
        }

        public bool ValidateOtp(int customerId, string otp)
        {
            if (_otpStore.TryGetValue(customerId, out var storedOtp))
            {
                return storedOtp == otp;
            }
            return false;
        }

        public void ClearOtp(int customerId)
        {
            _otpStore.Remove(customerId);
        }
    }

}