---
## Queue-Fair ASP / .NET / C# Adapter README & Installation Guide

Queue-Fair can be added to any web server easily in minutes.  You will need a Queue-Fair account - please visit https://queue-fair.com/free-trial if you don't already have one.  You should also have received our Technical Guide.  Find out all about Queue-Fair at https://queue-fair.com

## Client-Side JavaScript Adapter

Most of our customers prefer to use the Client-Side JavaScript Adapter, which is suitable for all sites that wish solely to protect against overload.

To add the Queue-Fair Client-Side JavaScript Adapter to your web server, you don't need the cs files included in this extension.

Instead, add the following tag to the `<head>` section of your pages:
 
```
<script data-queue-fair-client="CLIENT_NAME" src="https://files.queue-fair.net/queue-fair-adapter.js"></script>`
```

Replace CLIENT_NAME with the account system name visibile on the Account -> Your Account page of the Queue-Fair Portal

You shoud now see the Adapter tag when you perform View Source after refreshing your pages.

And you're done!  Your queues and activation rules can now be configured in the Queue-Fair Portal.

## Server-Side Adapter

The Server-Side Adapter means that your web server communicates directly with the Queue-Fair servers, rather than your visitors' browsers.

This can introduce a dependency between our systems, which is why most customers prefer the Client-Side Adapter.  See Section 10 of the Technical Guide for help regarding which integration method is most suitable for you.

The Server-Side Adapter is a small .NET library that will run when visitors access your site.  It periodically checks to see if you have changed your Queue-Fair settings in the Portal, but other than that if the visitor is requesting a page that does not match any queue's Activation Rules, it does nothing.

If a visitor requests a page that DOES match any queue's Activation Rules, the Adapter consults the Queue-Fair Queue Servers to determine whether that particular visitor should be queued.  If so, the visitor is sent to our Queue Servers and execution and generation of the page for that HTTP request for that visitor will cease.  If the Adapter determines that the visitor should not be queued, it sets a cookie to indicate that the visitor has been processed and your page executes and shows as normal.

Thus the Server-Side Adapter prevents visitors from skipping the queue by disabling the Client-Side JavaScript Adapter, and also reduces load on your web server when things get busy.

This guide assumes you already have a functional .NET or ASP systemm, that you have dotnet installed, and are using Visual Studio Code  If you are creating a .NET or ASP application for the first time, see https://dotnet.microsoft.com/learn/aspnet/hello-world-tutorial/intro

Example code for integrating with Queue-Fair can be found in the Startup.cs file that is part of this distribution.  We'll walk you through creating a "Hello World" dotnet webapp, and then the process of adding Queue-Fair to your webapp.

Here's every keystroke for the install.

### Creating the Hello World Webapp ###

**1.** Open a command prompt or terminal.  Go to the directory in which you want the Hello World webapp to live.

```
  cd \path\to\webapps
```

**2.** To create the out-of-the-box .NET Hello World webapp, do

```
  dotnet new webApp -o QueueFairDemo --no-https
```

**3.** In File Explorer, copy the Startup.cs file from this distribution into the new QueueFairDemo folder that has been created, overwriting the one that's already there.

**4.** Open Visual Studio Code.  Use File -> Open Folder to open the QueueFairDemo folder.  Visual Studio Code will download some dependencies.  Wait for it to finish.  Your webapp won't build until you add the QueueFairAdapter code, as described in the next section.

### Adding Queue-Fair to an existing Webapp ###

**5.** Copy the QueueFairAdapter/QueueFair folder from this distribution into the top level folder of your webapp (QueueFairDemo if you are using the Hello World app).

**6.** Add

```
  using QueueFair.Adapter
```
  
at the top of any .cs files that you wish to protect.  It usually goes in Startup.cs, as then the Adapter can run on any of your pages that match your queue's Activation Rules.

**7.** The Adapter uses the Newtonsoft.JSON package for parsing JSON.  To add this, you need the NuGet Package Manager extension.  Find the icon in the left nav of Visual Studio Code that looks like four squares.  Tap it to open Extensions.  Start typing NuGet.  When you see NuGet Package Manager, install it.

**8.** To use NuGet Package Manager to install Newtonsoft.JSON, type CTRL-SHIFT-P to open the Command Palette.  Start typing 'NuGet'.  Select NuGet Packet Manager : Add Package.  Type Newtonsoft.  Select Newtonsoft.JSON.  Select the most recent version.  If a button pops up saying "Restore", press it.

**9.** **IMPORTANT:** Make sure the system clock on your webserver is accurately set to network time! On unix systems, this is usually done with the ntp package.  It doesn't matter which timezone you are using. On Windows 10, it's under Settings -> Date & Time - make sure Set the time automatically is On.  On Windows Server, this procedure may vary.

**10.** In the example code in Startup.cs, set your account name and account secret to the account System Name and Account Secret shown on the Your Account page of the Queue-Fair portal.

**11.** Note the `QueueFairConfig.SettingsFileCacheLifetimeMinutes` setting - this is how often your web server will check for updated settings from the Queue-Fair queue servers (which change when you hit Make Live).   The default value is 5 minutes.  You can set this to -1 to disable local caching but **DON'T DO THIS** on your production machine/live queue with real people, or your server may collapse under load.  On download, your settings are parsed and stored in a memory cache.  If you restart your .NET webserver a fressh copy will be downloaded.

**12.** Note the `QueueFairConfig.AdapterMode` setting.  "safe" is recommended - we also support "simple" - see the Technical Guide for further details.

**13.** **IMPORTANT** Note the `QueueFairConfig.debug` setting - this is set to true by default, BUT you MUST set debug to false on production machines/live queues as otherwise your web logs will rapidly become full.  You can also safely set `QueueFairConfig.DebugIPAddress` to a single IP address to just output debug information for a single visitor, even on a production machine.

The debug logging statements will appear in your .NET debug console (when using Visual Studio Code) and Event Viewer -> Windows Logs -> Application.  The default loglevel is Warning to make them easy to see, but you can change this by editing QueueFairCoreService.cs, which you can also change to use a different logging framework if you wish.

That's it you're done!  Build and run your Webapp.

In the case where the Adapter sends the request elsewhere (for example to show the user a queue page), the `IsContinue()` method will return false and the rest of the page will NOT be generated, which means it isn't sent to the visitor's browser, which makes it secure, as well as preventing your server from having to do the work of producing the rest of the page.  It is important that this code runs *before* any other .NET framework you may have in place initialises so that your server can perform this under load, when your full site frameworkis too onerous to load.  You may wish to also ensure that the Adapter only runs on page requests, rather than images or other files that may be served by your .NET server.

**NOTE:** If your web server is sitting behind a proxy, CDN or load balancer, you may need to edit the property sets that occur in Startup.cs before the IsContinue() method is called to use values from forwarded headers instead.  If you need help with this, contact Queue-Fair support.

**IMPORTANT:** If the Adapter needs to add a cookie or send a Location: header, these responses MUST NOT be cached.  The Adapter will add a suitable Cache-Control header if it does this.  You must ensure it is not overridden by any other Cache-Control header produced by your framework.  The one from the Adapter should be the only one present in the HTTP responses you can see in your browser's Inspector.

### To test the Server-Side Adapter

Use a queue that is not in use on other pages, or create a new queue for testing.  Add an Activation Rule for Path Contains /.  Hit Make Live.

#### Testing SafeGuard
Put the queue in SafeGuard mode.  Hit Make Live again.

In a new Private Browsing window, visit http://localhost:5000

 - Verify that you can see debug output from the Adapter in your Visual Studio console and/or Event Log.
 - Verify that a cookie has been created named `Queue-Fair-Pass-queuename`, where queuename is the System Name of your queue
 - If the Adapter is in Safe mode, also verify that a cookie has been created named QueueFair-Store-accountname, where accountname is the System Name of your account (on the Your Account page on the portal).
 - If the Adapter is in Simple mode, the Queue-Fair-Store cookie is not created.
 - Hit Refresh.  Verify that the cookie(s) have not changed their values.

#### Testing Queue
Go back to the Portal and put the queue in Demo mode on the Queue Settings page.  Hit Make Live.  Delete any Queue-Fair-Pass cookies from your browser.  In a new tab, visit https://accountname.queue-fair.net , and delete any Queue-Fair-Pass or Queue-Fair-Data cookies that appear there.  Go back to http://localhost:5000 , and Refresh.

 - Verify that you are now sent to queue.
 - When you come back to the page from the queue, verify that a new QueueFair-Pass-queuename cookie has been created.
 - If the Adapter is in Safe mode, also verify that the QueueFair-Store cookie has not changed its value.
 - Hit Refresh.  Verify that you are not queued again.  Verify that the cookies have not changed their values.

**IMPORTANT:**  Once you are sure the Server-Side Adapter is working as expected, you may remove the Client-Side JavaScript Adapter tag from your pages, and don't forget to disable debug level logging, and also set `QueueFairConfig.settingsFileCacheLifetimeMinutes` to at least 5 (its default value).

### For maximum security

The Server-Side Adapter contains multiple checks to prevent visitors bypassing the queue, either by tampering with set cookie values or query strings, or by sharing this information with each other.  When a tamper is detected, the visitor is treated as a new visitor, and will be sent to the back of the queue if people are queuing.

 - The Server-Side Adapter checks that Passed Cookies and Passed Strings presented by web browsers have been signed by our Queue-Server.  It uses the Secret visible on each queue's Settings page to do this.
 - If you change the queue Secret, this will invalidate everyone's cookies and also cause anyone in the queue to lose their place, so modify with care!
 - The Server-Side Adapter also checks that Passed Strings coming from our Queue Server to your web server were produced within the last 30 seconds, which is why your clock must be accurately set.
 -  The Server-Side Adapter also checks that passed cookies were produced within the time limit set by Passed Lifetime on the queue Settings page, to prevent visitors trying to cheat by tampering with cookie expiration times or sharing cookie values.  So, the Passed Lifetime should be set to long enough for your visitors to complete their transaction, plus an allowance for those visitors that are slow, but no longer.
 - The signature also includes the visitor's USER_AGENT, to further prevent visitors from sharing cookie values.

## Validating Cookies (Hybrid Security Model)
In many cases it is better to use the Client-Side Javascript Adapter to send and receive people to and from the queue - the reasons for this are covered in the Technical Guide.  If your aim with the Server Side adapter is merely to prevent the very small percentage of people who attempt to cheat the queue from ordering, you can leave the Client-Side Javascript Adapter in place and use the ValidateCookie method of the Adapter instead. Example code is also included in the `Startup.cs` file included in this distribution.

## AND FINALLY

All client-modifiable settings are in `QueueFairConfig.cs` .  You should never find you need to modify the other files except QueueFairCoreService.cs if you wish to use an alternative logging framework.  If you are using .NET Framework rather than .NET Core, or are using some other HTTP framework, you can easily create your own implementation of IQueueFairService - see QueueFairCoreService.cs to find out what they need to do; it's only a few basic low-level methods and shouldn't take you very long. If something comes up, please contact support@queue-fair.com right away so we can discuss your requirements.

Remember we are here to help you! The integration process shouldn't take you more than an hour - so if you are scratching your head, ask us.  Many answers are contained in the Technical Guide too.  We're always happy to help!
