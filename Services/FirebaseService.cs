using Google.Cloud.Firestore;
using AssetManager.Models;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore.V1;
using System.Text.Json;
using AssetManager.Models;

namespace AssetManager.Services
{
    public class FirebaseService
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly string _apiKey;
        private const string CollectionName = "assets";

        public FirebaseService(IConfiguration config)
        {
            var credentialPath = Path.Combine(Directory.GetCurrentDirectory(), "qlts-6baea-firebase-adminsdk-fbsvc-b3acc9bc3c.json");
            var credential = GoogleCredential.FromFile(credentialPath);
            var firestoreClient = new FirestoreClientBuilder
            {
                Credential = credential
            }.Build();

            _firestoreDb = FirestoreDb.Create(config["Firestore:ProjectId"], firestoreClient);
            _apiKey = config["Firestore:ApiKey"];
        }
        public FirestoreDb GetFirestoreDb()
        {
            return _firestoreDb;
        }

        //  CRUD tài sản
        public async Task<List<Asset>> GetAllAssetsAsync()
        {
            var snapshot = await _firestoreDb.Collection(CollectionName).GetSnapshotAsync();
            return snapshot.Documents.Select(d => d.ConvertTo<Asset>()).ToList();
        }

        public async Task<Asset> GetAssetByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id), "Asset ID must not be null or empty.");

            var docRef = _firestoreDb.Collection("assets").Document(id);
            var snapshot = await docRef.GetSnapshotAsync();
            if (!snapshot.Exists)
                return null;

            return snapshot.ConvertTo<Asset>();
        }

        // public async Task<Asset?> GetAssetByIdAsync(string id)
        // {
        //     var docRef = _firestoreDb.Collection(CollectionName).Document(id);
        //     var snapshot = await docRef.GetSnapshotAsync();
        //     return snapshot.Exists ? snapshot.ConvertTo<Asset>() : null;
        // }

        public async Task AddAssetAsync(Asset asset)
        {
            await _firestoreDb.Collection(CollectionName).AddAsync(asset);
        }

        public async Task UpdateAssetAsync(Asset asset)
        {
            var docRef = _firestoreDb.Collection(CollectionName).Document(asset.Id);
            await docRef.SetAsync(asset);
        }

        public async Task DeleteAssetAsync(string id)
        {
            var docRef = _firestoreDb.Collection(CollectionName).Document(id);
            await docRef.DeleteAsync();
        }

        //  Xác thực tài khoản bằng Firebase Auth
        public async Task<User?> AuthenticateUserAsync(string email, string password)
        {
            using var client = new HttpClient();

            var payload = new
            {
                email,
                password,
                returnSecureToken = true
            };

            var response = await client.PostAsJsonAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_apiKey}",
                payload);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                return new User
                {
                    Uid = result.GetProperty("localId").GetString(),
                    Email = result.GetProperty("email").GetString()
                };
            }

            return null;
        }

        //  Lấy vai trò (role) từ Firestore
        public async Task<string?> GetUserRoleAsync(string email)
        {
            var usersRef = _firestoreDb.Collection("users");
            var query = usersRef.WhereEqualTo("email", email).Limit(1);
            var snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count > 0)
            {
                var doc = snapshot.Documents[0];
                if (doc.ContainsField("role"))
                {
                    var role = doc.GetValue<string>("role");
                    //Console.WriteLine($"✅ Role lấy từ Firestore: {role} (email: {email})");
                    return role;
                }
            }

            Console.WriteLine($"⚠️ Không tìm thấy role trong Firestore cho email: {email}");
            return null;
        }

        public async Task AddAssetHistoryAsync(History history)
        {
            await _firestoreDb.Collection("assetHistories").AddAsync(history);
        }

        public async Task<List<History>> GetHistoriesByAssetIdAsync(string assetId)
        {
            var snapshot = await _firestoreDb
                .Collection("assetHistories")
                .WhereEqualTo("AssetId", assetId)
                .OrderByDescending("Timestamp")
                .GetSnapshotAsync();

            return snapshot.Documents.Select(d => d.ConvertTo<History>()).ToList();
        }

        // public async Task<List<Assignment>> GetAllAssignmentsAsync()
        // {
        //     var assignments = new List<Assignment>();

        //     var snapshot = await _firestoreDb.Collection("assignments").GetSnapshotAsync();

        //     foreach (var doc in snapshot.Documents)
        //     {
        //         var assignment = doc.ConvertTo<Assignment>();
        //         assignment.Id = doc.Id;
        //         assignments.Add(assignment);
        //     }

        //     return assignments;
        // }

        // public async Task<List<Assignment>> GetAssignmentsByAssetIdAsync(string assetId)
        // {
        //     var assignments = new List<Assignment>();
        //     var snapshot = await _firestoreDb.Collection("assignments")
        //         .WhereEqualTo("assetId", assetId)
        //         .GetSnapshotAsync();

        //     foreach (var doc in snapshot.Documents)
        //     {
        //         var assignment = doc.ConvertTo<Assignment>();
        //         assignment.Id = doc.Id;
        //         assignments.Add(assignment);
        //     }

        //     return assignments;
        // }

        public async Task<List<History>> GetAllHistoriesAsync()
        {
            var snapshot = await _firestoreDb
                .Collection("assetHistories")
                .OrderByDescending("Timestamp")
                .GetSnapshotAsync();

            return snapshot.Documents.Select(d => d.ConvertTo<History>()).ToList();
        }

        // Lấy tất cả phòng ban
        public async Task<List<Department>> GetAllDepartmentsAsync()
        {
            var snapshot = await _firestoreDb.Collection("departments").GetSnapshotAsync();
            return snapshot.Documents.Select(doc =>
            {
                var dept = doc.ConvertTo<Department>();
                dept.Id = doc.Id;
                return dept;
            }).ToList();
        }

        // Lấy phòng ban theo ID
        public async Task<Department?> GetDepartmentByIdAsync(string id)
        {
            var doc = await _firestoreDb.Collection("departments").Document(id).GetSnapshotAsync();
            if (doc.Exists)
            {
                var dept = doc.ConvertTo<Department>();
                dept.Id = doc.Id;
                return dept;
            }
            return null;
        }

        public DocumentReference GetDepartmentReferenceById(string id)
        {
            return _firestoreDb.Collection("departments").Document(id);
        }

        // sinh code
        public async Task<string> GenerateNextAssetCodeAsync()
        {
            var assets = await GetAllAssetsAsync();
            var existingCodes = assets
                .Select(a => a.Code)
                .Where(code => !string.IsNullOrEmpty(code) && code.StartsWith("AS"))
                .Select(code => int.TryParse(code.Substring(2), out int num) ? num : 0)
                .ToList();

            int nextNumber = existingCodes.Count > 0 ? existingCodes.Max() + 1 : 1;
            return $"AS{nextNumber:D2}"; // AS01, AS02, AS03...
        }

        public async Task<User?> GetUserByIdAsync(string id)
        {
            var doc = await _firestoreDb.Collection("users").Document(id).GetSnapshotAsync();
            if (doc.Exists)
            {
                var user = doc.ConvertTo<User>();
                user.Id = doc.Id;
                return user;
            }
            return null;
        }

        public async Task<List<Asset>> GetAvailableAssetsAsync()
        {
            var snapshot = await _firestoreDb.Collection("assets").WhereEqualTo("Status", "Mới").GetSnapshotAsync();
            return snapshot.Documents.Select(d =>
            {
                var asset = d.ConvertTo<Asset>();
                asset.Id = d.Id;
                return asset;
            }).ToList();
        }

        // public async Task CreateAssignmentAsync(string assetId, string assignee)
        // {
        //     var assetRef = _firestoreDb.Collection("assets").Document(assetId);

        //     var assignment = new Assignment
        //     {
        //         AssetRef = assetRef,
        //         Assignee = assignee,
        //         AssignedDate = DateTime.UtcNow,
        //         ReturnedDate = null
        //     };

        //     await _firestoreDb.Collection("assignments").AddAsync(assignment);

        //     // Cập nhật trạng thái tài sản thành "Đang sử dụng"
        //     await assetRef.UpdateAsync("Status", "Đang sử dụng");
        // }

        public async Task<List<AssetAssignmentViewModel>> GetCurrentAssignmentsAsync()
        {
            var snapshot = await _firestoreDb.Collection("assignments")
                .WhereEqualTo("ReturnedDate", null)
                .GetSnapshotAsync();

            var list = new List<AssetAssignmentViewModel>();

            foreach (var doc in snapshot.Documents)
            {
                var assignment = doc.ConvertTo<Assignment>();
                assignment.Id = doc.Id;

                // Load asset từ reference
                var assetSnap = await assignment.AssetRef.GetSnapshotAsync();
                var asset = assetSnap.ConvertTo<Asset>();

                list.Add(new AssetAssignmentViewModel
                {
                    Id = assignment.Id,
                    AssigneeName = assignment.AssigneeId,
                    AssignedDate = assignment.AssignedDate ?? DateTime.MinValue,
                    AssetName = asset?.Name ?? "(N/A)",
                    AssetCode = asset?.Code ?? "(N/A)"
                });
            }

            return list;
        }

        public async Task MarkAsReturnedAsync(string assignmentId)
        {
            var docRef = _firestoreDb.Collection("assignments").Document(assignmentId);
            var snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists) return;

            var assignment = snapshot.ConvertTo<Assignment>();
            assignment.Id = snapshot.Id;

            // Cập nhật ngày trả
            await docRef.UpdateAsync("ReturnedDate", Timestamp.FromDateTime(DateTime.UtcNow));

            // Cập nhật lại trạng thái tài sản thành "Mới"
            if (assignment.AssetRef != null)
            {
                await assignment.AssetRef.UpdateAsync("Status", "Mới");
            }
        }

        //lấy giao nhận
        public async Task<List<AssetAssignmentViewModel>> GetAllAssignmentsAsync()
        {
            var snapshot = await _firestoreDb.Collection("assignments").GetSnapshotAsync();

            var list = new List<AssetAssignmentViewModel>();

            foreach (var doc in snapshot.Documents)
            {
                var assignment = doc.ConvertTo<Assignment>();
                assignment.Id = doc.Id;

                var assetSnap = await assignment.AssetRef.GetSnapshotAsync();
                var asset = assetSnap.ConvertTo<Asset>();

                list.Add(new AssetAssignmentViewModel
                {
                    Id = assignment.Id,
                    AssigneeName = assignment.AssigneeId,
                    AssignedDate = assignment.AssignedDate ?? DateTime.MinValue,
                    AssetCode = asset?.Code ?? "N/A",
                    AssetName = asset?.Name ?? "N/A",
                    returnedDate = assignment.ReturnedDate ?? DateTime.MinValue,
                });
            }

            return list;
        }

        public async Task<List<AssetAssignmentViewModel>> GetAssignmentsByAssetIdAsync(string assetId)
        {
            var assetRef = _firestoreDb.Collection("assets").Document(assetId);

            var snapshot = await _firestoreDb.Collection("assignments")
                .WhereEqualTo("AssetRef", assetRef)
                .GetSnapshotAsync();

            var list = new List<AssetAssignmentViewModel>();

            foreach (var doc in snapshot.Documents)
            {
                var assignment = doc.ConvertTo<Assignment>();
                assignment.Id = doc.Id;

                var assetSnap = await assignment.AssetRef.GetSnapshotAsync();
                var asset = assetSnap.ConvertTo<Asset>();

                list.Add(new AssetAssignmentViewModel
                {
                    Id = assignment.Id,
                    AssigneeName = assignment.AssigneeId,
                    AssignedDate = assignment.AssignedDate ?? DateTime.MinValue,
                    returnedDate = assignment.ReturnedDate ?? DateTime.MinValue,
                    AssetName = asset?.Name ?? "(N/A)",
                    AssetCode = asset?.Code ?? "(N/A)"
                });
            }

            return list;
        }

        public async Task CreateAssignmentAsync(string assetId, string assigneeId, string assignedById)
        {
            var assetRef = _firestoreDb.Collection("assets").Document(assetId);

            // Lấy tên người nhận từ ID
            var userDoc = await _firestoreDb.Collection("users").Document(assigneeId).GetSnapshotAsync();
            string assigneeName = userDoc.Exists && userDoc.ContainsField("name")
                ? userDoc.GetValue<string>("name")
                : "(Không rõ)";

            Console.WriteLine($"[LOG] Assignee: {assigneeName}");

            // Lấy tên người giao từ ID
            var assignerDoc = await _firestoreDb.Collection("users").Document(assignedById).GetSnapshotAsync();
            string assignerEmail = assignerDoc.Exists && assignerDoc.ContainsField("email")
                ? assignerDoc.GetValue<string>("email")
                : "(Không rõ)";

            Console.WriteLine($"[LOG] Assigner: {assignerEmail}");

            var assignment = new Assignment
            {
                AssetRef = assetRef,
                AssigneeId = assigneeId,
                AssigneeName = assigneeName,
                AssignedBy = assignerEmail,
                AssignedDate = DateTime.UtcNow,
                ReturnedDate = null
            };

            await _firestoreDb.Collection("assignments").AddAsync(assignment);
            await assetRef.UpdateAsync("Status", "Đang sử dụng");
        }


        public async Task ReturnAssignmentAsync(string assignmentId)
        {
            var assignmentRef = _firestoreDb.Collection("assignments").Document(assignmentId);
            var snapshot = await assignmentRef.GetSnapshotAsync();

            if (!snapshot.Exists) return;

            var assignment = snapshot.ConvertTo<Assignment>();
            var assetRef = assignment.AssetRef;

            await assignmentRef.UpdateAsync(new Dictionary<string, object>
            {
                { "ReturnedDate", DateTime.UtcNow }
            });

            await assetRef.UpdateAsync("Status", "Mới");
        }

        public async Task<List<AssetAssignmentViewModel>> GetAssignmentsInUseViewModelAsync()
        {
            var assignmentDocs = await _firestoreDb.Collection("assignments")
                .WhereEqualTo("ReturnedDate", null)
                .GetSnapshotAsync();

            var list = new List<AssetAssignmentViewModel>();

            foreach (var doc in assignmentDocs.Documents)
            {
                var assignment = doc.ConvertTo<Assignment>();
                assignment.Id = doc.Id;

                var assetSnapshot = await assignment.AssetRef.GetSnapshotAsync();
                var assetData = assetSnapshot.ToDictionary();

                string assetCode = assetData.ContainsKey("Code") ? assetData["Code"].ToString() : "(không rõ)";
                string assetName = assetData.ContainsKey("Name") ? assetData["Name"].ToString() : "(không rõ)";

                list.Add(new AssetAssignmentViewModel
                {
                    Id = assignment.Id,
                    AssetId = assignment.AssetRef.Id,
                    AssetCode = assetCode,
                    AssetName = assetName,
                    AssigneeId = assignment.AssigneeId,
                    AssigneeName = assignment.AssigneeName,
                    AssignedById = assignment.AssignedBy,
                    AssignedDate = assignment.AssignedDate ?? DateTime.MinValue,
                    returnedDate = assignment.ReturnedDate
                });
            }

            return list;
        }


        public async Task<List<Assignment>> GetAssignmentsInUseAsync()
        {
            var snapshot = await _firestoreDb.Collection("assignments")
                .WhereEqualTo("ReturnedDate", null)
                .GetSnapshotAsync();

            var assignments = new List<Assignment>();

            foreach (var doc in snapshot.Documents)
            {
                var assignment = doc.ConvertTo<Assignment>();
                assignment.Id = doc.Id;
                assignments.Add(assignment);
            }

            return assignments;
        }

        public async Task<List<User>> SearchUsersByNameAsync(string keyword)
        {
            //Console.WriteLine($" Bắt đầu tìm kiếm người dùng với từ khóa: {keyword}");

            var query = _firestoreDb.Collection("users")
                .WhereGreaterThanOrEqualTo("IdUser", keyword)
                .WhereLessThanOrEqualTo("IdUser", keyword + "\uf8ff")
                .Limit(10);

            var snapshot = await query.GetSnapshotAsync();
            //Console.WriteLine($" Số lượng kết quả tìm thấy: {snapshot.Documents.Count}");
            var results = new List<User>();

            foreach (var doc in snapshot.Documents)
            {
                var user = doc.ConvertTo<User>();
                user.Id = doc.Id;
                results.Add(user);
            }

            return results;
        }

        public async Task<List<User>> SearchUsersAsync(string keyword)
        {
            var users = new List<User>();

            var usersRef = _firestoreDb.Collection("users");

            // Tìm theo name
            var nameQuery = usersRef
                .WhereGreaterThanOrEqualTo("name", keyword)
                .WhereLessThanOrEqualTo("name", keyword + "\uf8ff")
                .Limit(10);

            var nameSnapshot = await nameQuery.GetSnapshotAsync();
            users.AddRange(nameSnapshot.Documents.Select(doc =>
            {
                var user = doc.ConvertTo<User>();
                user.Id = doc.Id;
                return user;
            }));

            // Tìm theo email
            if (users.Count < 10)
            {
                var emailQuery = usersRef.WhereEqualTo("email", keyword).Limit(1);
                var emailSnapshot = await emailQuery.GetSnapshotAsync();
                foreach (var doc in emailSnapshot.Documents)
                {
                    if (!users.Any(u => u.Id == doc.Id))
                    {
                        var user = doc.ConvertTo<User>();
                        user.Id = doc.Id;
                        users.Add(user);
                    }
                }
            }

            // Tìm theo IdUser 
            if (users.Count < 10)
            {
                var idUserQuery = usersRef.WhereEqualTo("IdUser", keyword).Limit(1);
                var idUserSnapshot = await idUserQuery.GetSnapshotAsync();
                foreach (var doc in idUserSnapshot.Documents)
                {
                    if (!users.Any(u => u.Id == doc.Id))
                    {
                        var user = doc.ConvertTo<User>();
                        user.Id = doc.Id;
                        users.Add(user);
                    }
                }
            }

            return users;
        }

        public async Task<List<AssetHistoryViewModel>> GetAssetHistoryAsync()
        {
            var assignmentDocs = await _firestoreDb.Collection("assignments").GetSnapshotAsync();
            var list = new List<AssetHistoryViewModel>();

            foreach (var doc in assignmentDocs.Documents)
            {
                try
                {
                    var data = doc.ToDictionary();

                    if (!data.ContainsKey("AssetRef") || data["AssetRef"] is not DocumentReference assetRef)
                    {
                        Console.WriteLine($"[WARN] Assignment '{doc.Id}' is missing a valid AssetRef.");
                        continue;
                    }

                    var assetSnapshot = await assetRef.GetSnapshotAsync();
                    if (assetSnapshot == null || !assetSnapshot.Exists)
                    {
                        Console.WriteLine($"[WARN] Asset document not found for AssetRef: {assetRef.Id}");
                        continue;
                    }

                    var assetData = assetSnapshot.ToDictionary();

                    var assetCode = assetData.ContainsKey("Code") ? assetData["Code"]?.ToString() ?? "(null)" : "(missing)";
                    var assetName = assetData.ContainsKey("Name") ? assetData["Name"]?.ToString() ?? "(null)" : "(missing)";
                    var assetCategory = assetData.ContainsKey("Category") ? assetData["Category"]?.ToString() ?? "(null)" : "(missing)";

                    list.Add(new AssetHistoryViewModel
                    {
                        AssetId = assetRef.Id,
                        AssetCode = assetCode,
                        AssetName = assetName,
                        AssetCategory = assetCategory,
                        AssigneeName = data.ContainsKey("AssigneeName") ? data["AssigneeName"]?.ToString() ?? "(null)" : "(missing)",
                        AssignedBy = data.ContainsKey("AssignedBy") ? data["AssignedBy"]?.ToString() ?? "(null)" : "(missing)",
                        AssignedDate = data.ContainsKey("AssignedDate") && data["AssignedDate"] is Timestamp ts1 ? ts1.ToDateTime() : null,
                        ReturnedDate = data.ContainsKey("ReturnedDate") && data["ReturnedDate"] is Timestamp ts2 ? ts2.ToDateTime() : null
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to process assignment '{doc.Id}': {ex.Message}");
                }
            }

            return list.OrderByDescending(h => h.AssignedDate).ToList();
        }

        // public async Task<IActionResult> AssignAsset(string assetId, string assigneeId)
        // {
        //     var assigneeDoc = await _firestoreDb.Collection("users").Document(assigneeId).GetSnapshotAsync();

        //     string assigneeName = "(unknown)";
        //     if (assigneeDoc.Exists && assigneeDoc.TryGetValue("FullName", out string fullName))
        //     {
        //         assigneeName = fullName;
        //     }

        //     var assignment = new Dictionary<string, object>
        //     {
        //         { "AssetRef", _firestoreDb.Collection("assets").Document(assetId) },
        //         { "AssigneeId", assigneeId },
        //         { "AssigneeName", assigneeName },  
        //         { "AssignedBy", User.Identity.Name },
        //         { "AssignedDate", Timestamp.GetCurrentTimestamp() },
        //         { "ReturnedDate", null }
        //     };

        //     await _firestoreDb.Collection("assignments").AddAsync(assignment);

        //     return RedirectToAction("Index");
        // }

        public async Task CreateMaintenanceAsync(Maintenance maintenance)
        {
            await _firestoreDb.Collection("maintenance_logs").AddAsync(maintenance);
        }

        public async Task<List<MaintenanceViewModel>> GetAllMaintenancesAsync()
        {
            var snapshot = await _firestoreDb.Collection("maintenance_logs").GetSnapshotAsync();
            var list = new List<MaintenanceViewModel>();

            foreach (var doc in snapshot.Documents)
            {
                var data = doc.ConvertTo<Maintenance>();
                data.Id = doc.Id;

                var assetSnap = await data.AssetRef.GetSnapshotAsync();
                var asset = assetSnap.ConvertTo<Asset>();

                list.Add(new MaintenanceViewModel
                {
                    Id = data.Id,
                    //AssetId = assetSnap.Id,
                    AssetCode = asset?.Code,
                    AssetName = asset?.Name,
                    Description = data.Description,
                    MaintenanceDate = data.MaintenanceDate,
                    Cost = data.Cost,
                    PerformedBy = data.PerformedBy
                });
            }

            return list.OrderByDescending(x => x.MaintenanceDate).ToList();
        }

        public async Task UpdateMaintenanceAsync(MaintenanceViewModel vm)
        {
            var maintenanceRef = _firestoreDb.Collection("maintenance_logs").Document(vm.Id);

            var updates = new Dictionary<string, object>
            {
                ["description"] = vm.Description,
                ["maintenanceDate"] = vm.MaintenanceDate,
                ["cost"] = vm.Cost,
                ["performedBy"] = vm.PerformedBy
            };

            await maintenanceRef.UpdateAsync(updates);
        }



    }
}
