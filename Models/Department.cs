using Google.Cloud.Firestore;

namespace AssetManager.Models
{
    [FirestoreData]
    public class Department
    {
        [FirestoreDocumentId]
        public string Id { get; set; }

        [FirestoreProperty("name")]
        public string Name { get; set; }
    }
}
