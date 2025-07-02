namespace AssetManager.Models
{
    public class AssetHistoryViewModel
    {
        public string AssetId { get; set; }
        public string AssetCode { get; set; }
        public string AssetName { get; set; }
        public string AssetCategory { get; set; }

        public string AssigneeName { get; set; }
        public string AssignedBy { get; set; }


        public DateTime? AssignedDate { get; set; }
        public DateTime? ReturnedDate { get; set; }
    }
}
