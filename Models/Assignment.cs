using Google.Cloud.Firestore;
using System;
using System.ComponentModel.DataAnnotations;

namespace AssetManager.Models
{
    [FirestoreData]
    public class Assignment
    {
        [FirestoreDocumentId]
        public string? Id { get; set; }

        [FirestoreProperty]
        public string AssignedBy { get; set; }

        [FirestoreProperty]
        public DocumentReference AssetRef { get; set; }

        [FirestoreProperty]
        public string AssigneeId { get; set; }

        [FirestoreProperty]
        public DateTime? AssignedDate { get; set; }

        [FirestoreProperty]
        public DateTime? ReturnedDate { get; set; }

        // [FirestoreProperty]
        // public string AssigneeName { get; set; } = "";

        [FirestoreProperty]
        public string AssigneeName { get; set; }


        
    }
}
