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
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Primitives;
    using Microsoft.Extensions.Logging;

    public class QueueFairCoreService : IQueueFairService
    {
        private ILogger logger;

        private HttpContext context = null;

        private Microsoft.AspNetCore.Http.IQueryCollection query = null;

        private Microsoft.AspNetCore.Http.IRequestCookieCollection cookies { get; set; } = null;

        public QueueFairCoreService(ILogger l, HttpContext context, Microsoft.AspNetCore.Http.IQueryCollection query, Microsoft.AspNetCore.Http.IRequestCookieCollection cookies)
        {
            this.logger = l;
            this.query = query;
            this.context = context;
            this.cookies = cookies;
        }

        public static LogLevel Level { get; set; } = LogLevel.Warning;

        public void Log(string message)
        {
            this.logger.Log(Level, "QF " + message);
        }

        public void Err(Exception exception)
        {
            this.logger.LogError(exception, "QF an error occurred!");
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
            if (this.query == null)
            {
                return null;
            }

            StringValues parameterValues;
            bool success = this.query.TryGetValue(name, out parameterValues);

            if (!success)
            {
                return null;
            }

            string[] resArr = parameterValues.ToArray();
            return resArr[resArr.Length - 1];
        }

        public void SetCookie(string cookieName, string value, int lifetimeSeconds, string cookieDomain, bool isSecure)
        {
            CookieOptions cookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddSeconds(lifetimeSeconds),
                HttpOnly = false,
            };

            if (isSecure)
            {
                cookieOptions.Secure = true;
                cookieOptions.SameSite = SameSiteMode.None;
            }
            else
            {
                cookieOptions.Secure = false;
            }

            if (cookieDomain != null && cookieDomain != string.Empty)
            {
                cookieOptions.Domain = cookieDomain;
            }

            this.context.Response.Cookies.Append(cookieName, value, cookieOptions);
        }

        public void Redirect(string loc)
        {
            this.context.Response.Redirect(loc);
        }

        public string GetCookie(string cname)
        {
            if (cname == null || cname == string.Empty)
            {
                return string.Empty;
            }

            if (this.cookies == null)
            {
                return string.Empty;
            }

            string cookie = this.cookies[cname];

            if (cookie == null)
            {
                return string.Empty;
            }

            return cookie;
        }

        public long UnixTimeSeconds() {
            return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
        }
    }
}
