using NyayaDesk.Web;
using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace NyayaDesk.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // Register authentication
            AuthConfig.RegisterAuth();
        }

        protected void Application_PostAuthenticateRequest()
        {
            // Custom authentication handling if needed
        }

        protected void Session_End(object sender, EventArgs e)
        {
            // Clean up session on timeout
        }
    }
}