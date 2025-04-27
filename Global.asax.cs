using System;
using System.Web;
using System.Web.Routing;

namespace ClaimKitv1
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            // Register routes
            RegisterRoutes(RouteTable.Routes);
        }

        private void RegisterRoutes(RouteCollection routes)
        {
            routes.MapPageRoute(
                "DefaultRoute",
                "",
                "~/Default.aspx"
            );

            // Catch-all route to redirect everything to Default.aspx
            routes.MapPageRoute(
                "CatchAllRoute",
                "{*pathInfo}",
                "~/Default.aspx"
            );
        }
    }
}