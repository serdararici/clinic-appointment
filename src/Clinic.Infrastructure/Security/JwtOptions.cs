namespace Clinic.Infrastructure.Security;

/// <summary>JWT settings, read from the "Jwt" configuration section.</summary>
public class JwtOptions
{
    /// <summary>HS256 needs a key of at least 256 bits (32 bytes).</summary>
    public const int MinKeyLength = 32;

    public string Key { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;

    /// <summary>How long a token stays valid.</summary>
    public int ExpiryHours { get; init; } = 8;

    public void Validate()
    {
        if (Key.Length < MinKeyLength)
        {
            throw new InvalidOperationException(
                $"Configuration 'Jwt:Key' must be at least {MinKeyLength} characters long. In real deployments set it through an environment variable (Jwt__Key), not the committed settings file.");
        }

        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("Configuration 'Jwt:Issuer' and 'Jwt:Audience' are required.");
        }
    }
}
