using Google.Cloud.Firestore;
using System;
using System.ComponentModel.DataAnnotations;

namespace AssetManager.Models
{
    [FirestoreData]
    public class Asset
    {
        [FirestoreDocumentId]
        public string? Id { get; set; }

        [Required(ErrorMessage = "Không được bỏ trống")]
        [FirestoreProperty]
        public string? Code { get; set; }

        [Required(ErrorMessage = "Không được bỏ trống")]
        [FirestoreProperty]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Không được bỏ trống")]
        [FirestoreProperty]
        public string? Category { get; set; }

        [FirestoreProperty]
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Không được bỏ trống")]
        [FirestoreProperty]
        // public string? Location { get; set; }

        // [Required(ErrorMessage = "Không được bỏ trống")]
        // [FirestoreProperty]
        public long OriginalValue { get; set; }

        [Required(ErrorMessage = "Không được bỏ trống")]
        [DataType(DataType.Date)]
        [FirestoreProperty]
        public DateTime? PurchaseDate { get; set; }

        [Required(ErrorMessage = "Không được bỏ trống")]
        [FirestoreProperty]
        public string? Status { get; set; }

        // Liên kết đến phòng ban trong Firestore
        [FirestoreProperty]
        public DocumentReference? Department { get; set; }

        // Dùng để binding từ form
        [Required(ErrorMessage = "Không được bỏ trống")]
        public string? DepartmentId { get; set; }

        [FirestoreProperty]
        public Timestamp CreatedAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);
    }
}
