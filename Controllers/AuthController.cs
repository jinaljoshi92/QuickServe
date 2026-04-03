using Microsoft.AspNetCore.Mvc;
using QuickServe.Data;
using QuickServe.Models;

namespace QuickServe.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly QuickServeDbContext _context;
        public AuthController(QuickServeDbContext context)
        {
            _context = context;
        }

        // REGISTER
        [HttpPost("register")]
        public IActionResult Register(User user)
        {
            var existingUser = _context.Users.FirstOrDefault(x => x.Email == user.Email);
            if (existingUser != null)
            {
                return BadRequest("User already exists");
            }

            user.CreatedAt = DateTime.Now;
            user.Role = "User";

            _context.Users.Add(user);
            _context.SaveChanges();

            // Create wallet automatically
            var wallet = new Wallet
            {
                UserId = user.Id,
                Balance = 0,
                CreatedAt = DateTime.Now
            };

            _context.Wallet.Add(wallet);
            _context.SaveChanges();

            return Ok("User registered successfully");
        }

        // LOGIN
        [HttpPost("login")]
        public IActionResult Login(User loginData)
        {
            var user = _context.Users
                .FirstOrDefault(x => x.Email == loginData.Email && x.Password == loginData.Password);

            if (user == null)
            {
                return Unauthorized("Invalid credentials");
            }

            return Ok(new
            {
                user.Id,
                user.Name,
                user.Email,
                user.Role
            });
        }
    }
}
