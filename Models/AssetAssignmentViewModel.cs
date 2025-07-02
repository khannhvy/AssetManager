namespace AssetManager.Models
{
    public class AssetAssignmentViewModel
    {
        public string Id { get; set; }
        //public string Assignee { get; set; }    
        public DateTime AssignedDate { get; set; }
        public string AssetId { get; set; }

        public string AssetCode { get; set; }
        public string AssetName { get; set; }

        public string AssigneeName { get; set; }

        public string AssignedById { get; set; }

        public DateTime? returnedDate { get; set; }

        public string AssigneeId { get; set; }

    }
}
