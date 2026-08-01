using NyayaDesk.Services;
using System.Web.Mvc;

namespace NyayaDesk.Web.Areas.Admin.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        public ActionResult Index()
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            ViewBag.FullName = Session["FullName"];
            ViewBag.Email = Session["Email"];
            ViewBag.RoleId = Session["RoleId"];
            var subscription = new SubscriptionService().GetForUser((int)Session["UserId"]);
            if (subscription == null || !subscription.HasAccess)
                return RedirectToAction("Index", "Subscription", new { area = "Admin" });

            Session["TenantId"] = subscription.TenantId;
            Session["TenantName"] = subscription.TenantName;
            Session["SubscriptionPlan"] = subscription.PlanName;
            ViewBag.Subscription = subscription;
            return View();
        }
    }
}
