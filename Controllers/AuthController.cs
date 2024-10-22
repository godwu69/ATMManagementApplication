using Microsoft.AspNetCore.Mvc;
using ATMManagementApplication.Models;
using ATMManagementApplication.Data;
using System.Linq;

namespace ATMManagementApplication.Controllers{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase{
        private readonly ATMContext _context;
        public AuthController(ATMContext context){
            _context = context;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] Customer login){
            var customer = _context.Customers.FirstOrDefault(c => c.Email == login.Email && c.Password == login.Password);
            if(customer == null){
                return Unauthorized("Invalid credentials");
            }
            return Ok(new {message = "Login successful", customerId = customer.CustomerId});
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request){
            var existCustomer = _context.Customers.FirstOrDefault(c => c.Email == request.Email);
            if (existCustomer != null) return NotFound("This email is already in use. Please select another one");

            var newCustomer = new Customer{
                Name = request.Name,
                Password = request.Password,
                Email = request.Email,
                Balance = 0
            };
            _context.Customers.Add(newCustomer);
            _context.SaveChanges();


            return Ok(new {message = "Register successful", customerId = newCustomer.CustomerId});
        }

        [HttpPut("change-password")]
        public IActionResult ChangePassword([FromBody] ChangePasswordRequest request){
            var customer = _context.Customers.FirstOrDefault(c => c.Email == request.Email);
            if (customer == null) return NotFound("Customer not found");
            if (string.IsNullOrEmpty(request.OldPassword)) return BadRequest("Old password cannot be null or empty");
            if (string.IsNullOrEmpty(request.NewPassword)) return BadRequest("New password cannot be null or empty");
            if (customer.Password != request.OldPassword) return BadRequest("Old password is incorrect");

            customer.Password = request.NewPassword;
            _context.SaveChanges();


            return Ok(new {message = "Password changed successfully"});
        }
    }

    public class LoginRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }

    }

    public class RegisterRequest
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class ChangePasswordRequest
    {
        public required string Email { get; set; }
        public required string OldPassword { get; set; }
        public required string NewPassword { get; set; }
    }

}