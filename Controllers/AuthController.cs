using Microsoft.AspNetCore.Mvc;
using ATMManagementApplication.Models;
using ATMManagementApplication.Data;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ATMManagementApplication.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [Authorize]
    public class AuthController : ControllerBase
    {
        private readonly ATMContext _context;
        private readonly IConfiguration _configuration;
        public AuthController(ATMContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        private string GenerateJwtToken(Customer customer)
        {
            var key = _configuration.GetValue<string>("Jwt:Key");
            var issuer = _configuration.GetValue<string>("Jwt:Issuer");
            var audience = _configuration.GetValue<string>("Jwt:Audience");
            var expiryDuration = _configuration.GetValue<int>("Jwt:ExpiryDuration");

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, customer.Name),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var keyBytes = Encoding.UTF8.GetBytes(key);
            if (keyBytes.Length < 32)
            {
                throw new ArgumentException("The JWT key must be at least 256 bits (32 bytes) long.");
            }

            var credentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(expiryDuration),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest login)
        {
            var customer = _context.Customers.FirstOrDefault(c => c.Email == login.Email && c.Password == login.Password);
            if (customer == null)
            {
                return Unauthorized(new {message = "Invalid credentials" });
            }

            var token = GenerateJwtToken(customer);

            return Ok(new { message = "Login successful", customerId = customer.CustomerId, email = customer.Email, token });
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            var existCustomer = _context.Customers.FirstOrDefault(c => c.Email == request.Email);
            if (existCustomer != null) return NotFound(new {message = "This email is already in use. Please select another one"});

            var newCustomer = new Customer
            {
                Name = request.Name,
                Password = request.Password,
                Email = request.Email,
                Balance = 999999999
            };
            _context.Customers.Add(newCustomer);
            _context.SaveChanges();


            return Ok(new { message = "Register successful", customerId = newCustomer.CustomerId });
        }

        [HttpPut("change-password")]
        public IActionResult ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var customer = _context.Customers.FirstOrDefault(c => c.Email == request.Email);
            if (customer == null) return NotFound("Customer not found");
            if (string.IsNullOrEmpty(request.OldPassword)) return BadRequest(new {message = "Old password cannot be null or empty"});
            if (string.IsNullOrEmpty(request.NewPassword)) return BadRequest(new {message = "New password cannot be null or empty"});
            if (customer.Password != request.OldPassword) return BadRequest(new {message = "Old password is incorrect"});

            customer.Password = request.NewPassword;
            _context.SaveChanges();


            return Ok(new { message = "Password changed successfully" });
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