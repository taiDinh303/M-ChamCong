using M.Core.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using M.Contract.Repositories.AuthEntities;
using M.Contract.Repositories.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace M.Services.Security
{
    /// <summary>
    /// Sinh JWT (claims + token) cho luồng đăng nhập.
    /// Tách khỏi AuthService để tập trung & dễ test.
    /// </summary>
    public class JwtGenerator
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JwtSettings _settings;
        private readonly TimeProvider _clock;

        public JwtGenerator(
            UserManager<ApplicationUser> userManager,
            IOptions<JwtSettings> settings, TimeProvider clock)
        {
            _userManager = userManager;
            _settings = settings.Value;
            _clock = clock;
        }

        public async Task<string> GenerateAsync(
            ApplicationUser user,
            Employee? employee)
        {
            var expireMinutes = _settings.ExpirationMinutes;
            DateTime expires = _clock.GetUtcNow().UtcDateTime.AddMinutes(expireMinutes);

            List<Claim> claims =
                await BuildClaimsAsync(user, employee);
            return GenerateJwtToken(claims, expires);
        }

        private async Task<List<Claim>> BuildClaimsAsync(
            ApplicationUser user,
            Employee? employee)
        {
            List<Claim> claims = new()
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            claims.Add(new Claim("securityStamp", user.SecurityStamp ?? string.Empty));

            if (employee != null)
            {
                string fullName =
                    $"{employee.GivenName} {employee.FamilyName}".Trim();
                claims.Add(new Claim("employeeId", employee.Id.ToString()));
                claims.Add(new Claim("employeeCode", employee.EmployeeCode));
                claims.Add(new Claim("fullName", fullName));
                claims.Add(new Claim("givenName", employee.GivenName));
                claims.Add(new Claim("familyName", employee.FamilyName));
                claims.Add(new Claim("gender", employee.Gender.ToString()));
            }

            // Role claims -> [Authorize(Roles = "...")]
            foreach (string role in await _userManager.GetRolesAsync(user))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            return claims;
        }

        private string GenerateJwtToken(IEnumerable<Claim> claims, DateTime expires)
        {
            SymmetricSecurityKey key =
                new(Encoding.UTF8.GetBytes(_settings.Key));
            SigningCredentials creds =
                new(key, SecurityAlgorithms.HmacSha256);
            JwtSecurityToken token =
                new(
                    issuer: _settings.Issuer,
                    audience: _settings.Audience,
                    claims: claims,
                    expires: expires,
                    signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
