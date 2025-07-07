namespace AssetManager.Models
{
    public class Dashboard
    {
        public List<Asset> WarningAssets { get; set; } = new();
        public List<CategoryStat> AssetsByCategory { get; set; } = new();
        public List<StatusStat> AssetsByStatus { get; set; } = new();

        public int UsedAssetsCount { get; set; }
        public int FreeAssetsCount { get; set; }
    }

    public class CategoryStat
    {
        public string Category { get; set; }
        public int Count { get; set; }
    }

    public class StatusStat
    {
        public string Status { get; set; }
        public int Count { get; set; }
    }
}
