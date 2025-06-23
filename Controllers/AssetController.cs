using AssetManager.Models;
using AssetManager.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;


namespace AssetManagerMVC.Controllers
{
    [Authorize]
    public class AssetController : Controller
    {
        private readonly FirebaseService _firebaseService;

        public AssetController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        [Authorize(Roles = "admin,ql,user")]
        public async Task<IActionResult> Index(string search, string category, string status)
        {
            var assets = await _firebaseService.GetAllAssetsAsync();
            //var assetsRef = _firebaseService.Collection("assets");
            //var snapshot = await assetsRef.GetSnapshotAsync();
            //var assets = snapshot.Documents.Select(doc => doc.ConvertTo<Asset>()).ToList();

            // Lọc theo category
            if (!string.IsNullOrEmpty(category))
            {
                assets = assets.Where(a => a.Category == category).ToList();
            }

            // Lọc theo status
            if (!string.IsNullOrEmpty(status))
            {
                assets = assets.Where(a => a.Status == status).ToList();
            }

            // Tìm kiếm theo tên hoặc mã
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                assets = assets.Where(a =>
                    (!string.IsNullOrEmpty(a.Name) && a.Name.ToLower().Contains(search)) ||
                    (!string.IsNullOrEmpty(a.Code) && a.Code.ToLower().Contains(search))
                ).ToList();
            }

            // Lấy danh sách category duy nhất để hiển thị lọc
            ViewBag.Categories = assets.Select(a => a.Category).Distinct().ToList();
            ViewBag.Statuss = assets.Select(a => a.Status).Distinct().ToList();
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentCategory = category;
            ViewBag.CurrentStatus = status;
            return View(assets);
        }

        [Authorize(Roles = "admin")]
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.StatusList = new SelectList(new List<string>
            {
                "Mới", "Đang sử dụng", "Hỏng", "Thanh lý"
            });
            ViewBag.CategoryList = new SelectList(new List<string>
            {
                "Laptop", "Máy in", "Máy chiếu", "Bàn", "Ghế", "Tủ", "Khác"
            });
            return View();
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Asset asset, IFormFile ImageFile)
        {
            if (!ModelState.IsValid)
            {
                // Log các lỗi ModelState
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine("❌ ModelState Error: " + error.ErrorMessage);
                }

                ViewBag.StatusList = new SelectList(new List<string>
                {
                    "Mới", "Đang sử dụng", "Hỏng", "Thanh lý"
                });
                ViewBag.CategoryList = new SelectList(new List<string>
                {
                    "Laptop", "Máy in", "Máy chiếu", "Bàn", "Ghế", "Tủ", "Khác"
                });

                return View(asset);
            }

            try
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                    Directory.CreateDirectory(uploadsFolder);

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    asset.ImageUrl = "/uploads/" + fileName;
                }

                asset.PurchaseDate = asset.PurchaseDate?.ToUniversalTime();
                asset.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

                // Log thông tin asset
                Console.WriteLine("=== DEBUG THÊM ASSET ===");
                Console.WriteLine($"Code: {asset.Code}");
                Console.WriteLine($"Name: {asset.Name}");
                Console.WriteLine($"Category: {asset.Category}");
                Console.WriteLine($"Location: {asset.Location}");
                Console.WriteLine($"OriginalValue: {asset.OriginalValue}");
                Console.WriteLine($"PurchaseDate: {asset.PurchaseDate}");
                Console.WriteLine($"Status: {asset.Status}");
                Console.WriteLine($"ImageUrl: {asset.ImageUrl}");
                Console.WriteLine("========================");

                await _firebaseService.AddAssetAsync(asset);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 LỖI khi tạo asset: " + ex.Message);
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi lưu tài sản. Chi tiết: " + ex.Message);
                return View(asset);
            }
        }

        [Authorize(Roles = "admin,ql")]
        public async Task<IActionResult> Edit(string id)
        {

            var asset = await _firebaseService.GetAssetByIdAsync(id);
            if (asset == null) return NotFound();

            ViewBag.StatusList = new SelectList(new List<string>
            {
                "Mới", "Đang sử dụng", "Hỏng", "Thanh lý"
            }, asset.Status);
            ViewBag.CategoryList = new SelectList(new List<string>
            {
                "Laptop", "Máy in", "Máy chiếu", "Bàn", "Ghế", "Tủ", "Khác"
            }, asset.Category);
            return View(asset);
        }

        [Authorize(Roles = "admin,ql")]
        [HttpPost]
        public async Task<IActionResult> Edit(string id, Asset asset, IFormFile ImageFile)
        {
            Console.WriteLine("🔧 Edit POST gọi đến với asset.Id = " + asset.Id);
            Console.WriteLine("📷 ImageFile: " + (ImageFile != null ? ImageFile.FileName : "null"));

            if (id != asset.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                ViewBag.StatusList = new SelectList(new List<string>
                {
                    "Mới", "Đang sử dụng", "Hỏng", "Thanh lý"
                }, asset.Status);
                ViewBag.CategoryList = new SelectList(new List<string>
                {
                    "Laptop", "Máy in", "Máy chiếu", "Bàn", "Ghế", "Tủ", "Khác"
                }, asset.Category);

                return View(asset);
            }

            try
            {
                var existingAsset = await _firebaseService.GetAssetByIdAsync(id);
                if (existingAsset == null) return NotFound();

                // Cập nhật thông tin từ form
                existingAsset.Code = asset.Code;
                existingAsset.Name = asset.Name;
                existingAsset.Category = asset.Category;
                existingAsset.Status = asset.Status;
                existingAsset.Location = asset.Location;
                existingAsset.OriginalValue = asset.OriginalValue;
                existingAsset.PurchaseDate = asset.PurchaseDate?.ToUniversalTime();

                // Nếu có file ảnh mới => cập nhật ImageUrl
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                    Directory.CreateDirectory(uploadsFolder);

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    existingAsset.ImageUrl = "/uploads/" + fileName;
                }

                Console.WriteLine("=== DEBUG UPDATE ASSET ===");
                Console.WriteLine($"ID: {existingAsset.Id}");
                Console.WriteLine($"Code: {existingAsset.Code}");
                Console.WriteLine($"Name: {existingAsset.Name}");
                Console.WriteLine($"ImageUrl: {existingAsset.ImageUrl}");
                Console.WriteLine("==========================");

                await _firebaseService.UpdateAssetAsync(existingAsset);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 Lỗi cập nhật tài sản: " + ex.Message);
                ModelState.AddModelError("", "Có lỗi khi cập nhật tài sản: " + ex.Message);
                return View(asset);
            }
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var asset = await _firebaseService.GetAssetByIdAsync(id);
            return View(asset);
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            await _firebaseService.DeleteAssetAsync(id);
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "admin,ql,user")]
        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var asset = await _firebaseService.GetAssetByIdAsync(id);
            if (asset == null) return NotFound();

            return View(asset);
        }

    }
}
