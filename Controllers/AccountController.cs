using AssetManager.Services;
using AssetManager.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Google.Cloud.Firestore;
using AssetManager.Models;
using Microsoft.AspNetCore.Authorization;

namespace AssetManager.Controllers
{
    // [Authorize]
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

            return RedirectToAction("Index", "Asset");
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

        [HttpGet("/api/users/{id}")]
        public async Task<IActionResult> GetUserName(string id)
        {
            var user = await _firebaseService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();

            return Json(new { fullName = user.Name });
        }

        // [HttpGet("/api/users/search")]
        [HttpGet("/api/users/findbyiduser")]
        public async Task<IActionResult> GetUserByIdUserField(string q)
        {
            // Console.WriteLine($" [API] Bắt đầu tìm kiếm người dùng theo IdUser = {idUser}");
            var query = _firebaseService.GetFirestoreDb().Collection("users")
                .WhereEqualTo("IdUser", q)
                .Limit(1);

            var snapshot = await query.GetSnapshotAsync();

            // Console.WriteLine($" Kết quả trả về: {snapshot.Count} bản ghi");

            if (snapshot.Count == 0)
                return NotFound();

            var doc = snapshot.Documents.First();
            var user = doc.ConvertTo<User>();

            //user.Id = doc.Id;

            return Json(new
            {
                id = user.Id,
                name = user.Name,
                email = user.Email
            });

        }

        [HttpGet("/api/users/search")]
        public async Task<IActionResult> SearchUsers(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("Thiếu từ khóa tìm kiếm");

            //Console.WriteLine($" Đang tìm với từ khóa: {q}");

            var users = await _firebaseService.SearchUsersAsync(q);

            var results = users.Select(user => new
            {
                id = user.Id,
                name = user.Name,
                email = user.Email
            });

            return Ok(results);
        }

        [HttpGet("/api/users/findbyiduser")]
        public async Task<IActionResult> FindByIdUser(string q)
        {
            Console.WriteLine($"🔍 Đang tìm người dùng theo IdUser: {q}");

            var query = _firebaseService.GetFirestoreDb().Collection("users")
                .WhereEqualTo("IdUser", q)
                .Limit(1);

            var snapshot = await query.GetSnapshotAsync();

            if (snapshot.Count == 0)
                return NotFound();

            var doc = snapshot.Documents.First();
            var user = doc.ConvertTo<User>();
            user.Id = doc.Id; 
            return Json(new
            {
                id = user.Id,
                name = user.Name,
                email = user.Email
            });
        }

    }
}
