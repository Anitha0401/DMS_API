namespace DMS.API.Auth;

/// <summary>Settings for the tokens this API issues. Set Jwt__Key as an environment variable or user-secret.</summary>
public sealed class JwtOptions
{
    public const string Section = "Jwt";

    /// <summary>At least 32 random characters. Never commit a real value.</summary>
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpireMinutes { get; set; } = 480;
}

/// <summary>Claim names written into the token and read back by controllers.</summary>
public static class DmsClaims
{
    public const string UserId = "sub";
    public const string Name = "name";
    public const string Role = "role";
    public const string Email = "email";
}
