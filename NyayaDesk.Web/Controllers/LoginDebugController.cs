using NyayaDesk.Web.Helpers;
using System.Web.Mvc;

namespace NyayaDesk.Controllers
{
    /// <summary>
    /// Debug controller for troubleshooting login issues
    /// IMPORTANT: Remove this controller before going to production
    /// </summary>
    [Authorize(Roles = "ADMIN")]
    public class LoginDebugController : Controller
    {
        /// <summary>
        /// Diagnose login issues - POST request with email and password
        /// Example: POST /LogDebug/DiagnoseLogin with email and password in body
        /// REMOVE IN PRODUCTION - This exposes sensitive information for debugging only
        /// </summary>
        [HttpPost]
        public ActionResult DiagnoseLogin(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return Json(new { error = "Email and password are required" });
            }

            var helper = new LoginDiagnosticsHelper();
            var diagnosis = helper.DiagnoseLogin(email, password);

            return Json(diagnosis);
        }

        /// <summary>
        /// Unlock a user account by UserId
        /// REMOVE IN PRODUCTION - This is for emergency unlock only
        /// </summary>
        [HttpPost]
        public ActionResult UnlockUser(int userId)
        {
            var helper = new LoginDiagnosticsHelper();
            bool success = helper.UnlockUser(userId);

            return Json(new { success = success, message = success ? "User unlocked" : "User not found" });
        }

        /// <summary>
        /// Activate a user account by UserId
        /// REMOVE IN PRODUCTION
        /// </summary>
        [HttpPost]
        public ActionResult ActivateUser(int userId)
        {
            var helper = new LoginDiagnosticsHelper();
            bool success = helper.ActivateUser(userId);

            return Json(new { success = success, message = success ? "User activated" : "User not found" });
        }
    }
}
