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

        //  CRUD tài sản
        public async Task<List<Asset>> GetAllAssetsAsync()
        {
            var snapshot = await _firestoreDb.Collection(CollectionName).GetSnapshotAsync();
            return snapshot.Documents.Select(d => d.ConvertTo<Asset>()).ToList();
        }

        public async Task<Asset?> GetAssetByIdAsync(string id)
        {
            var docRef = _firestoreDb.Collection(CollectionName).Document(id);
            var snapshot = await docRef.GetSnapshotAsync();
            return snapshot.Exists ? snapshot.ConvertTo<Asset>() : null;
        }

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


    }
}
