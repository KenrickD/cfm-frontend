using System.IdentityModel.Tokens.Jwt;

namespace cfm_frontend.Helpers
{
    /// <summary>
    /// Helper class for JWT token operations including expiration checking
    /// </summary>
    public static class JwtTokenHelper
    {
        private static readonly JwtSecurityTokenHandler _tokenHandler = new JwtSecurityTokenHandler();

        /// <summary>
        /// Checks if a JWT token is near expiry within the specified threshold
        /// </summary>
        /// <param name="token">The JWT token string</param>
        /// <param name="minutesBeforeExpiry">Minutes before expiry to consider "near expiry" (default: 5)</param>
        /// <returns>True if token expires within the threshold or is invalid, false otherwise</returns>
        /// <remarks>
        /// The exp claim is expected to be a Unix timestamp (NumericDate per JWT spec).
        /// JwtSecurityToken.ValidTo automatically converts this to DateTime UTC.
        /// </remarks>
        public static bool IsTokenNearExpiry(string token, int minutesBeforeExpiry = 5)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return true;

                var jwtToken = _tokenHandler.ReadJwtToken(token);

                // ValidTo returns DateTime in UTC (automatically converted from Unix timestamp in exp claim)
                var expiryTime = jwtToken.ValidTo;
                var threshold = DateTime.UtcNow.AddMinutes(minutesBeforeExpiry);

                return expiryTime <= threshold;
            }
            catch
            {
                // If can't decode, assume expired (safe default)
                return true;
            }
        }

        /// <summary>
        /// Checks if a JWT token is already expired
        /// </summary>
        /// <param name="token">The JWT token string</param>
        /// <returns>True if token is expired or invalid, false otherwise</returns>
        public static bool IsTokenExpired(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return true;

                var jwtToken = _tokenHandler.ReadJwtToken(token);
                return jwtToken.ValidTo <= DateTime.UtcNow;
            }
            catch
            {
                // If can't decode, assume expired (safe default)
                return true;
            }
        }

        /// <summary>
        /// Gets the expiration time from a JWT token
        /// </summary>
        /// <param name="token">The JWT token string</param>
        /// <returns>Expiration DateTime if successful, null otherwise</returns>
        public static DateTime? GetTokenExpiration(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return null;

                var jwtToken = _tokenHandler.ReadJwtToken(token);
                return jwtToken.ValidTo;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the remaining time until token expiration
        /// </summary>
        /// <param name="token">The JWT token string</param>
        /// <returns>TimeSpan until expiration, or null if token is invalid</returns>
        public static TimeSpan? GetTimeUntilExpiration(string token)
        {
            try
            {
                var expiration = GetTokenExpiration(token);
                if (expiration == null)
                    return null;

                var remaining = expiration.Value - DateTime.UtcNow;
                return remaining;
            }
            catch
            {
                return null;
            }
        }
    }
}
