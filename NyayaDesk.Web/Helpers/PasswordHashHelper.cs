using System;
using System.Security.Cryptography;
using System.Text;

namespace NyayaDesk.Web.Helpers
{
    /// <summary>
    /// Password helper for generating hashes and testing login
    /// USE THIS TOOL TO RESET OR CREATE TEST USER PASSWORDS
    /// </summary>
    public class PasswordHashHelper
    {
        /// <summary>
        /// Generate a password hash for a given password and salt
        /// Use this to create test passwords for user accounts
        /// </summary>
        public static string GeneratePasswordHash(string password, string salt)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(salt))
            {
                throw new ArgumentException("Password and salt cannot be empty");
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
        /// Generate a new password salt (GUID format)
        /// </summary>
        public static string GeneratePasswordSalt()
        {
            return Guid.NewGuid().ToString().ToUpper();
        }

        /// <summary>
        /// Create a complete password setup (salt + hash) for a new password
        /// </summary>
        public static PasswordSetup CreatePasswordSetup(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                throw new ArgumentException("Password must be at least 6 characters");
            }

            string salt = GeneratePasswordSalt();
            string hash = GeneratePasswordHash(password, salt);

            return new PasswordSetup
            {
                Salt = salt,
                Hash = hash,
                Password = password
            };
        }
    }

    /// <summary>
    /// Result of password setup generation
    /// </summary>
    public class PasswordSetup
    {
        public string Password { get; set; }
        public string Salt { get; set; }
        public string Hash { get; set; }

        public override string ToString()
        {
            return $"Password: {Password}\nSalt: {Salt}\nHash: {Hash}";
        }

        /// <summary>
        /// Get SQL UPDATE statement to update user password in database
        /// </summary>
        public string GetUpdateSql(string email)
        {
            return $@"UPDATE [User] 
SET PasswordHash = '{Hash}', 
    PasswordSalt = '{Salt}',
    IsActive = 1,
    IsLocked = 0,
    FailedLoginAttempts = 0,
    LockoutEndDate = NULL
WHERE Email = '{email}'";
        }
    }
}
