namespace cfm_frontend.Models.Privilege
{
    /// <summary>
    /// Complete privilege tree for a user with helper methods for privilege checks
    /// </summary>
    public class UserPrivileges
    {
        public List<ModulePrivilege> Modules { get; set; } = new List<ModulePrivilege>();
        public DateTime LoadedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Get page privilege for a specific module and page
        /// </summary>
        public PagePrivilege? GetPagePrivilege(string moduleName, string pageName)
        {
            return Modules
                .FirstOrDefault(m => m.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                ?.Pages
                .FirstOrDefault(p => p.PageName.Equals(pageName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Check if user can view a specific page
        /// </summary>
        public bool CanViewPage(string moduleName, string pageName)
        {
            return GetPagePrivilege(moduleName, pageName)?.CanView ?? false;
        }

        /// <summary>
        /// Check if user can add to a specific page
        /// </summary>
        public bool CanAddToPage(string moduleName, string pageName)
        {
            return GetPagePrivilege(moduleName, pageName)?.CanAdd ?? false;
        }

        /// <summary>
        /// Check if user can edit on a specific page
        /// </summary>
        public bool CanEditPage(string moduleName, string pageName)
        {
            return GetPagePrivilege(moduleName, pageName)?.CanEdit ?? false;
        }

        /// <summary>
        /// Check if user can delete from a specific page
        /// </summary>
        public bool CanDeleteFromPage(string moduleName, string pageName)
        {
            return GetPagePrivilege(moduleName, pageName)?.CanDelete ?? false;
        }

        /// <summary>
        /// Check if user has access to any page in a module
        /// </summary>
        public bool HasModuleAccess(string moduleName)
        {
            return Modules
                .FirstOrDefault(m => m.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                ?.HasAnyAccessiblePage ?? false;
        }
    }
}
