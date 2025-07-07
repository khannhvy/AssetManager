using AssetManager.Models;
using AssetManager.Services;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManager.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly FirebaseService _firebaseService;

        public UserController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        // GET: /User
        public async Task<IActionResult> Index()
        {
            var usersRef = _firebaseService.GetFirestoreDb().Collection("users");
            var snapshot = await usersRef.GetSnapshotAsync();

            List<User> users = new List<User>();
            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                if (doc.Exists)
                {
                    User user = doc.ConvertTo<User>();
                    user.Id = doc.Id;
                    users.Add(user);
                }
            }

            return View(users);
        }

        // GET: /User/Create
        [Authorize(Roles = "admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /User/Create
        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user)
        {
            if (!ModelState.IsValid)
                return View(user);

            // Sinh Uid nếu chưa có
            if (string.IsNullOrEmpty(user.Uid))
            {
                user.Uid = Guid.NewGuid().ToString();
            }

            if (string.IsNullOrEmpty(user.Role))
            {
                user.Role = "user";
            }

            var usersRef = _firebaseService.GetFirestoreDb().Collection("users");
            await usersRef.AddAsync(user);

            return RedirectToAction(nameof(Index));
        }

        // GET: /User/Edit/{id}
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Edit(string id)
        {
            var docRef = _firebaseService.GetFirestoreDb().Collection("users").Document(id);
            var doc = await docRef.GetSnapshotAsync();

            if (!doc.Exists)
                return NotFound();

            var user = doc.ConvertTo<User>();
            user.Id = doc.Id;

            return View(user);
        }

        // POST: /User/Edit
        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(User user)
        {
            if (!ModelState.IsValid)
                return View(user);

            var docRef = _firebaseService.GetFirestoreDb().Collection("users").Document(user.Id);
            await docRef.SetAsync(user, SetOptions.Overwrite);

            return RedirectToAction(nameof(Index));
        }

        // POST: /User/Delete/{id}
        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var docRef = _firebaseService.GetFirestoreDb().Collection("users").Document(id);
            await docRef.DeleteAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
