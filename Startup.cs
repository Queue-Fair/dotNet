//-----------------------------------------------------------------------
// <copyright file="Startup.cs" company="Matt King for Orderly Telecoms">
// Copyright Matt King. All rights Reserved
// </copyright>
//-----------------------------------------------------------------------
namespace QueueFairDemo
{
    using System;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.EventSource;

    // Queue-Fair
    using QueueFair.Adapter;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.Use(async (context, next) =>
            {

	    	// This if statement prevents .png, .js requests etc
		// from running the Adapter.  You only want to run the
		// Adapter on Page requests.
                if (context.Request.Path.ToString().IndexOf(".") == -1)
                {
                    ILogger l =
                    context.RequestServices.GetService<ILogger<EventSourceLoggerProvider>>();

                    QueueFairCoreService service = new QueueFairCoreService(l,context,context.Request.Query, context.Request.Cookies);

                    //The following values mut be replaced with the values shown on the Account -> Your Account page in the Portal.			
                    QueueFairConfig.AccountSecret = "REPLACE_WITH_YOUR_ACCOUNT_SECRET";
                    QueueFairConfig.Account = "REPLACE_WITH_YOUR_ACCOUNT_SYSTEM_NAME";
			
		    // Uncomment below to enable debug logging.
                    // QueueFairConfig.Debug = true;

		    // Uncomment below to download a fresh copy
		    // of your settings with every page requst.
		    // Set it to at least 5 for production.
		    // QueueFairConfig.SettingsCacheLifetimeMinutes = 0

		    QueueFairAdapter queueFairAdapter = new QueueFairAdapter(service);

                    queueFairAdapter.RequestedURL = context.Request.Scheme + "://" + context.Request.Host + context.Request.PathBase + context.Request.Path + context.Request.QueryString;
		    queueFairAdapter.UserAgent = context.Request.Headers["User-Agent"];

	            //IMPORTANT: If your server is behind a firewall/load balancer, you may need to edit these lines to reflect whether the end visitor connection is secure
		    //and to give the end visitor's IP address, not the firewall/load balancer IP address.  The visitor's IP address is usually in an X-Forwarded-For header.
                    queueFairAdapter.IsSecure = context.Request.IsHttps;
                    queueFairAdapter.RemoteIP = Convert.ToString(context.Connection.RemoteIpAddress);
                    

                    /* If you JUST want to validate a PassedCookie, use this on a path that does NOT match any queue's Activation Rules: */
                    /*
                    if (queueFairAdapter.RequestedURL.IndexOf("/path/to/page") != -1)
                    {
                        try
                        {
                            int passedLifetimeMinutes = 60; //Passed cookie valid for one hour.
                            if (!queueFairAdapter.ValidateCookie("QUEUE SECRET FROM PORTAL", passedLifetimeMinutes, service.GetCookie("QueueFair-Pass-QUEUE_SYSTEM_NAME_FROM_PORTAL")))
                            {
                                queueFairAdapter.Redirect("https://YOUR_ACCOUNT_SYSTEM_NAME.queue-fair.net/YOUR_QUEUE_SYSTEM_NAME?qfError=InvalidCookie", 0);
                            }
                            else
                            {
                                await next.Invoke();
                            }
                        }
                        catch (Exception exception)
                        {
                            queueFairAdapter.Err(exception);
                            await next.Invoke();
                        }
                    }
                    else
                    {
                        await next.Invoke();
                    }
                    */

                    /* To run the full Adapter, use this instead */
                    if (queueFairAdapter.IsContinue())
                    {
                        await next.Invoke();
                    }

                }
                else
                {
                    await next.Invoke();
                }
            });
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}

