using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IPhoneShop.Models;

namespace IPhoneShop.Controllers
{
    public class OrdersController : Controller
    {
        private readonly IphoneShopContext _context;

        public OrdersController(IphoneShopContext context)
        {
            _context = context;
        }

        // GET: /Orders/MyOrders
        [HttpGet]
        public IActionResult MyOrders()
        {
            var custId = HttpContext.Session.GetString("CustomerID");
            if (string.IsNullOrEmpty(custId))
                return RedirectToAction("Login", "Customers");

            var orders = _context.Orders
                .FromSqlRaw("SELECT * FROM Orders WHERE CustomerID = {0} ORDER BY OrderDate DESC", custId)
                .AsEnumerable()
                .ToList();

            return View(orders);
        }

        // POST: /Orders/Create (Save or Confirm) - Raw SQL Insert
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string productId, int qty, string status, string address)
        {
            var custId = HttpContext.Session.GetString("CustomerID");
            if (string.IsNullOrEmpty(custId))
                return RedirectToAction("Login", "Customers");

            if (string.IsNullOrWhiteSpace(productId) || qty <= 0 || string.IsNullOrWhiteSpace(status) || string.IsNullOrWhiteSpace(address))
            {
                TempData["OrderError"] = "All order fields are required.";
                return RedirectToAction("Browse", "Products");
            }

            // Safety: only allow Saved or Confirmed
            var normalized = status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase) ? "Confirmed" : "Saved";

            _context.Database.ExecuteSqlRaw(
                "INSERT INTO Orders (CustomerID, ProductID, Quantity, Status, OrderDate, DeliveryAddress) VALUES ({0}, {1}, {2}, {3}, GETDATE(), {4})",
                custId, productId, qty, normalized, address.Trim()
            );

            return RedirectToAction("MyOrders");
        }

        // POST: /Orders/Edit (Saved only) - Raw SQL Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int orderId, int qty, string address)
        {
            var custId = HttpContext.Session.GetString("CustomerID");
            if (string.IsNullOrEmpty(custId))
                return RedirectToAction("Login", "Customers");

            _context.Database.ExecuteSqlRaw(
                "UPDATE Orders SET Quantity = {0}, DeliveryAddress = {1} WHERE OrderID = {2} AND Status = 'Saved' AND CustomerID = {3}",
                qty, address, orderId, custId
            );

            return RedirectToAction("MyOrders");
        }

        // GET: /Orders/Delete/{orderId} (Saved only) - Raw SQL Delete
        [HttpGet]
        public IActionResult Delete(int orderId)
        {
            var custId = HttpContext.Session.GetString("CustomerID");
            if (string.IsNullOrEmpty(custId))
                return RedirectToAction("Login", "Customers");

            _context.Database.ExecuteSqlRaw(
                "DELETE FROM Orders WHERE OrderID = {0} AND Status = 'Saved' AND CustomerID = {1}",
                orderId, custId
            );

            return RedirectToAction("MyOrders");
        }

        // GET: /Orders/Details/{orderId} (View any status)
        [HttpGet]
        public IActionResult Details(int orderId)
        {
            var custId = HttpContext.Session.GetString("CustomerID");
            if (string.IsNullOrEmpty(custId))
                return RedirectToAction("Login", "Customers");

            var order = _context.Orders
                .FromSqlRaw("SELECT * FROM Orders WHERE OrderID = {0} AND CustomerID = {1}", orderId, custId)
                .AsEnumerable()
                .FirstOrDefault();

            if (order == null) return RedirectToAction("MyOrders");

            return View(order);
        }

        // GET: /Orders/Confirm/{orderId} - Confirm single saved order
        [HttpGet]
        public IActionResult Confirm(int orderId)
        {
            var custId = HttpContext.Session.GetString("CustomerID");
            if (string.IsNullOrEmpty(custId))
                return RedirectToAction("Login", "Customers");

            // Update the order status from "Saved" to "Confirmed"
            _context.Database.ExecuteSqlRaw(
                "UPDATE Orders SET Status = 'Confirmed', OrderDate = GETDATE() WHERE OrderID = {0} AND CustomerID = {1} AND Status = 'Saved'",
                orderId, custId
            );

            return RedirectToAction("MyOrders");
        }

        // GET: /Orders/ConfirmAll - Confirm all saved orders
        [HttpGet]
        public IActionResult ConfirmAll()
        {
            var custId = HttpContext.Session.GetString("CustomerID");
            if (string.IsNullOrEmpty(custId))
                return RedirectToAction("Login", "Customers");

            // Update all saved orders to confirmed
            _context.Database.ExecuteSqlRaw(
                "UPDATE Orders SET Status = 'Confirmed', OrderDate = GETDATE() WHERE CustomerID = {0} AND Status = 'Saved'",
                custId
            );

            return RedirectToAction("MyOrders");
        }
    }
}