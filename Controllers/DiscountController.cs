using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskAuthenticationAuthorization.Models;

namespace TaskAuthenticationAuthorization.Controllers
{
    [Authorize(Policy = "MyDiscount")]
    public class DiscountController : Controller
    {
        public IActionResult Index()
        {
            return View(model: User.FindFirst(BuyerTypes.ClaimName)?.Value);
        }
    }
}
