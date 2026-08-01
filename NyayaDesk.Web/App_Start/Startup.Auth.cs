using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace NyayaDesk
{
    public static class AuthConfig
    {
        public static void RegisterAuth()
        {
            // Forms Authentication is configured in Web.config
            // No additional setup required here

            // You can optionally set global authorization filters
            // GlobalFilters.Filters.Add(new AuthorizeAttribute());

            // Set default roles provider if needed
            // Roles.Enabled = true;
        }
    }
}