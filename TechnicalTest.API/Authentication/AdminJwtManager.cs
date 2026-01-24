using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TechnicalTest.API.Authentication;

public class AdminJwtManager(IConfiguration configuration)
{
    private readonly string _issuer =  configuration["Jwt:Issuer"] ??  throw new InvalidOperationException("Jwt:Issuer not set");
    private readonly string _audience =  configuration["Jwt:Audience"] ??  throw new InvalidOperationException("Jwt:Audience not set");
    
    // ASSUMPTION: This is quick enough for a proof of concept.
    // Reality should use asymmetric keys, and should be pulled from KeyVault etc - somewhere more secure than an exposed key in a config file in source control!
    private readonly string _key =   configuration["Jwt:Key"] ??  throw new InvalidOperationException("Jwt:Key not set");
    
    // Used by the admin endpoints, for tech test demonstration purposes only.
    // ASSUMPTION: A separate sign-in service would exist, which would handle authentication & provide the mobile app a JWT
    public string GenerateJwt(Claim[] claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Note: 30 minute timeout, since this is just a tech test convenience. A production system would need a specific timeout identified (the shorter the better), refresh token support etc.
        var token = new JwtSecurityToken(_issuer, _audience, claims, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(30), creds);
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}