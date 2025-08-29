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

        // POST: /Customers/Register  (Raw SQL Insert)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(string customerId, string firstName, string lastName, string email, string password, string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(customerId) ||
                string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(phoneNumber))
            {
                ViewBag.Error = "All fields are required.";
                return View();
            }

            // Insert with parameters
            _context.Database.ExecuteSqlRaw(
                "INSERT INTO Customer (CustomerID, FirstName, LastName, Email, Password, PhoneNumber) VALUES ({0}, {1}, {2}, {3}, {4}, {5})",
                customerId.Trim(), firstName.Trim(), lastName.Trim(), email.Trim(), password.Trim(), phoneNumber.Trim()
            );

            return RedirectToAction("Login");
        }

        // GET: /Customers/Login
        [HttpGet]
        public IActionResult Login()
        {
            // reset session
            HttpContext.Session.SetString("CustomerID", "");
            return View();
        }

        // POST: /Customers/Login  (Raw SQL Select)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string customerId, string password)
        {
            var customer = _context.Customers
                .FromSqlRaw("SELECT * FROM Customer WHERE CustomerID = {0} AND Password = {1}", customerId, password)
                .AsEnumerable() // materialize for EF Core 8 safety
                .FirstOrDefault();

            if (customer == null)
            {
                ViewBag.Error = "Invalid Customer ID or Password.";
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