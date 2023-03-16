using System;
using System.Web;
using System.Web.Mvc;
using QueueFair.Adapter;

namespace DotNetFramework.Controllers
{
    public class HomeController : Controller
    {
        
        private bool goQueueFair()
        {
            //The following values mut be replaced with the values shown on the Account -> Your Account page in the Portal.
            QueueFairConfig.AccountSecret = "REPLACE_WITH_YOUR_ACCOUNT_SECRET";
            QueueFairConfig.Account = "REPLACE_WITH_YOUR_ACCOUNT_SYSTEM_NAME";

            //Always set this to false in production environments.  When set to true,
            //Debug output is written to System.Diagnostics.Debug console output.
            QueueFairConfig.Debug = true;

            HttpContextBase context = HttpContext;
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

        public ActionResult Index()
        {
            
            if(!goQueueFair())
            {
                return null;
            }
            return View();
        }

        public ActionResult About()
        {
            if (!goQueueFair())
            {
                return null;
            }
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            if (!goQueueFair())
            {
                return null;
            }

            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}
