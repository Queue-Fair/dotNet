//-----------------------------------------------------------------------
// <copyright file="QueueFairLogger.cs" company="Matt King for Orderly Telecoms">
// Copyright Matt King. All rights Reserved
// </copyright>
//-----------------------------------------------------------------------

namespace QueueFair.Adapter
{
    using System;
    using System.Text;
    using System.Web;
    using System.Diagnostics;

    public class QueueFairFrameworkService : IQueueFairService
    {

        private HttpContextBase context = null;

        public QueueFairFrameworkService(HttpContextBase context)
        {
            this.context = context;
        }

        public void Log(string message)
        {
            Debug.WriteLine("QF " + message);
        }

        public void Err(Exception exception)
        {
           Debug.WriteLine("QF ERROR " + exception.ToString() + " " + exception.Message);
        }

        public void AddHeader(string name, string value)
        {
             this.context.Response.Headers.Add("Cache-Control", "no-store,max-age=0");
        }

        public string Encode(string value)
        {
            return HttpUtility.UrlEncode(value, Encoding.UTF8);
        }

        public string Decode(string value)
        {
            return HttpUtility.UrlDecode(Convert.ToString(value), Encoding.UTF8);
        }

        public string GetQueryParameter(string name) {
            return context.Request.QueryString[name];
        }

        public void SetCookie(string cookieName, string value, int lifetimeSeconds, string cookieDomain, bool isSecure)
        {
            HttpCookie cookie = new HttpCookie(cookieName);
            cookie.Value = value;
            DateTime now = DateTime.Now;
            cookie.HttpOnly = false;
            cookie.Expires = now.AddSeconds(lifetimeSeconds);


            if (isSecure)
            {
                cookie.Secure = true;
                cookie.SameSite = SameSiteMode.None;
            }
            else
            {
                cookie.Secure = false;
            }

            if (cookieDomain != null && cookieDomain != string.Empty)
            {
                cookie.Domain = cookieDomain;
            }

            this.context.Response.Cookies.Add(cookie);
        }

        public void Redirect(string loc)
        {
            this.context.Response.Redirect(loc, false);
        }

        public string GetCookie(string cname)
        {
            if (cname == null || cname == string.Empty)
            {
                return string.Empty;
            }

            if (context.Request.Cookies[cname] == null)
            {
                return string.Empty;
            }
            return context.Request.Cookies[cname].Value;
        }

        public long UnixTimeSeconds() {
            return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
        }
    }
}
