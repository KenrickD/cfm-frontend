namespace cfm_frontend.Models.Privilege
{
    /// <summary>
    /// Represents page-level CRUD permissions for a specific page within a module
    /// </summary>
    public class PagePrivilege
    {
        public string PageName { get; set; } = string.Empty;
        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }
}
