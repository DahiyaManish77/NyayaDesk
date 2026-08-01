using NyayaDesk.Models.ViewModels.Subscription;
using NyayaDesk.Services;
using System;
using System.Web.Mvc;

namespace NyayaDesk.Web.Areas.Admin.Controllers
{
    [Authorize]
    public class SubscriptionController : Controller
    {
        private readonly SubscriptionService _service = new SubscriptionService();
        public ActionResult Index()
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account", new { area = "" });
            int userId = Convert.ToInt32(Session["UserId"]);
            return View(new SubscriptionPageVM { Current = _service.GetForUser(userId), Plans = _service.GetPlans() });
        }
    }
}
