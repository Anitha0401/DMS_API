namespace DMS.Data.EF.DTO.Manuals
{
    public class ManualLogDto
    {
        public long DM_ManualLogID { get; set; }

        public long? DM_ManualID { get; set; }

        public long? DM_ManualVersionID { get; set; }

        public DateTime? Logdate { get; set; }
        
        public string? UserId { get; set; }
        
        public string? UserName { get; set; }
        
        public string? Activity { get; set; }
        
        public string? Remarks { get; set; }
    }
}