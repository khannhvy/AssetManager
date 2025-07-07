using AssetManager.Models;
using AssetManager.Services;
using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authorization;
using AssetManager.Models;
using System.Security.Claims;



namespace AssetManager.Controllers
{
    [Authorize]
    public class AssignmentController : Controller
    {
        private readonly FirebaseService _firebase;

        public AssignmentController(FirebaseService firebase)
        {
            _firebase = firebase;
        }

        public async Task<IActionResult> SelectAsset()
        {
            var allAssets = await _firebase.GetAvailableAssetsAsync();

            // Nhóm theo loại tài sản (Category)
            var grouped = allAssets
                .GroupBy(a => a.Category)
                .ToDictionary(g => g.Key, g => g.ToList());

            return View(grouped);
        }

        public async Task<IActionResult> Assign(string assetId)
        {
            if (string.IsNullOrEmpty(assetId))
            {
                return BadRequest("Thiếu mã tài sản.");
            }

            var asset = await _firebase.GetAssetByIdAsync(assetId);
            if (asset == null) return NotFound();

            var vm = new AssetAssignmentViewModel
            {
                AssetId = asset.Id,
                AssetCode = asset.Code,
                AssetName = asset.Name
            };

            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(AssetAssignmentViewModel vm)
        {
            // Console.WriteLine($" AssetId: {vm.AssetId}");
            // Console.WriteLine($" AssigneeId (Firestore doc ID): {vm.AssigneeId}");

            ModelState.Remove("Id");

            if (!ModelState.IsValid)
            {
                // Console.WriteLine(" ModelState không hợp lệ");
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    foreach (var error in state.Errors)
                    {
                        Console.WriteLine($" - {key}: {error.ErrorMessage}");
                    }
                }
                return View(vm);

            }

            
            //var assignedById = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var assignedByEmail = User.FindFirst(ClaimTypes.Name)?.Value;

           // vm.AssignedById = assignedById;
            //vm.AssignedByEmail = assignedByEmail;

            // Lấy thông tin người giao (đang đăng nhập)
            //var assigner = await _firebase.GetUserByIdAsync(assignedById);
            //var assignedByName = assigner?.Name ?? "(Không rõ)";

            var user = await _firebase.GetUserByIdAsync(vm.AssigneeId);
            //var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (user == null)
            {
                // Console.WriteLine(" Không tìm thấy người dùng với ID này");
                ModelState.AddModelError("AssigneeId", "Không tìm thấy người dùng với ID này.");
                return View(vm);
            }
            // Giao tài sản và lưu thông tin người nhận + người giao
            // await _firebase.CreateAssignmentAsync(vm.AssetId, vm.AssigneeId, assignedById, assignedByName);
            await _firebase.CreateAssignmentAsync(vm.AssetId, vm.AssigneeId,  assignedByEmail);

            try
            {
                //Console.WriteLine($" Người dùng hợp lệ: {user.Name} - {user.Email}");

                // await _firebase.CreateAssignmentAsync(vm.AssetId, vm.AssigneeId, assignedById, user.Name);
                await _firebase.CreateAssignmentAsync(vm.AssetId, vm.AssigneeId,  assignedByEmail);
                // Console.WriteLine($" Đã ghi assignment vào Firestore");

                await _firebase.AddAssetHistoryAsync(new History
                {
                    AssetId = vm.AssetId,
                    Action = $"Giao cho {user.Name} bởi {assignedByEmail}",
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
                });
                //Console.WriteLine($" Đã ghi lịch sử giao tài sản");
            }
            catch (Exception ex)
            {
                // Console.WriteLine($" Lỗi khi ghi giao tài sản hoặc lịch sử: {ex.Message}");
                // Console.WriteLine(ex.StackTrace);
                return View(vm);
            }

            return RedirectToAction("ReturnList");
        }

        // Danh sách đang sử dụng
        public async Task<IActionResult> InUse()
        {
            var assignments = await _firebase.GetCurrentAssignmentsAsync();
            return View(assignments);
        }

        public async Task<IActionResult> Return(string assignmentId)
        {
            await _firebase.ReturnAssignmentAsync(assignmentId);
            TempData["Success"] = "Đã thu hồi tài sản thành công!";
            return RedirectToAction("ReturnList");
        }

        // public async Task<IActionResult> ReturnList()
        // {
        //     var assignments = await _firebase.GetAssignmentsInUseAsync();
        //     return View(assignments);
        // }

        public async Task<IActionResult> ReturnList()
        {
            var viewModel = await _firebase.GetAssignmentsInUseViewModelAsync();
            return View(viewModel);
        }

        public async Task<IActionResult> HistoryInUse()
        {
            var history = await _firebase.GetAssetHistoryAsync();
            return View(history);
        }

        [HttpGet]
        public async Task<IActionResult> HistoryInUse(string category, string assignee)
        {
            var allHistory = await _firebase.GetAssetHistoryAsync();

            if (!string.IsNullOrEmpty(category))
                allHistory = allHistory.Where(h => h.AssetName?.Contains(category, StringComparison.OrdinalIgnoreCase) == true).ToList();

            if (!string.IsNullOrEmpty(assignee))
                allHistory = allHistory.Where(h => h.AssigneeName?.Contains(assignee, StringComparison.OrdinalIgnoreCase) == true).ToList();

            ViewBag.Categories = new List<string> { "Tất cả", "Máy tính", "Laptop", "Máy in", "Máy chiếu", "Bàn", "Ghế", "Tủ", "Khác" };
            ViewBag.CurrentCategory = category;
            ViewBag.CurrentAssignee = assignee;

            return View(allHistory);
        }



    }
}
