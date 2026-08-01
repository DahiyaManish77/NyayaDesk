using NyayaDesk.Models.ViewModels;
using NyayaDesk.Web.Models.Entities;
using System;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace NyayaDesk.Web.Helpers
{
    /// <summary>
    /// Helper class for diagnosing login issues
    /// </summary>
    public class LoginDiagnosticsHelper
    {
        private readonly NyayaDeskDBEntities1 _dbContext;

        public LoginDiagnosticsHelper()
        {
            _dbContext = new NyayaDeskDBEntities1();
        }

        /// <summary>
        /// Diagnose why a login is failing
        /// </summary>
        public LoginDiagnosisResult DiagnoseLogin(string email, string password)
        {
            var result = new LoginDiagnosisResult();

            if (string.IsNullOrWhiteSpace(email))
            {
                result.Issues.Add("Email is empty");
                return result;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                result.Issues.Add("Password is empty");
                return result;
            }

            var user = _dbContext.Users
                .FirstOrDefault(x =>
                    x.Email != null &&
                    x.Email.ToLower() == email.Trim().ToLower() &&
                    !x.IsDeleted);

            if (user == null)
            {
                result.Issues.Add($"No user found with email: {email}");
                result.AvailableEmails = _dbContext.Users
                    .Where(x => !x.IsDeleted)
                    .Select(x => x.Email)
                    .ToList();
                return result;
            }

            result.UserFound = true;
            result.UserId = user.UserId;
            result.UserEmail = user.Email;
            result.UserName = user.UserName;

            // Check account status
            if (!user.IsActive)
            {
                result.Issues.Add("Account is INACTIVE");
            }

            if (user.IsDeleted)
            {
                result.Issues.Add("Account is DELETED");
            }

            if (user.IsLocked)
            {
                if (user.LockoutEndDate.HasValue && user.LockoutEndDate.Value > DateTime.Now)
                {
                    result.Issues.Add($"Account is LOCKED until {user.LockoutEndDate:yyyy-MM-dd HH:mm:ss}");
                }
                else
                {
                    result.Issues.Add("Account is marked as locked but lockout period has expired");
                }
            }

            result.FailedLoginAttempts = user.FailedLoginAttempts;

            // Verify password
            if (VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
            {
                result.PasswordIsCorrect = true;
                result.Status = "All checks passed - Login should work";
            }
            else
            {
                result.Issues.Add("PASSWORD IS INCORRECT");
                result.PasswordIsCorrect = false;

                // Debug info
                result.DebugInfo = new PasswordDebugInfo
                {
                    EnteredPassword = password,
                    StoredSalt = user.PasswordSalt,
                    StoredHash = user.PasswordHash,
                    ComputedHash = HashPassword(password, user.PasswordSalt)
                };
            }

            return result;
        }

        private bool VerifyPassword(string enteredPassword, string storedHash, string storedSalt)
        {
            if (string.IsNullOrWhiteSpace(enteredPassword) ||
                string.IsNullOrWhiteSpace(storedHash) ||
                string.IsNullOrWhiteSpace(storedSalt))
            {
                return false;
            }

            string computedHash = HashPassword(enteredPassword.Trim(), storedSalt.Trim());

            return string.Equals(
                computedHash,
                storedHash.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        private string HashPassword(string password, string salt)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(salt))
            {
                return string.Empty;
            }

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password + salt);
                byte[] hash = sha256.ComputeHash(bytes);
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("X2"));
                }

                return builder.ToString();
            }
        }

        /// <summary>
        /// Unlock a user account
        /// </summary>
        public bool UnlockUser(int userId)
        {
            var user = _dbContext.Users.FirstOrDefault(x => x.UserId == userId);

            if (user == null)
                return false;

            user.IsLocked = false;
            user.FailedLoginAttempts = 0;
            user.LockoutEndDate = null;

            _dbContext.SaveChanges();
            return true;
        }

        /// <summary>
        /// Activate a user account
        /// </summary>
        public bool ActivateUser(int userId)
        {
            var user = _dbContext.Users.FirstOrDefault(x => x.UserId == userId);

            if (user == null)
                return false;

            user.IsActive = true;
            _dbContext.SaveChanges();
            return true;
        }
    }

    public class LoginDiagnosisResult
    {
        public LoginDiagnosisResult()
        {
            Issues = new System.Collections.Generic.List<string>();
            AvailableEmails = new System.Collections.Generic.List<string>();
        }

        public bool UserFound { get; set; }
        public int UserId { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public bool PasswordIsCorrect { get; set; }
        public int FailedLoginAttempts { get; set; }
        public string Status { get; set; }
        public System.Collections.Generic.List<string> Issues { get; set; }
        public System.Collections.Generic.List<string> AvailableEmails { get; set; }
        public PasswordDebugInfo DebugInfo { get; set; }
    }

    public class PasswordDebugInfo
    {
        public string EnteredPassword { get; set; }
        public string StoredSalt { get; set; }
        public string StoredHash { get; set; }
        public string ComputedHash { get; set; }
    }
}
