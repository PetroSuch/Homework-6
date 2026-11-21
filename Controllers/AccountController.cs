using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskAuthenticationAuthorization.Models;

namespace TaskAuthenticationAuthorization.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly SignInManager<ApplicationUser> _signIn;
        private readonly AccountsContext _accounts;

        public AccountController(UserManager<ApplicationUser> users,
            SignInManager<ApplicationUser> signIn, AccountsContext accounts)
        {
            _users = users;
            _signIn = signIn;
            _accounts = accounts;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email
            };

            await using var transaction = await _accounts.Database.BeginTransactionAsync();
            var result = await _users.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                result = await _users.AddToRoleAsync(user, "Buyer");
            }
            if (result.Succeeded)
            {
                var buyerType = new Claim(BuyerTypes.ClaimName, BuyerTypes.Regular);
                result = await _users.AddClaimAsync(user, buyerType);
            }
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            await transaction.CommitAsync();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Login(string returnUrl)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _signIn.PasswordSignInAsync(model.Email, model.Password,
                isPersistent: false, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Unable to sign in with these credentials.");
                return View(model);
            }

            if (Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signIn.SignOutAsync();
            return RedirectToAction("Index", "Products");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            Response.StatusCode = 403;
            return View();
        }
    }
}
