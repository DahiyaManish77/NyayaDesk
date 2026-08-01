using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using NyayaDesk.Models.ViewModels;
using NyayaDesk.Services;

namespace NyayaDesk.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;

        public AccountController()
        {
            _accountService = new AccountService();
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            if (Request.IsAuthenticated)
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard",
                    new { area = "Admin" });
            }

            ViewBag.ReturnUrl = returnUrl;

            if (TempData["Error"] != null)
            {
                ViewBag.Error = TempData["Error"].ToString();
            }

            return View(new LoginViewModel());
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Register(string plan = "PRO")
        {
            if (Request.IsAuthenticated) return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            return View(new WorkspaceRegistrationVM { PlanCode = String.IsNullOrWhiteSpace(plan) ? "PRO" : plan.ToUpperInvariant(), ProfessionalRole = "ADVOCATE" });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(WorkspaceRegistrationVM model)
        {
            string[] plans = { "SOLO", "PRO", "FIRM" };
            if (Array.IndexOf(plans, (model.PlanCode ?? "").ToUpperInvariant()) < 0) ModelState.AddModelError("PlanCode", "Select a valid subscription plan.");
            if (!ModelState.IsValid) return View(model);
            try
            {
                model.PlanCode = model.PlanCode.ToUpperInvariant();
                new WorkspaceOnboardingService().Register(model);
                TempData["Success"] = "Your workspace and free trial are ready. Sign in to continue.";
                return RedirectToAction("Login");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Workspace registration could not be completed. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                string ipAddress = GetClientIP();

                string userAgent = Request.UserAgent;

                LoginResult result =
                    _accountService.ValidateLogin(model);
                _accountService.LogLoginAttempt(
                    result.IsSuccess ? result.UserId : 0,
                    ipAddress,
                    userAgent,
                    result.IsSuccess);

                if (!result.IsSuccess)
                {
                    ModelState.AddModelError("", result.ErrorMessage);

                    return View(model);
                }

                if (!string.IsNullOrWhiteSpace(returnUrl) &&
                    Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction(
                    "Index",
                    "Dashboard",
                    new { area = "Admin" });
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Login could not be completed. Please try again or contact the administrator.");

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();

            Session.Clear();

            Session.Abandon();

            HttpCookie cookie =
                new HttpCookie(
                    FormsAuthentication.FormsCookieName,
                    string.Empty);

            cookie.Expires = DateTime.Now.AddYears(-1);

            Response.Cookies.Add(cookie);

            return RedirectToAction(
                "Login",
                "Account");
        }

        [HttpGet]
        public JsonResult CheckSession()
        {
            return Json(
                new
                {
                    IsAuthenticated = Request.IsAuthenticated,
                    UserId = Session["UserId"],
                    UserName = Session["UserName"],
                    RoleId = Session["RoleId"],
                    RoleName = Session["RoleName"]
                },
                JsonRequestBehavior.AllowGet);
        }

        private string GetClientIP()
        {
            string ip =
                Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (string.IsNullOrWhiteSpace(ip))
            {
                ip =
                    Request.ServerVariables["REMOTE_ADDR"];
            }

            if (string.IsNullOrWhiteSpace(ip))
            {
                ip =
                    Request.UserHostAddress;
            }

            return ip;
        }
    }
}
