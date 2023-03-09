using QueueFair.Adapter;
using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace DotNetFramework
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        private bool goQueueFair(HttpContextBase context)
        {
            QueueFairConfig.AccountSecret = "REPLACE_WITH_YOUR_ACCOUNT_SECRET";
            QueueFairConfig.Account = "REPLACE_WITH_YOUR_ACCOUNT_SYSTEM_NAME";

            //Always set this to false in production environments.  When set to true,
            //Debug output is written to System.Diagnostics.Debug console output.
            QueueFairConfig.Debug = true;

            QueueFairFrameworkService service = new QueueFairFrameworkService(context);
            QueueFairAdapter queueFairAdapter = new QueueFairAdapter(service);

            //Must be the full page URL that the visitor sees in the browser.
            queueFairAdapter.RequestedURL = context.Request.Url.OriginalString;

            // If your server is behind a load balancer, you may need to use a custom header to
            // determine this.
            queueFairAdapter.IsSecure = context.Request.IsSecureConnection;

            // Must be the visitor's IP address.  If your server is behind a load balancer,
            // use the X-Forwarded-For header value instead.
            queueFairAdapter.RemoteIP = Convert.ToString(context.Request.UserHostAddress);

            queueFairAdapter.UserAgent = context.Request.Headers["User-Agent"];

            return queueFairAdapter.IsContinue();
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            HttpContextBase context = new HttpContextWrapper(((System.Web.HttpApplication)sender).Context);
            if(!goQueueFair(context))
            {
                // Terminate processing of the request.
                context.Response.Flush();
                context.Response.SuppressContent = true;
                context.ApplicationInstance.CompleteRequest();
            }
        }
        
    }
}
