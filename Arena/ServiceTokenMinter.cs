using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace MaichessBotArenaService.Arena;

// Mints short-lived JWTs signed with the shared platform key so the arena can
// call Match Maker's bearer-protected bot-vs-bot endpoint on behalf of the user
// who created a setup (attributing the spawned games to them).
internal sealed class ServiceTokenMinter(string signingKey)
{
    private readonly SigningCredentials credentials = new(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256);

    internal string Mint(string userId, DateTimeOffset now, TimeSpan lifetime)
    {
        SecurityTokenDescriptor descriptor = new()
        {
            Subject = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)]),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = now.Add(lifetime).UtcDateTime,
            SigningCredentials = credentials,
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
