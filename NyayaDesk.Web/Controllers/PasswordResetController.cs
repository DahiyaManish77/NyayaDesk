using NyayaDesk.Web.Helpers;
using System;
using System.Web.Mvc;

namespace NyayaDesk.Controllers
{
    /// <summary>
    /// Password reset utility controller
    /// IMPORTANT: REMOVE THIS CONTROLLER BEFORE PRODUCTION
    /// This is for development and emergency password resets only
    /// </summary>
    public class PasswordResetController : Controller
    {
        /// <summary>
        /// Display password reset form
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Index()
        {
            // Check if in development mode
            #if DEBUG
                return View();
            #else
                return RedirectToAction("Login", "Account");
            #endif
        }

        /// <summary>
        /// Generate a password hash for testing/reset
        /// 
        /// Example Usage:
        /// POST /PasswordReset/GenerateHash
        /// Body: { "password": "Welcome@123" }
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public ActionResult GenerateHash(string password)
        {
            #if !DEBUG
                return Json(new { error = "Only available in DEBUG mode" });
            #endif

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                return Json(new 
                { 
                    error = "Password must be at least 6 characters" 
                });
            }

            try
            {
                var setup = PasswordHashHelper.CreatePasswordSetup(password);

                return Json(new
                {
                    success = true,
                    password = setup.Password,
                    salt = setup.Salt,
                    hash = setup.Hash,
                    sqlUpdate = setup.GetUpdateSql("admin@nyayadesk.in"),
                    message = "Use the password, salt, and hash values below to update the database"
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Reset password for a specific user by email
        /// 
        /// Example Usage:
        /// POST /PasswordReset/ResetUserPassword
        /// Body: { "email": "admin@nyayadesk.in", "newPassword": "Welcome@123" }
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public ActionResult ResetUserPassword(string email, string newPassword)
        {
            #if !DEBUG
                return Json(new { error = "Only available in DEBUG mode" });
            #endif

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(newPassword))
            {
                return Json(new { error = "Email and password are required" });
            }

            if (newPassword.Length < 6)
            {
                return Json(new { error = "Password must be at least 6 characters" });
            }

            try
            {
                using (var db = new NyayaDeskDBEntities1())
                {
                    var user = db.Users.Find(db.Users.SqlQuery($"SELECT * FROM [User] WHERE Email = '{email}' AND IsDeleted = 0").FirstOrDefault()?.UserId);

                    if (user == null)
                    {
                        return Json(new { error = "User not found" });
                    }

                    var setup = PasswordHashHelper.CreatePasswordSetup(newPassword);

                    user.PasswordHash = setup.Hash;
                    user.PasswordSalt = setup.Salt;
                    user.IsActive = true;
                    user.IsLocked = false;
                    user.FailedLoginAttempts = 0;
                    user.LockoutEndDate = null;

                    db.SaveChanges();

                    return Json(new
                    {
                        success = true,
                        message = $"Password reset successfully for {email}",
                        password = newPassword
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Simple test to verify password hashing
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public ActionResult TestPasswordHash(string password, string salt)
        {
            #if !DEBUG
                return Json(new { error = "Only available in DEBUG mode" });
            #endif

            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(salt))
            {
                return Json(new { error = "Password and salt are required" });
            }

            try
            {
                string hash = PasswordHashHelper.GeneratePasswordHash(password, salt);

                return Json(new
                {
                    success = true,
                    password = password,
                    salt = salt,
                    hash = hash,
                    message = "Hash generated successfully. Use this to verify passwords."
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
    }
}
