using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace WerewolfParty_Server.Service;

public class JwtService(IConfiguration config)
{
    public string GenerateToken()
    {
        var handler = new JwtSecurityTokenHandler();
        var privateKeyValue = config.GetValue<string>("Auth:PrivateKey");
        if (string.IsNullOrEmpty(privateKeyValue)) throw new ApplicationException("JWT:Private key is empty");
        var encodedPrivateKey = Encoding.UTF8.GetBytes(privateKeyValue);

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(encodedPrivateKey),
            SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            SigningCredentials = credentials,
            Expires = DateTime.UtcNow.AddDays(1),
            Subject = GenerateClaims()
        };
        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    public string RefreshToken(Guid playerId)
    {
        var handler = new JwtSecurityTokenHandler();
        var privateKeyValue = config.GetValue<string>("Auth:PrivateKey");
        if (string.IsNullOrEmpty(privateKeyValue)) throw new ApplicationException("JWT:Private key is empty");
        var encodedPrivateKey = Encoding.UTF8.GetBytes(privateKeyValue);

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(encodedPrivateKey),
            SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            SigningCredentials = credentials,
            Expires = DateTime.UtcNow.AddDays(1),
            Subject = GenerateClaims(playerId)
        };
        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }


    private static ClaimsIdentity GenerateClaims(Guid? existingPlayerGuid = null)
    {
        //Generate players id's using guids
        var id = existingPlayerGuid.GetValueOrDefault(Guid.NewGuid());
        var ci = new ClaimsIdentity();
        ci.AddClaim(new Claim(ClaimTypes.NameIdentifier, id.ToString()));
        return ci;
    }
}