using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TaskAuthenticationAuthorization.Models;

namespace TaskAuthenticationAuthorization.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly AccountsContext _accounts;
        private readonly ShoppingContext _shopping;

        public AdminController(UserManager<ApplicationUser> users, AccountsContext accounts, ShoppingContext shopping)
        {
            _users = users;
            _accounts = accounts;
            _shopping = shopping;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _users.Users.OrderBy(user => user.Email).ToListAsync();
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _users.GetRolesAsync(user);
            var claims = await _users.GetClaimsAsync(user);
            var buyerType = claims.FirstOrDefault(claim => claim.Type == BuyerTypes.ClaimName);
            var model = new EditUserViewModel
            {
                Role = roles.FirstOrDefault(),
                BuyerType = buyerType?.Value ?? BuyerTypes.None,
                CustomerId = user.CustomerId
            };

            return await EditViewAsync(user, model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditUserViewModel model)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (model.CustomerId.HasValue)
            {
                var customerId = model.CustomerId.Value;
                var customerExists = await _shopping.Customers.AnyAsync(customer => customer.ID == customerId);
                var customerIsAssigned = await _users.Users
                    .AnyAsync(other => other.Id != id && other.CustomerId == customerId);

                if (!customerExists || customerIsAssigned)
                {
                    ModelState.AddModelError(nameof(model.CustomerId), "Choose an available customer.");
                }
            }
            if (!ModelState.IsValid)
            {
                return await EditViewAsync(user, model);
            }

            var result = await SaveUserAsync(user, model);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return await EditViewAsync(user, model);
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<IdentityResult> SaveUserAsync(ApplicationUser user, EditUserViewModel model)
        {
            // Save the role, buyer type and customer assignment together.
            await using var transaction = await _accounts.Database.BeginTransactionAsync();
            var result = IdentityResult.Success;

            var oldRoles = await _users.GetRolesAsync(user);
            var rolesToRemove = oldRoles.Where(role => role != model.Role).ToList();
            if (rolesToRemove.Count > 0)
            {
                result = await _users.RemoveFromRolesAsync(user, rolesToRemove);
            }
            if (result.Succeeded && !oldRoles.Contains(model.Role))
            {
                result = await _users.AddToRoleAsync(user, model.Role);
            }
            if (!result.Succeeded)
            {
                return result;
            }

            var claims = await _users.GetClaimsAsync(user);
            var buyerClaims = claims.Where(claim => claim.Type == BuyerTypes.ClaimName).ToList();
            if (buyerClaims.Count > 0)
            {
                result = await _users.RemoveClaimsAsync(user, buyerClaims);
            }
            if (result.Succeeded)
            {
                var buyerType = new Claim(BuyerTypes.ClaimName, model.BuyerType);
                result = await _users.AddClaimAsync(user, buyerType);
            }
            if (!result.Succeeded)
            {
                return result;
            }

            user.CustomerId = model.CustomerId;
            try
            {
                result = await _users.UpdateSecurityStampAsync(user);
            }
            catch (DbUpdateException)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Description = "Could not save the customer assignment. Try again."
                });
            }

            if (result.Succeeded)
            {
                await transaction.CommitAsync();
            }
            return result;
        }

        private async Task<IActionResult> EditViewAsync(ApplicationUser user, EditUserViewModel model)
        {
            var assignedIds = await _users.Users
                .Where(other => other.Id != user.Id && other.CustomerId.HasValue)
                .Select(other => other.CustomerId.Value)
                .ToListAsync();
            var customers = await _shopping.Customers
                .Where(customer => !assignedIds.Contains(customer.ID))
                .OrderBy(customer => customer.LastName)
                .Select(customer => new { customer.ID, Name = customer.FirstName + " " + customer.LastName })
                .ToListAsync();

            ViewData["Customers"] = new SelectList(customers, "ID", "Name");
            model.Email = user.Email;
            model.Id = user.Id;
            return View("Edit", model);
        }
    }
}
