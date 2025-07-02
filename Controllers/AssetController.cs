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
        public async Task<IActionResult> Index(string search, string category, string status, string department)
        {
            //var assets = await _firebaseService.GetAllAssetsAsync();
            //var assetsRef = _firebaseService.Collection("assets");
            //var snapshot = await assetsRef.GetSnapshotAsync();
            //var assets = snapshot.Documents.Select(doc => doc.ConvertTo<Asset>()).ToList();
            var departments = await _firebaseService.GetAllDepartmentsAsync();
            ViewBag.Departments = departments;
            // foreach (var dept in departments)
            // {
            //     Console.WriteLine($"ID: {dept.Id}, Name: {dept.Name}");
            // }
            ViewBag.DepartmentList = new SelectList(departments, "Id", "Name", department);

            var allAssets = await _firebaseService.GetAllAssetsAsync();

            ViewBag.Categories = allAssets
                .Where(a => !string.IsNullOrEmpty(a.Category))
                .Select(a => a.Category)
                .Distinct()
                .ToList();

            // ViewBag.Departmentt = allAssets
            //     .Where(a => !string.IsNullOrEmpty(a.Department))
            //     .Select(a => a.Department)
            //     .Distinct()
            //     .ToList();

            ViewBag.Statuss = allAssets
                .Where(a => !string.IsNullOrEmpty(a.Status))
                .Select(a => a.Status)
                .Distinct()
                .ToList();

            var assets = allAssets;

            // Lọc theo category
            if (!string.IsNullOrEmpty(category))
            {
                assets = assets.Where(a => a.Category == category).ToList();
            }

            // Lọc theo department
            if (!string.IsNullOrEmpty(department))
            {
                assets = assets.Where(a => a.Department?.Id == department).ToList();
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
            // ViewBag.Categories = assets.Select(a => a.Category).Distinct().ToList();
            // ViewBag.Locationn = assets.Select(a => a.Location).Distinct().ToList();
            // ViewBag.Statuss = assets.Select(a => a.Status).Distinct().ToList();
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentCategory = category;
            ViewBag.CurrentDepartment = department;
            ViewBag.CurrentStatus = status;

            return View(assets);
        }

        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.StatusList = new SelectList(new List<string> { "Mới", "Đang sử dụng", "Hỏng", "Thanh lý" });
            ViewBag.CategoryList = new SelectList(new List<string> { "Máy tính", "Laptop", "Máy in", "Máy chiếu", "Bàn", "Ghế", "Tủ", "Khác" });

            var departments = await _firebaseService.GetAllDepartmentsAsync();
            ViewBag.DepartmentList = new SelectList(departments, "Id", "Name");

            ViewBag.NewCode = await _firebaseService.GenerateNextAssetCodeAsync();

            return View();
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Asset asset, IFormFile ImageFile)
        {
            if (!string.IsNullOrEmpty(asset.DepartmentId))
            {
                asset.Department = _firebaseService.GetDepartmentReferenceById(asset.DepartmentId);
            }

            if (!ModelState.IsValid)
            {
                ViewBag.StatusList = new SelectList(new List<string> { "Mới", "Đang sử dụng", "Hỏng", "Thanh lý" });
                ViewBag.CategoryList = new SelectList(new List<string> { "Máy tính", "Laptop", "Máy in", "Máy chiếu", "Bàn", "Ghế", "Tủ", "Khác" });

                var departments = await _firebaseService.GetAllDepartmentsAsync();
                ViewBag.DepartmentList = new SelectList(departments, "Id", "Name");

                return View(asset);
            }

            try
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                    Directory.CreateDirectory(uploadsFolder);

                    var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    asset.ImageUrl = "/uploads/" + fileName;
                }

                asset.PurchaseDate = asset.PurchaseDate?.ToUniversalTime();
                asset.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

                await _firebaseService.AddAssetAsync(asset);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi khi tạo tài sản: " + ex.Message);
                return View(asset);
            }
        }


        [Authorize(Roles = "admin,ql")]
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var asset = await _firebaseService.GetAssetByIdAsync(id);
            if (asset == null) return NotFound();

            // Binding dropdown lists
            ViewBag.StatusList = new SelectList(new List<string> { "Mới", "Đang sử dụng", "Hỏng", "Thanh lý" }, asset.Status);
            ViewBag.CategoryList = new SelectList(new List<string> { "Máy tính", "Laptop", "Máy in", "Máy chiếu", "Bàn", "Ghế", "Tủ", "Khác" }, asset.Category);

            var departments = await _firebaseService.GetAllDepartmentsAsync();
            ViewBag.DepartmentList = new SelectList(departments, "Id", "Name", asset.Department?.Id);

            // Gán DepartmentId tạm để hiển thị đúng selected trong form
            asset.DepartmentId = asset.Department?.Id;

            return View(asset);
        }

        [Authorize(Roles = "admin,ql")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Asset asset, IFormFile ImageFile)
        {
              ModelState.Remove("ImageFile");
            if (id != asset.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                 foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine("Model error: " + error.ErrorMessage);
                }
                ViewBag.StatusList = new SelectList(new List<string> { "Mới", "Đang sử dụng", "Hỏng", "Thanh lý" }, asset.Status);
                ViewBag.CategoryList = new SelectList(new List<string> { "Máy tính", "Laptop", "Máy in", "Máy chiếu", "Bàn", "Ghế", "Tủ", "Khác" }, asset.Category);
                var departments = await _firebaseService.GetAllDepartmentsAsync();
                ViewBag.DepartmentList = new SelectList(departments, "Id", "Name", asset.DepartmentId);
                return View(asset);
            }

            try
            {
                var existing = await _firebaseService.GetAssetByIdAsync(id);
                if (existing == null) return NotFound();

                // Cập nhật thông tin
                existing.Code = asset.Code;
                existing.Name = asset.Name;
                existing.Category = asset.Category;
                existing.Status = asset.Status;
                //existing.Location = asset.Location;
                existing.OriginalValue = asset.OriginalValue;
                existing.PurchaseDate = asset.PurchaseDate?.ToUniversalTime();

                if (!string.IsNullOrEmpty(asset.DepartmentId))
                {
                    existing.Department = _firebaseService.GetDepartmentReferenceById(asset.DepartmentId);
                }

                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                    Directory.CreateDirectory(uploadsFolder);

                    var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    existing.ImageUrl = "/uploads/" + fileName;
                }
                else
                {
                     // Nếu không có ảnh mới, giữ lại đường dẫn ảnh cũ
                    existing.ImageUrl = asset.ImageUrl;
                }

                await _firebaseService.UpdateAssetAsync(existing);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi cập nhật: " + ex.Message);
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

        [Authorize(Roles = "ql")]
        public async Task<IActionResult> History(string id)
        {
            List<History> histories;
            if (string.IsNullOrEmpty(id))
            {
                histories = await _firebaseService.GetAllHistoriesAsync();
                ViewBag.Asset = null;
                ViewBag.Title = "Lịch sử thay đổi tất cả tài sản";
            }
            else
            {
                var asset = await _firebaseService.GetAssetByIdAsync(id);
                if (asset == null)
                    return NotFound("Không tìm thấy tài sản.");

                histories = await _firebaseService.GetHistoriesByAssetIdAsync(id);
                ViewBag.Asset = asset;
                ViewBag.Title = $"Lịch sử thay đổi tài sản: {asset.Name}";
            }

            return View("History", histories);
        }

        // public async Task<IActionResult> Assignments(string id)
        // {
        //     var assignments = string.IsNullOrEmpty(id)
        //         ? await _firebaseService.GetAllAssignmentsAsync()
        //         : await _firebaseService.GetAssignmentsByAssetIdAsync(id);

        //     Console.WriteLine("Assignments count: " + assignments.Count);


        //     var viewModels = new List<AssignmentViewModel>();

        //     foreach (var a in assignments)
        //     {
        //         Console.WriteLine($"Assignment: AssetId={a.AssetId}, UserId={a.UserId}");
        //         if (string.IsNullOrWhiteSpace(a.AssetId))
        //             continue;

        //         var asset = await _firebaseService.GetAssetByIdAsync(a.AssetId);
        //         var user = await _firebaseService.GetUserByIdAsync(a.UserId);
        //         var dept = await _firebaseService.GetDepartmentByIdAsync(a.DepartmentId);

        //         viewModels.Add(new AssignmentViewModel
        //         {
        //             AssetName = asset?.Name ?? "Không rõ",
        //             UserName = user?.FullName ?? user?.Email ?? "Không rõ",
        //             DepartmentName = dept?.Name ?? "Không rõ",
        //             AssignedDate = a.AssignedDate.ToDateTime(),
        //             ReturnedDate = a.ReturnedDate?.ToDateTime(),
        //             Status = a.Status,
        //             Notes = a.Notes
        //         });
        //     }

        //     ViewBag.Title = string.IsNullOrEmpty(id)
        //         ? "Lịch sử mượn - trả tất cả tài sản"
        //         : $"Lịch sử mượn - trả tài sản: {id}";

        //     return View("Assignments", viewModels);
        // }




    }
}
