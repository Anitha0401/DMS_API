using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarineDMS_Infrastructure.Authentication
{
    public class JwtSettings
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; init; } = string.Empty;
        public string Audience { get; init; } = string.Empty;
        public string SecretKey { get; init; } = string.Empty;
        public int AccessTokenExpirationMinutes { get; init; }
        public int RefreshTokenExpirationDays { get; init; }

    }
}
