using AssetManager.Services;
using AssetManager.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AssetManager.Controllers
{
    public class AccountController : Controller
    {
        private readonly FirebaseService _firebaseService;

        public AccountController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(Login model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Xác thực tài khoản
            var user = await _firebaseService.AuthenticateUserAsync(model.Email, model.Password);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
                return View(model);
            }

            //  Lấy vai trò từ Firestore
            var role = await _firebaseService.GetUserRoleAsync(user.Email) ?? "user";
           // var role = await _firebaseService.GetUserRoleAsync(user.Uid) ?? "user";
             Console.WriteLine($" TK {user.Email} - Role: {role}");

            //  Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Uid),
                new Claim(ClaimTypes.Role, role)
            };

            // foreach (var claim in claims)
            // {
            //     Console.WriteLine($" Claim: {claim.Type} = {claim.Value}");
            // }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
        
        public IActionResult DebugUser()
        {
            if (!User.Identity.IsAuthenticated)
                return Content("Chưa đăng nhập");

            var result = "Thông tin User:\n";
            foreach (var claim in User.Claims)
            {
                result += $"{claim.Type} = {claim.Value}\n";
            }

            return Content(result);
        }

    }
}
