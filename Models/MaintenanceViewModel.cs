namespace AssetManager.Models
{
    public class MaintenanceViewModel
    {
        public string? Id { get; set; }
        public string? AssetCode { get; set; }
        public string? AssetName { get; set; }
        public string? Description { get; set; }
        public DateTime MaintenanceDate { get; set; }
        public double Cost { get; set; }
        public string? PerformedBy { get; set; }
    }
}
