using Google.Cloud.Firestore;
using System;

namespace AssetManager.Models
{
    [FirestoreData]
    public class Maintenance
    {
        [FirestoreDocumentId]
        public string Id { get; set; }

        [FirestoreProperty]
        public DocumentReference AssetRef { get; set; }

        [FirestoreProperty("description")]
        public string Description { get; set; }

        [FirestoreProperty("maintenanceDate")]
        public DateTime MaintenanceDate { get; set; }

        [FirestoreProperty("cost")]
        public double Cost { get; set; }

        [FirestoreProperty("performedBy")]
        public string PerformedBy { get; set; }
    }
}
