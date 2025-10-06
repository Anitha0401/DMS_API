namespace DMS.Application
{
    public class ManualContentDto
    {
        public long DM_ManualID { get; set; }

        public string ManualCode { get; set; } = string.Empty;
        
        public string? ManualName { get; set; } = string.Empty;
        
        public string ManualNo { get; set; } = string.Empty;
        
        public long DM_CategoryID { get; set; }
        
        public string CategoryName { get; set; } = string.Empty;
        
        public string TextContents { get; set; } = string.Empty;
    }
}