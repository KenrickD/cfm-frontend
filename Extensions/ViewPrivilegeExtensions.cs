namespace cfm_frontend.Extensions
{
    /// <summary>
    /// Extension methods for privilege checks in Razor views
    /// </summary>
    public static class ViewPrivilegeExtensions
    {
        public static bool CanView(this ISession session, string moduleName, string pageName)
        {
            return session.GetPrivileges()?.CanViewPage(moduleName, pageName) ?? false;
        }

        public static bool CanAdd(this ISession session, string moduleName, string pageName)
        {
            return session.GetPrivileges()?.CanAddToPage(moduleName, pageName) ?? false;
        }

        public static bool CanEdit(this ISession session, string moduleName, string pageName)
        {
            return session.GetPrivileges()?.CanEditPage(moduleName, pageName) ?? false;
        }

        public static bool CanDelete(this ISession session, string moduleName, string pageName)
        {
            return session.GetPrivileges()?.CanDeleteFromPage(moduleName, pageName) ?? false;
        }

        public static bool HasModuleAccess(this ISession session, string moduleName)
        {
            return session.GetPrivileges()?.HasModuleAccess(moduleName) ?? false;
        }
    }
}
