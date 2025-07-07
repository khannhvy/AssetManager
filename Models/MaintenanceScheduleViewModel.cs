namespace AssetManager.Models
{
    public class MaintenanceScheduleViewModel
    {
        public string AssetId { get; set; }
        public string AssetCode { get; set; }
        public string AssetName { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public DateTime? LastMaintenanceDate { get; set; }
        public DateTime NextMaintenanceDate { get; set; }
        public string Status { get; set; }
    }
}
