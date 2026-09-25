using M.Contract.Repositories.Entities;
using M.Contract.Repositories.Entity;
using M.Contract.Services.Interface;
using M.Core.Base;
using M.Repositories.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ModelViews.AuthModelView;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using static M.Core.Base.BaseException;

namespace M.Services.Service
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly DatabaseContext _dbContext;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            DatabaseContext dbContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _dbContext = dbContext;
        }

        #region Register

        public async Task RegisterAsync(RegisterModelView model)
        {
            model.Email = model.Email.Trim();
            model.Username = model.Username.Trim();

            if (string.IsNullOrEmpty(model.Username))
                throw new BadRequestException(
                    "INVALID_USERNAME",
                    "Username cannot be empty");

            if (string.IsNullOrEmpty(model.Email) ||
                !MailAddress.TryCreate(model.Email, out _))
                throw new BadRequestException(
                    "INVALID_EMAIL",
                    "Email is not valid");

            if (string.IsNullOrEmpty(model.Password) ||
                model.Password != model.ConfirmPassword)
                throw new BadRequestException(
                    "INVALID_PASSWORD",
                    "Password cannot be empty or does not match");

            // Kiểm tra username
            if (await _userManager.FindByNameAsync(model.Username) != null)
                throw new BadRequestException(
                    "DUPLICATE_USERNAME",
                    "Username already exists");

            // Kiểm tra email
            if (await _userManager.FindByEmailAsync(model.Email) != null)
                throw new BadRequestException(
                    "DUPLICATE_EMAIL",
                    "Email already exists");

            ApplicationUser user = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email,
                EmailConfirmed = true
            };

            IdentityResult createResult =
                await _userManager.CreateAsync(user, model.Password);

            if (!createResult.Succeeded)
            {
                throw new BadRequestException(
                    "INVALID_INPUT",
                    createResult.Errors.FirstOrDefault()?.Description
                    ?? "Unknown error occurred");
            }

            await _userManager.AddToRoleAsync(user, "User");

            // =====================================================
            // Tạo Employee thay cho UserInfo
            // =====================================================

            Employee employee = new Employee
            {
                UserId = user.Id,

                // Tạm thời sinh mã nhân viên tự động
                EmployeeCode = $"EMP{DateTime.UtcNow:yyyyMMddHHmmss}",

                GivenName = model.Username,
                FamilyName = string.Empty,

                Email = model.Email,

                BirthDate = null,
                Gender = GenderType.RatherNotSay,

                Status = EmployeeStatus.Probation,
                LaborType = LaborType.FullTime,

                StartDate = DateTime.UtcNow
            };

            await _dbContext.Set<Employee>().AddAsync(employee);
            await _dbContext.SaveChangesAsync();
        }

        #endregion

        #region Login

        public async Task<AuthResponseModelView> LoginAsync(
            LoginModelView loginModelView)
        {
            string login = loginModelView.Username?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(login))
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ResponseCodeConstants.BAD_REQUEST,
                    "Username, email or phone number is required.");
            }

            if (string.IsNullOrWhiteSpace(loginModelView.Password))
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ResponseCodeConstants.BAD_REQUEST,
                    "Password is required.");
            }

            // =====================================================
            // Tìm user bằng Username / Email / PhoneNumber
            // Không yêu cầu EmailConfirmed / PhoneNumberConfirmed
            // =====================================================

            ApplicationUser? user = await _userManager.Users
                .FirstOrDefaultAsync(x =>
                    x.UserName == login ||
                    x.Email == login ||
                    x.PhoneNumber == login);

            if (user == null)
            {
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ResponseCodeConstants.NOT_FOUND,
                    "Account not found.");
            }

            if (user.DeletedTime.HasValue)
            {
                throw new ErrorException(
                    StatusCodes.Status403Forbidden,
                    ResponseCodeConstants.FORBIDDEN,
                    "User account has been deactivated.");
            }

            // =====================================================
            // Kiểm tra password
            // =====================================================

            SignInResult result =
                await _signInManager.PasswordSignInAsync(
                    user,
                    loginModelView.Password,
                    loginModelView.RememberMe,
                    lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                throw new ErrorException(
                    StatusCodes.Status401Unauthorized,
                    ResponseCodeConstants.UNAUTHORIZED,
                    "Username, email/phone number or password is incorrect.");
            }

            // =====================================================
            // Lấy Employee
            // =====================================================

            Employee? employee = await _dbContext.Set<Employee>()
                .FirstOrDefaultAsync(x =>
                    x.UserId == user.Id &&
                    !x.DeletedTime.HasValue);

            if (employee == null)
            {
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ResponseCodeConstants.NOT_FOUND,
                    "Employee information is not found.");
            }

            // =====================================================
            // Generate JWT
            // =====================================================

            int expireMinutes = int.Parse(
                _configuration["Jwtsettings:ExpirationMinutes"]!);

            DateTime expires =
                DateTime.UtcNow.AddMinutes(expireMinutes);

            string token = GenerateJwtToken(
                await GenerateClaims(user, employee),
                expires);

            return new AuthResponseModelView
            {
                Token = token,
                ExpiredAt = expires,
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,

                GivenName = employee.GivenName,
                FamilyName = employee.FamilyName
            };
        }

        #endregion

        #region Private

        private async Task<List<Claim>> GenerateClaims(
            ApplicationUser user,
            Employee employee)
        {
            string fullName =
                $"{employee.GivenName} {employee.FamilyName}".Trim();

            List<Claim> claims = new()
            {
                new(
                    JwtRegisteredClaimNames.Sub,
                    user.Id.ToString()),

                new(
                    JwtRegisteredClaimNames.Email,
                    user.Email ?? string.Empty),

                new(
                    ClaimTypes.Name,
                    user.UserName ?? string.Empty),

                new(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString()),

                new(
                    "employeeId",
                    employee.Id.ToString()),

                new(
                    "employeeCode",
                    employee.EmployeeCode),

                new(
                    "fullName",
                    fullName),

                new(
                    "givenName",
                    employee.GivenName),

                new(
                    "familyName",
                    employee.FamilyName)
            };

            foreach (string role in await _userManager.GetRolesAsync(user))
            {
                claims.Add(
                    new Claim(ClaimTypes.Role, role));
            }

            return claims;
        }

        private string GenerateJwtToken(
            IEnumerable<Claim> claims,
            DateTime expires)
        {
            SymmetricSecurityKey key =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        _configuration["Jwtsettings:Key"]!));

            SigningCredentials creds =
                new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token =
                new JwtSecurityToken(
                    issuer: _configuration["Jwtsettings:Issuer"],
                    audience: _configuration["Jwtsettings:Audience"],
                    claims: claims,
                    expires: expires,
                    signingCredentials: creds);

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }

        #endregion
    }
}