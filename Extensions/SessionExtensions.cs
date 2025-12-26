using cfm_frontend.Models;
using cfm_frontend.Models.Privilege;
using System.Text.Json;

namespace cfm_frontend.Extensions
{
    /// <summary>
    /// Extension methods for type-safe session management
    /// </summary>
    public static class SessionExtensions
    {
        private const string UserSessionKey = "UserSession";
        private const string PrivilegesKey = "UserPrivileges";

        // UserInfo methods
        public static UserInfo? GetUserInfo(this ISession session)
        {
            var userSessionJson = session.GetString(UserSessionKey);
            if (string.IsNullOrEmpty(userSessionJson))
                return null;

            return JsonSerializer.Deserialize<UserInfo>(
                userSessionJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
        }

        public static void SetUserInfo(this ISession session, UserInfo userInfo)
        {
            session.SetString(UserSessionKey, JsonSerializer.Serialize(userInfo));
        }

        // Privilege methods
        public static UserPrivileges? GetPrivileges(this ISession session)
        {
            var privilegesJson = session.GetString(PrivilegesKey);
            if (string.IsNullOrEmpty(privilegesJson))
                return null;

            return JsonSerializer.Deserialize<UserPrivileges>(
                privilegesJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
        }

        public static void SetPrivileges(this ISession session, UserPrivileges privileges)
        {
            session.SetString(PrivilegesKey, JsonSerializer.Serialize(privileges));
        }

        public static void ClearPrivileges(this ISession session)
        {
            session.Remove(PrivilegesKey);
        }
    }
}
