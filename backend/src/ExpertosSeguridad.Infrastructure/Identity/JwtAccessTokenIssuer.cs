using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ExpertosSeguridad.Application.Abstractions;
using ExpertosSeguridad.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ExpertosSeguridad.Infrastructure.Identity;

/// <summary>
/// Genera el JWT firmado que el frontend presenta en cada llamada. El rol viaja como claim para
/// que la API pueda filtrar endpoints por rol, pero no es la fuente de verdad: en cada request se
/// contrasta con la cuenta en la base de datos, y las reglas de pertenencia se verifican contra
/// los datos persistidos en los casos de uso.
/// </summary>
public sealed class JwtAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly JwtOptions _options;
    private readonly IClock _clock;

    public JwtAccessTokenIssuer(IOptions<JwtOptions> options, IClock clock)
    {
        _options = options.Value;
        _clock = clock;
    }

    public AccessToken Issue(User user)
    {
        var issuedAt = _clock.UtcNow;
        var expiresAt = issuedAt.AddMinutes(_options.LifetimeMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
