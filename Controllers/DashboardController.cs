using AssetManager.Models;
using AssetManager.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace AssetManager.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly FirebaseService _firebaseService;

        public DashboardController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        [Authorize(Roles = "ql,admin")]
        public async Task<IActionResult> Index()
        {
            var assets = await _firebaseService.GetAllAssetsAsync();

            int usedAssetsCount = assets.Count(a => a.Status == "Đang sử dụng");
            int freeAssetsCount = assets.Count(a => a.Status == "Mới");


            foreach (var asset in assets)
            {
                if (asset.PurchaseDate.HasValue &&
                    (DateTime.Now - asset.PurchaseDate.Value).TotalDays > 365 * 10)
                {
                    asset.Status = "Cũ, thanh lý";
                }
            }

            var assetsByCategory = assets
                .GroupBy(a => a.Category)
                .Select(g => new CategoryStat { Category = g.Key ?? "Không rõ", Count = g.Count() })
                .ToList();

            var assetsByStatus = assets
                .GroupBy(a => a.Status)
                .Select(g => new StatusStat { Status = g.Key ?? "Không rõ", Count = g.Count() })
                .ToList();

            var warningAssets = assets
                .Where(a =>
                    (a.PurchaseDate.HasValue && (DateTime.Now - a.PurchaseDate.Value).TotalDays > 365 * 10) ||
                    a.Status == "Cần bảo trì" || a.Status == "Chờ kiểm kê")
                .ToList();

            var viewModel = new Dashboard
            {
                WarningAssets = warningAssets,
                AssetsByCategory = assetsByCategory,
                AssetsByStatus = assetsByStatus,
                UsedAssetsCount = usedAssetsCount,
                FreeAssetsCount = freeAssetsCount
            };

            return View(viewModel);
        }
    }
}
