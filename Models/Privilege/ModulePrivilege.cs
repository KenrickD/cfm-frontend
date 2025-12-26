namespace cfm_frontend.Models.Privilege
{
    /// <summary>
    /// Represents a module with its collection of page privileges
    /// </summary>
    public class ModulePrivilege
    {
        public string ModuleName { get; set; } = string.Empty;
        public List<PagePrivilege> Pages { get; set; } = new List<PagePrivilege>();

        /// <summary>
        /// Computed property: Returns true if user has at least one accessible page in this module
        /// </summary>
        public bool HasAnyAccessiblePage => Pages.Any(p => p.CanView);
    }
}
