using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IPhoneShop.Models;
using Microsoft.AspNetCore.Http;

namespace IPhoneShop.Controllers
{
    public class CustomersController : Controller
    {
        private readonly IphoneShopContext _context;

        public CustomersController(IphoneShopContext context)
        {
            _context = context;
        }

        // GET: /Customers/Register
        [HttpGet]
        public IActionResult Register() => View();

        // POST: /Customers/Register  (Auto-generate CustomerID)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(string firstName, string lastName, string email, string password, string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(phoneNumber))
            {
                ViewBag.Error = "All fields are required.";
                return View();
            }

            // Check if email already exists
            var existingCustomer = _context.Customers
                .FromSqlRaw("SELECT * FROM Customer WHERE Email = {0}", email.Trim())
                .AsEnumerable()
                .FirstOrDefault();

            if (existingCustomer != null)
            {
                ViewBag.Error = "An account with this email address already exists.";
                return View();
            }

            // Auto-generate CustomerID
            string newCustomerId = GenerateCustomerId();

            // Insert with parameters
            _context.Database.ExecuteSqlRaw(
                "INSERT INTO Customer (CustomerID, FirstName, LastName, Email, Password, PhoneNumber) VALUES ({0}, {1}, {2}, {3}, {4}, {5})",
                newCustomerId, firstName.Trim(), lastName.Trim(), email.Trim(), password.Trim(), phoneNumber.Trim()
            );

            return RedirectToAction("Login");
        }

        // Helper method to generate unique CustomerID
        private string GenerateCustomerId()
        {
            // Get the highest existing CustomerID number (looking for S prefix)
            var existingIds = _context.Customers
                .FromSqlRaw("SELECT * FROM Customer")
                .AsEnumerable()
                .Select(c => c.CustomerId)
                .Where(id => id.StartsWith("S") && id.Length == 4)
                .Select(id => {
                    if (int.TryParse(id.Substring(1), out int num))
                        return num;
                    return 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            // Generate next ID
            int nextId = existingIds + 1;
            return $"S{nextId:000}"; // Format as S001, S002, etc.
        }

        // GET: /Customers/Login
        [HttpGet]
        public IActionResult Login()
        {
            // reset session
            HttpContext.Session.SetString("CustomerID", "");
            return View();
        }

        // POST: /Customers/Login  (Updated to use Email instead of CustomerID)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            var customer = _context.Customers
                .FromSqlRaw("SELECT * FROM Customer WHERE Email = {0} AND Password = {1}", email, password)
                .AsEnumerable() // materialize for EF Core 8 safety
                .FirstOrDefault();

            if (customer == null)
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            HttpContext.Session.SetString("CustomerID", customer.CustomerId);
            return RedirectToAction("Browse", "Products");
        }

        // GET: /Customers/Logout
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // GET: /Customers/OrderHistory
        [HttpGet]
        public IActionResult OrderHistory()
        {
            var custId = HttpContext.Session.GetString("CustomerID");
            if (string.IsNullOrEmpty(custId))
                return RedirectToAction("Login");

            var orders = _context.Orders
                .FromSqlRaw("SELECT * FROM Orders WHERE CustomerID = {0} ORDER BY OrderDate DESC", custId)
                .AsEnumerable()
                .ToList();

            return View(orders);
        }
    }
}