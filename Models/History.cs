using Google.Cloud.Firestore;
using System;

namespace AssetManager.Models
{
    [FirestoreData]
    public class History
    {
        [FirestoreDocumentId]
        public string Id { get; set; }

        [FirestoreProperty]
        public string AssetId { get; set; }

        [FirestoreProperty]
        public string Action { get; set; } // VD: "Mượn", "Trả", "Bảo trì", "Sửa chữa", "Thay đổi trạng thái"

        [FirestoreProperty]
        public string PerformedBy { get; set; } // Email/người thực hiện

        [FirestoreProperty]
        public string Note { get; set; }

        [FirestoreProperty]
        public Timestamp Timestamp { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);  // Thời gian thực hiện
    }
}
