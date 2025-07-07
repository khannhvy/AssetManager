using AssetManager.Models;
using AssetManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;

namespace AssetManager.Controllers
{
    [Authorize]
    public class MaintenanceController : Controller
    {
        private readonly FirebaseService _firebase;

        public MaintenanceController(FirebaseService firebase)
        {
            _firebase = firebase;
        }

        public async Task<IActionResult> Index()
        {
            var maintenances = await _firebase.GetAllMaintenancesAsync();
            return View(maintenances);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new MaintenanceViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MaintenanceViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            // Tìm docId theo Code
            var assetQuery = _firebase.GetFirestoreDb()
                .Collection("assets")
                .WhereEqualTo("Code", vm.AssetCode)
                .Limit(1);
            var snapshot = await assetQuery.GetSnapshotAsync();
            if (snapshot.Count == 0)
            {
                ModelState.AddModelError("AssetCode", "Không tìm thấy tài sản với mã này.");
                return View(vm);
            }

            var assetDoc = snapshot.Documents.First();
            var assetId = assetDoc.Id;

            var assetRef = _firebase.GetFirestoreDb().Collection("assets").Document(assetId);

            var maintenance = new Maintenance
            {
                AssetRef = assetRef,
                Description = vm.Description,
                MaintenanceDate = vm.MaintenanceDate.ToUniversalTime(),
                Cost = vm.Cost,
                PerformedBy = vm.PerformedBy
            };

            await _firebase.CreateMaintenanceAsync(maintenance);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var maintenanceDoc = await _firebase.GetFirestoreDb()
                .Collection("maintenance_logs")
                .Document(id)
                .GetSnapshotAsync();

            if (!maintenanceDoc.Exists)
                return NotFound();

            var maintenance = maintenanceDoc.ConvertTo<Maintenance>();
            maintenance.Id = maintenanceDoc.Id;

            var assetSnap = await maintenance.AssetRef.GetSnapshotAsync();
            var asset = assetSnap.ConvertTo<Asset>();

            var vm = new MaintenanceViewModel
            {
                Id = maintenance.Id,
                AssetCode = asset?.Code,
                AssetName = asset?.Name,
                Description = maintenance.Description,
                MaintenanceDate = maintenance.MaintenanceDate,
                Cost = maintenance.Cost,
                PerformedBy = maintenance.PerformedBy
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MaintenanceViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            if (vm.MaintenanceDate.Kind != DateTimeKind.Utc)
            {
                vm.MaintenanceDate = vm.MaintenanceDate.ToUniversalTime();
            }

            await _firebase.UpdateMaintenanceAsync(vm);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Scheduled()
        {
            var assetsSnap = await _firebase.GetFirestoreDb()
                .Collection("assets")
                .GetSnapshotAsync();

            var assets = new List<MaintenanceScheduleViewModel>();

            foreach (var assetDoc in assetsSnap.Documents)
            {
                var asset = assetDoc.ConvertTo<Asset>();
                var assetId = assetDoc.Id;

                var assetRef = _firebase.GetFirestoreDb().Collection("assets").Document(assetId);

                var logsSnap = await _firebase.GetFirestoreDb()
                    .Collection("maintenance_logs")
                    .WhereEqualTo("AssetRef", assetRef)
                    .OrderByDescending("maintenanceDate")
                    .Limit(1)
                    .GetSnapshotAsync();

                DateTime? lastDate = null;

                if (logsSnap.Documents.Any())
                {
                    var lastLog = logsSnap.Documents.First().ConvertTo<Maintenance>();
                    lastDate = lastLog.MaintenanceDate.ToLocalTime();
                }

                DateTime nextDate;

                if (lastDate.HasValue)
                {
                    nextDate = lastDate.Value.AddMonths(6);
                }
                else
                {
                    nextDate = asset.PurchaseDate.HasValue
                        ? asset.PurchaseDate.Value.AddMonths(6)
                        : DateTime.Now.AddMonths(6);

                }

                string status = nextDate < DateTime.Now
                    ? "Quá hạn bảo trì"
                    : "Còn hạn";

                assets.Add(new MaintenanceScheduleViewModel
                {
                    AssetId = assetId,
                    AssetCode = asset.Code,
                    AssetName = asset.Name,
                    PurchaseDate = asset.PurchaseDate,
                    LastMaintenanceDate = lastDate,
                    NextMaintenanceDate = nextDate,
                    Status = status
                });
            }

            return View(assets);
        }

    }
}
