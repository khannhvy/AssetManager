using Google.Cloud.Firestore;

namespace AssetManager.Models
{
    [FirestoreData]
    public class User
    {
        [FirestoreDocumentId]
        public string Id { get; set; }
        
        public string Uid { get; set; }

        [FirestoreProperty("IdUser")]
        public string IdUser { get; set; }
        
        [FirestoreProperty("name")]
        public string Name { get; set; }

        [FirestoreProperty("email")]
        public string Email { get; set; }

        [FirestoreProperty("role")]
        public string Role { get; set; }
    }
}
