using System.Security.Claims;
using System.Text;
using MaichessBotArenaService.Arena;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace MaichessBotArenaService.Tests;

public sealed class ServiceTokenMinterTests
{
    private const string Key = "this-is-a-sufficiently-long-test-signing-key-0123456789";

    [Fact]
    public async Task Mint_ProducesAValidTokenCarryingTheUserId()
    {
        ServiceTokenMinter minter = new(Key);

        string token = minter.Mint("user-9", DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5));

        TokenValidationResult result = await new JsonWebTokenHandler().ValidateTokenAsync(
            token,
            new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
            });

        Assert.True(result.IsValid);
        Assert.Equal("user-9", result.ClaimsIdentity.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }
}
