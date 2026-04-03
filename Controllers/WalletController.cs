using Microsoft.AspNetCore.Mvc;
using QuickServe.Data;
using QuickServe.Models;

namespace QuickServe.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController : ControllerBase
    {
        private readonly QuickServeDbContext _context;

        public WalletController(QuickServeDbContext context)
        {
            _context = context;
        }

        // GET BALANCE
        [HttpGet("balance/{userId}")]
        public IActionResult GetBalance(int userId)
        {
            var wallet = _context.Wallet.FirstOrDefault(w => w.UserId == userId);

            if (wallet == null)
                return NotFound("Wallet not found");

            return Ok(new { wallet.Balance });
        }

        [HttpPost("add-dummy/{userId}")]
        public IActionResult AddDummyMoney(int userId)
        {
            var wallet = _context.Wallet.FirstOrDefault(w => w.UserId == userId);

            if (wallet == null)
                return NotFound();

            wallet.Balance += 100;

            _context.SaveChanges();

            return Ok("Rs.100 Money added");
        }

        [HttpPost("create-payment-session")]
        public IActionResult CreatePaymentSession(int userId, decimal amount)
        {
            // Simulate payment URL
            var fakeUrl = $"https://localhost:5033//payment?userId={userId}&amount={amount}";

            return Ok(new
            {
                message = "Redirect to payment page",
                url = fakeUrl
            });
        }

        [HttpPost("payment-success")]
        public IActionResult PaymentSuccess(int userId, decimal amount)
        {
            var wallet = _context.Wallet.FirstOrDefault(w => w.UserId == userId);

            if (wallet == null)
                return NotFound();

            wallet.Balance += amount;

            var transaction = new Transaction
            {
                WalletId = wallet.WalletId,
                Amount = amount,
                Type = "Credit",
                Description = "Payment Success",
                CreatedAt = DateTime.Now
            };

            _context.Transactions.Add(transaction);
            _context.SaveChanges();

            return Ok("Payment successful and wallet updated");
        }

        [HttpPost("payment-failure")]
        public IActionResult PaymentFailure(int userId, decimal amount)
        {
            // Optional: log failed transaction (good practice)

            var wallet = _context.Wallet.FirstOrDefault(w => w.UserId == userId);

            if (wallet == null)
                return NotFound();

            var transaction = new Transaction
            {
                WalletId = wallet.WalletId,
                Amount = amount,
                Type = "Failed",
                Description = "Payment Failed",
                CreatedAt = DateTime.Now
            };

            _context.Transactions.Add(transaction);
            _context.SaveChanges();

            return Ok("Payment failed recorded");
        }
    }
}
