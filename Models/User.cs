using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;

namespace AssetManager.Models
{
    [FirestoreData]
    public class User
    {
        [FirestoreDocumentId]
        public string? Id { get; set; }
        
        public string? Uid { get; set; }

        [FirestoreProperty("IdUser")]
        [Required(ErrorMessage = "Vui lòng nhập mã người dùng.")]
        public string IdUser { get; set; }

        [FirestoreProperty("name")]
        [Required(ErrorMessage = "Vui lòng nhập tên người dùng.")]
        public string Name { get; set; }

        [FirestoreProperty("email")]
        [Required(ErrorMessage = "Vui lòng nhập email người dùng.")]
        public string Email { get; set; }

        [FirestoreProperty("role")]
        [Required(ErrorMessage = "Vui lòng chọn vai trò của người dùng.")]
        public string Role { get; set; }
    }
}
