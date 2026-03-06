using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace OmiliaWebhook.Auth;

public class JwtValidator
{
    private static readonly HttpClient Http = new();
    private static readonly JsonWebTokenHandler TokenHandler = new();

    private readonly string _jwksUri;
    private readonly string? _issuer;
    private readonly string? _audience;

    private JsonWebKeySet? _cachedKeys;
    private DateTime _cacheExpiry;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public JwtValidator(string jwksUri, string? issuer, string? audience)
    {
        _jwksUri = jwksUri;
        _issuer = issuer;
        _audience = audience;
    }

    public async Task<TokenValidationResult> ValidateTokenAsync(string token)
    {
        var keys = await GetSigningKeysAsync();

        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = keys.GetSigningKeys(),
            ValidateIssuer = _issuer is not null,
            ValidIssuer = _issuer,
            ValidateAudience = _audience is not null,
            ValidAudience = _audience,
            ValidateLifetime = true,
        };

        return await TokenHandler.ValidateTokenAsync(token, parameters);
    }

    private async Task<JsonWebKeySet> GetSigningKeysAsync()
    {
        if (_cachedKeys is not null && DateTime.UtcNow < _cacheExpiry)
            return _cachedKeys;

        await _cacheLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_cachedKeys is not null && DateTime.UtcNow < _cacheExpiry)
                return _cachedKeys;

            var json = await Http.GetStringAsync(_jwksUri);
            _cachedKeys = new JsonWebKeySet(json);
            _cacheExpiry = DateTime.UtcNow.AddHours(1);
            return _cachedKeys;
        }
        finally
        {
            _cacheLock.Release();
        }
    }
}
