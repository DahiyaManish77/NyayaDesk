using NyayaDesk.Models;
using NyayaDesk.Models.ViewModels;
using NyayaDesk.Web.Models.Entities;

using System;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Security.Cryptography;

namespace NyayaDesk.Services
{
    public interface IAccountService
    {
        LoginResult ValidateLogin(LoginViewModel model);

        User GetUserById(int userId);

        User GetUserByEmail(string email);

        void UpdateLastLogin(int userId);

        void LogLoginAttempt(
            int userId,
            string ipAddress,
            string userAgent,
            bool isSuccess);

        bool IsUserLocked(User user);

        void IncrementFailedAttempts(User user);

        void ResetFailedAttempts(User user);

        string GenerateSalt();

        string HashPassword(
            string password,
            string salt);

        bool VerifyPassword(
            string enteredPassword,
            string storedHash,
            string storedSalt);
    }

    public class AccountService : IAccountService
    {
        private readonly NyayaDeskDBEntities1 _dbContext;

        public AccountService()
        {
            _dbContext = new NyayaDeskDBEntities1();
        }

        public LoginResult ValidateLogin(LoginViewModel model)
        {
            LoginResult result = new LoginResult();

            result.IsSuccess = false;

            result.ErrorMessage = string.Empty;

            if (model == null)
            {
                result.ErrorMessage = "Invalid request.";

                return result;
            }

            string email = model.Email == null
                ? string.Empty
                : model.Email.Trim();

            string password = model.Password == null
                ? string.Empty
                : model.Password.Trim();

            User user = GetUserByEmail(email);

            if (user == null)
            {
                result.ErrorMessage =
                    "Invalid email or password.";

                return result;
            }

            if (!user.IsActive)
            {
                result.ErrorMessage =
                    "Your account is inactive.";

                return result;
            }

            if (user.IsDeleted)
            {
                result.ErrorMessage =
                    "Your account does not exist.";

                return result;
            }

            if (IsUserLocked(user))
            {
                result.ErrorMessage =
                    "Your account is locked.";

                return result;
            }

            if (!VerifyPassword(
                    password,
                    user.PasswordHash,
                    user.PasswordSalt))
            {
                IncrementFailedAttempts(user);

                result.ErrorMessage =
                    "Invalid email or password.";

                return result;
            }

            ResetFailedAttempts(user);

            UpdateLastLogin(user.UserId);

            SetAuthentication(
                user,
                model.RememberMe);

            LoadSession(user);

            result.IsSuccess = true;

            result.UserId = user.UserId;

            result.User = user;

            return result;
        }
        public User GetUserByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            email = email.Trim().ToLower();

            return _dbContext.Users
                .FirstOrDefault(x =>
                    x.Email != null &&
                    x.Email.ToLower() == email &&
                    !x.IsDeleted);
        }

        public User GetUserById(int userId)
        {
            return _dbContext.Users
                .FirstOrDefault(x =>
                    x.UserId == userId &&
                    !x.IsDeleted);
        }

        public bool IsUserLocked(User user)
        {
            if (user == null)
            {
                return false;
            }

            if (!user.IsLocked)
            {
                return false;
            }

            if (!user.LockoutEndDate.HasValue)
            {
                return true;
            }

            if (user.LockoutEndDate.Value <= DateTime.Now)
            {
                user.IsLocked = false;
                user.FailedLoginAttempts = 0;
                user.LockoutEndDate = null;

                _dbContext.SaveChanges();

                return false;
            }

            return true;
        }

        public void IncrementFailedAttempts(User user)
        {
            if (user == null)
            {
                return;
            }

            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= 5)
            {
                user.IsLocked = true;
                user.LockoutEndDate = DateTime.Now.AddMinutes(15);
            }

            _dbContext.SaveChanges();
        }

        public void ResetFailedAttempts(User user)
        {
            if (user == null)
            {
                return;
            }

            user.FailedLoginAttempts = 0;

            user.IsLocked = false;

            user.LockoutEndDate = null;

            _dbContext.SaveChanges();
        }

        public void UpdateLastLogin(int userId)
        {
            User user = GetUserById(userId);

            if (user == null)
            {
                return;
            }

            user.LastLoginDate = DateTime.Now;

            user.LastLoginOn = DateTime.Now;

            _dbContext.SaveChanges();
        }

        public void LogLoginAttempt(
            int userId,
            string ipAddress,
            string userAgent,
            bool isSuccess)
        {
            // Future:
            // Insert into LoginHistory / AuditLog table.
        }
        public string GenerateSalt()
        {
            return Guid.NewGuid().ToString().ToUpper();
        }

        public string HashPassword(
            string password,
            string salt)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(salt))
            {
                return string.Empty;
            }

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes =
                    Encoding.UTF8.GetBytes(password + salt);

                byte[] hash =
                    sha256.ComputeHash(bytes);

                StringBuilder builder =
                    new StringBuilder();

                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(
                        hash[i].ToString("X2"));
                }

                return builder.ToString();
            }
        }

        public bool VerifyPassword(
            string enteredPassword,
            string storedHash,
            string storedSalt)
        {
            if (string.IsNullOrWhiteSpace(enteredPassword))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(storedHash))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(storedSalt))
            {
                return false;
            }

            string computedHash =
                HashPassword(
                    enteredPassword.Trim(),
                    storedSalt.Trim());

            return string.Equals(
                computedHash,
                storedHash.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        private void SetAuthentication(
            User user,
            bool rememberMe)
        {
            string[] roles =
                _dbContext.UserRoles
                    .Where(x =>
                        x.UserId == user.UserId &&
                        x.IsActive)
                    .Select(x => x.Role.RoleCode)
                    .ToArray();

            FormsAuthenticationTicket ticket =
                new FormsAuthenticationTicket(
                    1,
                    user.Email,
                    DateTime.Now,
                    DateTime.Now.AddDays(
                        rememberMe ? 30 : 1),
                    rememberMe,
                    string.Join(",", roles));

            string encryptedTicket =
                FormsAuthentication.Encrypt(ticket);

            HttpCookie cookie =
                new HttpCookie(
                    FormsAuthentication.FormsCookieName,
                    encryptedTicket);

            cookie.HttpOnly = true;
            cookie.Path = "/";

            if (rememberMe)
            {
                cookie.Expires =
                    DateTime.Now.AddDays(30);
            }

            HttpContext.Current.Response.Cookies.Add(cookie);
        }

        private void LoadSession(User user)
        {
            HttpContext.Current.Session["UserId"] =
                user.UserId;

            HttpContext.Current.Session["UserName"] =
                user.UserName;

            HttpContext.Current.Session["FullName"] =
                user.DisplayName;

            HttpContext.Current.Session["Email"] =
                user.Email;

            HttpContext.Current.Session["IsSystemUser"] =
                user.IsSystemUser;

            HttpContext.Current.Session["IsFirstLogin"] =
                user.IsFirstLogin;

            HttpContext.Current.Session["ProfilePhoto"] =
                user.ProfilePhotoPath;

            var primaryRole =
                _dbContext.UserRoles
                    .Where(x =>
                        x.UserId == user.UserId &&
                        x.IsPrimaryRole &&
                        x.IsActive)
                    .Select(x => new
                    {
                        x.RoleId,
                        x.Role.RoleName,
                        x.Role.RoleCode
                    })
                    .FirstOrDefault();

            if (primaryRole != null)
            {
                HttpContext.Current.Session["RoleId"] =
                    primaryRole.RoleId;

                HttpContext.Current.Session["RoleName"] =
                    primaryRole.RoleName;

                HttpContext.Current.Session["RoleCode"] =
                    primaryRole.RoleCode;
            }
        }
    }

    public class LoginResult
    {
        public bool IsSuccess
        {
            get;
            set;
        }

        public string ErrorMessage
        {
            get;
            set;
        }

        public int UserId
        {
            get;
            set;
        }

        public User User
        {
            get;
            set;
        }
    }
}
