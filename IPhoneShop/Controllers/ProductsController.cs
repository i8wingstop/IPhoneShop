using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IPhoneShop.Models;

namespace IPhoneShop.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IphoneShopContext _context;

        public ProductsController(IphoneShopContext context)
        {
            _context = context;
        }

        // GET: /Products/Browse
        [HttpGet]
        public IActionResult Browse()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("CustomerID")))
                return RedirectToAction("Login", "Customers");

            var products = _context.Products
                .FromSqlRaw("SELECT * FROM Product ORDER BY ProductName")
                .AsEnumerable()
                .ToList();

            return View(products);
        }
    }
}
