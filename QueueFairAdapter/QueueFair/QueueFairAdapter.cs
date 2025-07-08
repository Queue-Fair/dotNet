//-----------------------------------------------------------------------
// <copyright file="QueueFairAdapter.cs" company="Matt King for Orderly Telecoms">
// Copyright Matt King. All rights Reserved
// </copyright>
//-----------------------------------------------------------------------

namespace QueueFair.Adapter
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using Newtonsoft.Json;

    public class QueueFairAdapter
    {
        private static readonly uint[] Lookup32 = CreateLookup32();
        private static string cookieNameBase = "QueueFair-Pass-";
        private bool continuePage = true;
        private dynamic adapterResult = null;
        private QueueFairSettings.Queue adapterQueue = null;
        private Dictionary<string, bool> passedQueues = null;
        private string passedString = null;
        private string uid = null;
        private bool d = true;
        private bool addedCacheControlHeader = false;
        public QueueFairAdapter(IQueueFairService service)
        {
            this.d = QueueFairConfig.Debug;
            if (!this.d && QueueFairConfig.DebugIPAddress != null)
            {
                this.d = true;
            }

            this.Service = service;
        }

        public static string Protocol { get; set; } = "https";

        // Input - must be set before running:
        public string RequestedURL { get; set; } = null;
        public string RemoteIP { get; set; } = null;

        public string UserAgent { get; set; } = null;
        public bool IsSecure { get; set; } = false;
        public string Extra { get; set; } = null;
        // variables
        public string Config { get; set; } = null;

        public QueueFairSettings Settings { get; set; } = null;

        public IQueueFairService Service { get; set; } = null;

        public void SetuidFromCookie()
        {
            string cookieBase = "QueueFair-Store-" + QueueFairConfig.Account;

            string uidCookie = this.GetCookie(cookieBase);
            if (uidCookie == null || uidCookie == string.Empty)
            {
                return;
            }

            int i = uidCookie.IndexOf(":");
            if (i == -1)
            {
                i = uidCookie.IndexOf("=");
            }

            if (i == -1)
            {
                return;
            }

            this.uid = uidCookie.Substring(i + 1);
        }

        public string GetCookie(string cname)
        {
            string cookie = this.Service.GetCookie(cname);

            if (this.d)
            {
                this.Log("GetCookie: Cookie is " + cookie);
            }

            return cookie;
        }

        public void CheckAndAddCacheControl()
        {
            if (this.addedCacheControlHeader)
            {
                return;
            }

            this.Service.AddHeader("Cache-Control", "no-store,max-age=0");
            this.addedCacheControlHeader = true;
        }

        public bool IsContinue()
        {
            try
            {
                if (this.d)
                {
                    this.Log("IsContinue: Adapter Starting");
                }

                this.SetuidFromCookie();
                this.LoadSettings();
                if (this.d)
                {
                    this.Log("IsContinue: Adapter Finished " + this.continuePage);
                }
            }
            catch (Exception exception)
            {
                this.Err(exception);
            }

            return this.continuePage;
        }

        public void LoadSettings()
        {
            QueueFairSettings json = QueueFairConfig.SettingsSource.GetSettings(this);

            if (json == null)
            {
                if (this.d)
                {
                    this.Log("LoadSettings: No JSON returned. Returning.");
                }

                return;
            }

            if (this.d)
            {
                this.Log("LoadSettings: Using JSON Settings " + json);
            }

            this.Settings = json;

            this.GotSettings();
        }

        public void GotSettings()
        {
            if (this.d)
            {
                this.Log("GotSettings: got settings.");
            }

            this.CheckQueryString();
            if (!this.continuePage)
            {
                return;
            }

            this.ParseSettings();
        }

        public void ParseSettings()
        {
            if (this.Settings == null)
            {
                if (this.d)
                {
                    this.Log("ParseSettings ERROR: Settings not set.");
                }

                return;
            }

            QueueFairSettings.Queue[] queues = this.Settings.Queues;

            if (queues.Length == 0)
            {
                if (this.d)
                {
                    this.Log("ParseSettings No queues found.");
                }

                return;
            }

            if (this.d)
            {
                this.Log("ParseSettings Running through queue rules");
            }

            foreach (QueueFairSettings.Queue queue in queues)
            {
                if (this.IsMarkPassed(queue.Name))
                {
                    if (this.d)
                    {
                        this.Log("ParseSettings Passed from array " + queue.Name);
                    }

                    continue;
                }

                if (this.d)
                {
                    this.Log("ParseSettings Checking " + queue.DisplayName);
                }

                if (this.IsMatch(queue))
                {
                    if (this.d)
                    {
                        this.Log("ParseSettings Got a match " + queue.DisplayName);
                    }

                    if (!this.OnMatch(queue))
                    {
                        if (!this.continuePage)
                        {
                            return;
                        }

                        if (this.d)
                        {
                            this.Log("ParseSettings Found matching unpassed queue " + queue.DisplayName);
                        }

                        if (QueueFairConfig.AdapterMode == "simple")
                        {
                            return;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (!this.continuePage)
                    {
                        return;
                    }

                    // Passed.
                    this.MarkPassed(queue.Name);
                }
                else
                {
                    if (this.d)
                    {
                        this.Log("ParseSettings Rules did not match " + queue.DisplayName);
                    }
                }
            }

            if (this.d)
            {
                this.Log("ParseSettings All queues checked.");
            }
        }

        public void ConsultAdapter(QueueFairSettings.Queue queue)
        {
            if (this.d)
            {
                this.Log("ConsultAdapter Consulting Adapter Server for " + queue.Name);
            }

            this.adapterQueue = queue;
            string adapterMode = "safe";

            if (queue.AdapterMode != null && queue.AdapterMode != string.Empty)
            {
                adapterMode = queue.AdapterMode;
            }
            else if (QueueFairConfig.AdapterMode != null)
            {
                adapterMode = QueueFairConfig.AdapterMode;
            }

            if (this.d)
            {
                this.Log("ConsultAdapter Adapter mode is " + adapterMode);
            }

            if (adapterMode == "safe")
            {
                string url = Protocol + "://" + queue.AdapterServer + "/adapter/" + queue.Name;
                url += "?ipaddress=" + this.Service.Encode(this.RemoteIP);
                if (this.uid != null)
                {
                    url += "&uid=" + this.uid;
                }

                url += "&identifier=" + this.ProcessIdentifier(this.UserAgent);

                if (QueueFairConfig.SendURL)
                {
                     url += "&url=" + this.Service.Encode(this.GetURL());
                }
                
                if (this.d)
                {
                    this.Log("ConsultAdapter Consulting adapter at " + url);
                }

                string json = this.LoadURL(url);

                if (json == string.Empty)
                {
                    if (this.d)
                    {
                        this.Log("ConsultAdapter No Adapter JSON!");
                    }

                    return;
                }

                if (this.d)
                {
                    this.Log("ConsultAdapter Downloaded JSON Adapter " + json);
                }

                this.adapterResult = JsonConvert.DeserializeObject(json);
                this.GotAdapter(this.adapterQueue);
                if (!this.continuePage)
                {
                    return;
                }
            }
            else
            {
                string url = Protocol + "://" + queue.QueueServer + "/" + queue.Name + "?target=" + this.Service.Encode(this.GetURL());

                url = this.AppendVariantToRedirectLocation(queue, url);
                url = this.AppendExtraToRedirectLocation(queue, url);

                if (this.d)
                {
                    this.Log("ConsultAdapter Redirecting to queue server " + url);
                }

                this.Redirect(url, 0);
            }
        }

        public string GetVariant(QueueFairSettings.Queue queue)
        {
            if (this.d)
            {
                this.Log("GetVariant Getting variants for " + queue.Name);
            }

            if (queue.VariantRules == null)
            {
                return null;
            }

            QueueFairSettings.Variant[] variantRules = queue.VariantRules;

            if (this.d)
            {
                this.Log("GetVariant Got variant rules " + variantRules + " for " + queue.Name);
            }

            int lim = variantRules.Length;
            for (int i = 0; i < lim; i++)
            {
                QueueFairSettings.Variant variant = variantRules[i];
                string variantName = variant.VariantName;
                QueueFairSettings.Rule[] rules = variant.Rules;
                bool ret = this.IsMatchArray(rules);
                if (this.d)
                {
                    this.Log("GetVariant Variant match " + variantName + " " + ret);
                }

                if (ret)
                {
                    return variantName;
                }
            }

            return null;
        }

        public void GotAdapter(QueueFairSettings.Queue adapterQueue)
        {
            if (this.d)
            {
                this.Log("GotAdapter Got adapter");
            }

            if (this.adapterResult == null)
            {
                if (this.d)
                {
                    this.Log("ERROR: GotAdapter called without result");
                }

                return;
            }

            if (this.adapterResult.uid != null && Convert.ToString(this.adapterResult.uid) != string.Empty)
            {
                if (this.uid != null && this.uid != Convert.ToString(this.adapterResult.uid))
                {
                    if (this.d)
                    {
                        this.Log("ERROR: GotAdapter uid Cookie Mismatch - Contact Queue-Fair Support! expected " + this.uid + " but received " + this.adapterResult.uid);
                    }
                }
                else
                {
                    this.uid = Convert.ToString(this.adapterResult.uid);
                    this.CheckAndAddCacheControl();
                    this.SetCookieRaw("QueueFair-Store-" + QueueFairConfig.Account, "u:" + this.uid, Convert.ToInt32(this.adapterResult.cookieSeconds), adapterQueue.CookieDomain);
                }
            }

            if (this.adapterResult.action == null)
            {
                if (this.d)
                {
                    this.Log("ERROR: GotAdapter called without result action");
                }

                return;
            }

            if (Convert.ToString(this.adapterResult.action) == "SendToQueue")
            {
                if (this.d)
                {
                    this.Log("GotAdapter Sending to queue server.");
                }

                string dt = adapterQueue.DynamicTarget;
                string queryParams = string.Empty;
                string target = this.GetURL();
                if (dt != "disabled")
                {
                    if (dt == "path")
                    {
                        int i = target.IndexOf("?");
                        if (i != -1)
                        {
                            target = target.Substring(0, i);
                        }
                    }

                    queryParams += "target=";
                    queryParams += this.Service.Encode(target);
                }

                if (this.uid != null)
                {
                    if (queryParams != string.Empty)
                    {
                        queryParams += "&";
                    }

                    queryParams += "qfuid=" + this.uid;
                }

                string redirectLoc = this.adapterResult.location;
                if (queryParams != string.Empty)
                {
                    redirectLoc = redirectLoc + "?" + queryParams;
                }

                redirectLoc = this.AppendVariantToRedirectLocation(this.adapterQueue, redirectLoc);
                redirectLoc = this.AppendExtraToRedirectLocation(this.adapterQueue, redirectLoc);

                if (this.d)
                {
                    this.Log("GotAdapter Redirecting to " + redirectLoc);
                }

                this.Redirect(redirectLoc, 0);
                return;
            }

            // SafeGuard etc
            this.SetCookie(
                Convert.ToString(this.adapterResult.queue),
                this.Service.Decode(Convert.ToString(this.adapterResult.validation)),
                this.adapterQueue.PassedLifetimeMinutes * 60,
                Convert.ToString(this.adapterQueue.CookieDomain));
            if (!this.continuePage)
            {
                return;
            }

            if (this.d)
            {
                this.Log("GotAdapter Marking " + this.adapterResult.queue + " as passed by adapter.");
            }

            this.MarkPassed(Convert.ToString(this.adapterResult.queue));
        }

        public bool OnMatch(QueueFairSettings.Queue queue)
        {
            if (this.IsPassed(queue))
            {
                if (this.d)
                {
                    this.Log("OnMatch Already passed " + queue.Name + ".");
                }

                return true;
            }
            else if (!this.continuePage)
            {
                return false;
            }

            if (this.d)
            {
                this.Log("Checking at server " + queue.DisplayName);
            }

            this.ConsultAdapter(queue);
            return false;
        }

        public bool IsPassed(QueueFairSettings.Queue queue)
        {
            if (this.IsMarkPassed(queue.Name))
            {
                if (this.d)
                {
                    this.Log("IsPassed Queue " + queue.Name + " marked as passed already.");
                }

                return true;
            }

            string queueCookie = this.GetCookie(cookieNameBase + queue.Name);

            if (queueCookie == string.Empty || queueCookie == null)
            {
                if (this.d)
                {
                    this.Log("IsPassed No cookie found for queue " + queue.Name);
                }

                return false;
            }

            if (queueCookie.IndexOf(queue.Name) == -1)
            {
                if (this.d)
                {
                    this.Log("IsPAssed Cookie value is invalid for " + queue.Name);
                }

                return false;
            }

            if (!this.ValidateCookie(queue, queueCookie))
            {
                if (this.d)
                {
                    this.Log("IsPassed Cookie failed validation " + queueCookie);
                }

                this.SetCookie(queue.Name, string.Empty, 0, queue.CookieDomain);
                return false;
            }

            if (this.d)
            {
                this.Log("IsPassed Found valid cookie for " + queue.Name);
            }

            return true;
        }

        public bool IsRuleMatch(QueueFairSettings.Rule rule)
        {
            string comp = this.GetURL();
            if (this.d)
            {
                this.Log("IsRuleMatch Checking rule against " + comp);
            }

            string cs = rule.Component;

            if (cs == "Domain")
            {
                comp = comp.Replace("http://", string.Empty);
                comp = comp.Replace("https://", string.Empty);

                comp = Regex.Split(comp, "[/?#:]")[0];
            }
            else if (cs == "Path")
            {
                string domain = comp.Replace("http://", string.Empty);
                domain = domain.Replace("https://", string.Empty);
                domain = Regex.Split(domain, "[/?#:]")[0];
                comp = comp.Substring(comp.IndexOf(domain) + domain.Length);

                if (comp.StartsWith(":"))
                {
                    int j = comp.IndexOf("/");
                    if (j != -1)
                    {
                        comp = comp.Substring(j);
                    }
                    else
                    {
                        comp = string.Empty;
                    }
                }

                int i = comp.IndexOf("#");
                if (i != -1)
                {
                    comp = comp.Substring(0, i);
                }

                i = comp.IndexOf("?");
                if (i != -1)
                {
                    comp = comp.Substring(0, i);
                }

                if (comp == string.Empty)
                {
                    comp = "/";
                }
            }
            else if (cs == "Query")
            {
                if (comp.IndexOf("?") == -1)
                {
                    comp = string.Empty;
                }
                else if (comp == "?")
                {
                    comp = string.Empty;
                }
                else
                {
                    comp = comp.Substring(comp.IndexOf("?") + 1);
                }
            }
            else if (cs == "Cookie")
            {
                comp = this.GetCookie(rule.Name);
            }

            string test = rule.Value;

            if (rule.CaseSensitive == false)
            {
                comp = comp.ToLower();
                test = test.ToLower();
            }

            if (this.d)
            {
                this.Log("Testing " + rule.Component + " " + test + " against " + comp);
            }

            bool ret = false;

            string match = rule.Match;

            if (match == "Equal" && comp == test)
            {
                ret = true;
            }
            else if (match == "Contain" && comp != null && comp != string.Empty && comp.IndexOf(test) != -1)
            {
                ret = true;
            }
            else if (match == "Exist")
            {
                if (comp == null || comp == string.Empty)
                {
                    ret = false;
                }
                else
                {
                    ret = true;
                }
            }
            else if (match == "RegExp")
            {
                if (comp == null || comp == string.Empty)
                {
                    ret = false;
                }
                else
                {
                    ret = Regex.IsMatch(comp, test);
                }
            }

            if (rule.Negate)
            {
                ret = !ret;
            }

            return ret;
        }

        public bool IsMatch(QueueFairSettings.Queue queue)
        {
            if (queue == null || queue.Rules == null)
            {
                return false;
            }

            return this.IsMatchArray(queue.Rules);
        }

        public bool IsMatchArray(QueueFairSettings.Rule[] arr)
        {
            if (arr == null)
            {
                return false;
            }

            bool firstOp = true;
            bool state = false;

            for (int i = 0; i < arr.Length; i++)
            {
                QueueFairSettings.Rule rule = arr[i];

                if (!firstOp && rule.Operator != null)
                {
                    if (rule.Operator == "And" && !state)
                    {
                        return false;
                    }
                    else if (rule.Operator == "Or" && state)
                    {
                        return true;
                    }
                }

                bool ruleMatch = this.IsRuleMatch(rule);
                if (firstOp)
                {
                    state = ruleMatch;
                    firstOp = false;
                }
                else
                {
                    if (rule.Operator == "And")
                    {
                        state = state && ruleMatch;
                        if (!state)
                        {
                            break;
                        }
                    }
                    else if (rule.Operator == "Or")
                    {
                        state = state || ruleMatch;
                        if (state)
                        {
                            break;
                        }
                    }
                }
            }

            if (this.d)
            {
                this.Log("IsMatchArray result is " + state);
            }

            return state;
        }

        public void CheckQueryString()
        {
            string urlParams = this.GetURL();
            if (this.d)
            {
                this.Log("CheckQueryString Checking URL for Passed String " + urlParams);
            }

            int q = urlParams.IndexOf("qfqid");
            if (q == -1)
            {
                return;
            }

            if (this.d)
            {
                this.Log("CheckQueryString Passed String Found");
            }

            int i = urlParams.LastIndexOf("qfq=");
            if (i == -1)
            {
                return;
            }

            if (this.d)
            {
                this.Log("CheckQueryString Passed String with Queue Name found");
            }

            int j = urlParams.IndexOf("&", i);

            int subStart = i + "qfq=".Length;
            string queueName = urlParams.Substring(subStart, j - subStart);

            if (this.d)
            {
                this.Log("CheckQueryString Queue name is " + queueName);
            }

            int lim = this.Settings.Queues.Length;

            for (i = 0; i < lim; i++)
            {
                QueueFairSettings.Queue queue = this.Settings.Queues[i];
                if (queue.Name != queueName)
                {
                    continue;
                }

                if (this.d)
                {
                    this.Log("CheckQueryString Found Queue for querystring " + queueName);
                }

                string value = urlParams.Substring(urlParams.LastIndexOf("qfqid"));
                if (!this.ValidateQuery(queue))
                {
                    string queueCookie = this.GetCookie(cookieNameBase + queueName);
                    if (queueCookie != string.Empty)
                    {
                        if (this.d)
                        {
                            this.Log("CheckQueryString Query validation failed but we have cookie " + queueCookie);
                        }

                        if (this.ValidateCookie(queue, queueCookie))
                        {
                            if (this.d)
                            {
                                this.Log("CheckQueryString ...and the cookie is valid. That's fine.");
                            }

                            return;
                        }

                        if (this.d)
                        {
                            this.Log("CheckQueryString Query AND Cookie validation failed!!!");
                        }
                    }
                    else if (this.d)
                    {
                        this.Log("CheckQueryString Bad queueCookie for " + queueName + " " + queueCookie);
                    }

                    if (this.d)
                    {
                        this.Log("CheckQueryString Query validation failed - redirecting to error page.");
                    }

                    string loc = Protocol + "://" + queue.QueueServer + "/" + queue.Name + "?qfError=InvalidQuery";
                    this.Redirect(loc, 1);
                    return;
                }

                if (this.d)
                {
                    this.Log("CheckQueryString Query validation succeeded for " + value);
                }

                this.passedString = value;
                if (this.d)
                {
                    this.Log("Setting cookie with " + queueName + " " + value + " " + (queue.PassedLifetimeMinutes * 60));
                }

                this.SetCookie(queueName, value, queue.PassedLifetimeMinutes * 60, queue.CookieDomain);
                if (this.d)
                {
                    this.Log("CheckQueryString Marking " + queueName + " as passed by queryString");
                }

                if (!this.continuePage)
                {
                    return;
                }

                this.MarkPassed(queueName);
            }
        }

        public string GetQueryParameter(string name)
        {
            return this.Service.GetQueryParameter(name);
        }

        public bool IsNumeric(string input)
        {
            int i = 0;
            return int.TryParse(input, out i);
        }

        public long Time()
        {
            return this.Service.UnixTimeSeconds();
        }

        public string CreateHash(string secret, string message)
        {
            try
            {
                HMACSHA256 sha256HMAC = new HMACSHA256(Encoding.UTF8.GetBytes(secret));

                byte[] hashBytes = sha256HMAC.ComputeHash(Encoding.UTF8.GetBytes(message));

                return ByteArrayToHexViaLookup32(hashBytes);
            }
            catch (Exception exception)
            {
                if (this.d)
                {
                    this.Log("Could not create hash: " + exception.Message);
                }
            }

            return null;
        }

        public bool ValidateQuery(QueueFairSettings.Queue queue)
        {
            try
            {
                int i = this.RequestedURL.IndexOf("?");
                if (i == -1)
                {
                    return false;
                }

                string str = this.RequestedURL.Substring(i + 1);

                if (this.d)
                {
                    this.Log("ValidateQuery Validating Passed Query " + str);
                }

                int hpos = str.LastIndexOf("qfh=");
                if (hpos == -1)
                {
                    if (this.d)
                    {
                        this.Log("ValidateQuery No Hash In Query");
                    }

                    return false;
                }

                string queryHash = this.GetQueryParameter("qfh");
                if (queryHash == null || queryHash == string.Empty)
                {
                    if (this.d)
                    {
                        this.Log("ValidateQuery Malfored Hash");
                    }

                    return false;
                }

                int qpos = str.LastIndexOf("qfqid=");

                if (qpos == -1)
                {
                    if (this.d)
                    {
                        this.Log("ValidateQuery No Queue Identifier");
                    }

                    return false;
                }

                string queryQID = this.GetQueryParameter("qfqid");
                string queryTS = this.GetQueryParameter("qfts");
                string queryAccount = this.GetQueryParameter("qfa");
                string queryQueue = this.GetQueryParameter("qfq");
                string queryPassType = this.GetQueryParameter("qfpt");

                long queryTSLong;

                if (!this.IsNumeric(queryTS))
                {
                    if (this.d)
                    {
                        this.Log("ValidateQuery Timestamp Not Numeric " + queryTS);
                    }

                    return false;
                }

                if (queryTS == null || long.TryParse(queryTS, out queryTSLong) == false)
                {
                    if (this.d)
                    {
                        this.Log("ValidateQuery No/Bad Timestamp " + queryTS);
                    }

                    return false;
                }

                if (queryTSLong > this.Time() + QueueFairConfig.QueryTimeLimitSeconds)
                {
                    if (this.d)
                    {
                        this.Log("ValidateQuery Too Late " + queryTS + " " + this.Time());
                    }

                    return false;
                }

                if (queryTSLong < this.Time() - QueueFairConfig.QueryTimeLimitSeconds)
                {
                    if (this.d)
                    {
                        this.Log("ValidateQuery Too Early " + queryTS + " " + this.Time());
                    }

                    return false;
                }

                string check = str.Substring(qpos, hpos - qpos);

                string checkHash = this.CreateHash(queue.Secret, this.ProcessIdentifier(this.UserAgent) + check);

                if (checkHash != queryHash)
                {
                    if (this.d)
                    {
                        this.Log("ValidateQuery Failed Hash");
                    }

                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                if (this.d)
                {
                    this.Log("ValidateQuery failed: " + exception.Message);
                }
            }

            return false;
        }

        public string ProcessIdentifier(string param)
        {
            if (param == null)
            {
                return null;
            }

            int i = param.IndexOf("[");
            if (i == -1)
            {
                return param;
            }

            if (i < 20)
            {
                return param;
            }

            return param.Substring(0, i);
        }

        public bool ValidateCookie(QueueFairSettings.Queue queue, string cookie) {
            return ValidateCookie(queue.Secret, queue.PassedLifetimeMinutes, cookie);
        }

        public bool ValidateCookie(string secret, int passedLifetimeMinutes, string cookie)
        {
            if (this.d)
            {
                this.Log("ValidateCookie Validating cookie " + cookie);
            }

            if (secret == null)
            {
                return false;
            }

            if (cookie == null || cookie == string.Empty)
            {
                return false;
            }

            try
            {
                int hpos = cookie.LastIndexOf("qfh=");

                if (hpos == -1)
                {
                    if (this.d)
                    {
                        this.Log("ValidateCookie cookie has no hash!" + cookie);
                    }

                    return false;
                }

                string hash = cookie.Substring(hpos + "qfh=".Length);

                string check = cookie.Substring(0, hpos);

                string checkHash = this.CreateHash(secret, this.ProcessIdentifier(this.UserAgent) + check);

                if (hash != checkHash)
                {
                    if (this.d)
                    {
                        this.Log("ValidateCookie Cookie Hash Mismatch given " + hash + " should be " + checkHash);
                    }

                    return false;
                }

                int tspos = cookie.LastIndexOf("qfts=");
                if (tspos == -1)
                {
                    if (this.d)
                    {
                        this.Log("ValidateCookie cookie has no timestamp!" + cookie);
                    }

                    return false;
                }

                int tsEnd = cookie.IndexOf("&", tspos);
                if (tsEnd == -1)
                {
                    if (this.d)
                    {
                        this.Log("ValidateCookie cookie can't read timestamp!" + cookie);
                    }

                    return false;
                }

                string ts = cookie.Substring(tspos + "qfts=".Length, tsEnd - tspos - "qfts=".Length);

                long tsLong;

                if (!this.IsNumeric(ts))
                {
                    if (this.d)
                    {
                        this.Log("ValidateCookie Timestamp Not Numeric " + ts);
                    }

                    return false;
                }

                if (ts == null || long.TryParse(ts, out tsLong) == false)
                {
                    if (this.d)
                    {
                        this.Log("ValidateCookie No/Bad Timestamp " + ts);
                    }

                    return false;
                }

                if (tsLong < this.Time() - (passedLifetimeMinutes * 60))
                {
                    if (this.d)
                    {
                        this.Log("ValidateCookie Too Old " + ts + " " + this.Time());
                    }

                    return false;
                }

                if (this.d)
                {
                    this.Log("ValidateCookie Cookie Validated");
                }

                return true;
            }
            catch (Exception exception)
            {
                if (this.d)
                {
                    this.Log("ValidateCookie failed: " + exception.Message);
                }
            }

            return false;
        }

        public void SetCookieRaw(string cookieName, string value, int lifetimeSeconds, string cookieDomain)
        {
            this.Service.SetCookie(cookieName,value,lifetimeSeconds,cookieDomain,this.IsSecure);
        }

        public void SetCookie(string queueName, string value, int lifetimeSeconds, string cookieDomain)
        {
            if (this.d)
            {
                this.Log("SetCookie Setting Cookie for " + queueName + " to " + value);
            }

            string cookieName = cookieNameBase + queueName;
            this.CheckAndAddCacheControl();

            this.SetCookieRaw(cookieName, value, lifetimeSeconds, cookieDomain);

            if (lifetimeSeconds > 0)
            {
                this.MarkPassed(queueName);
                if (QueueFairConfig.StripPassedString)
                {
                    string loc = this.GetURL();
                    int pos = loc.IndexOf("qfqid=");
                    if (pos == -1)
                    {
                        return;
                    }

                    if (this.d)
                    {
                        this.Log("SetCookie Stripping PassedString from URL");
                    }

                    loc = loc.Substring(0, pos - 1);
                    this.Redirect(loc, 0);
                }
            }
        }

        public void Redirect(string loc, int sleep)
        {
            if (sleep > 0)
            {
                Thread.Sleep(sleep * 1000);
            }

            this.continuePage = false;

            this.CheckAndAddCacheControl();
            this.Service.Redirect(loc);
        }

        public void MarkPassed(string name)
        {
            if (this.passedQueues == null)
            {
                this.passedQueues = new Dictionary<string, bool>();
            }

            // Barfs if attempt to repeat add a key.
            if (this.passedQueues.ContainsKey(name))
            {
                return;
            }

            this.passedQueues.Add(name, true);
        }

        public bool IsMarkPassed(string name)
        {
            if (this.passedQueues == null)
            {
                return false;
            }

            bool ret = false;
            if (!this.passedQueues.TryGetValue(name, out ret))
            {
                return false;
            }

            return ret;
        }

        public string GetURL()
        {
            return this.RequestedURL;
        }

        public string AppendVariantToRedirectLocation(QueueFairSettings.Queue queue, string url)
        {
            if (this.d)
            {
                this.Log("appendVariant looking for variant");
            }

            string variant = this.GetVariant(queue);
            if (variant == null)
            {
                if (this.d)
                {
                    this.Log("appendVariant no variant found.");
                }

                return url;
            }

            if (this.d)
            {
                this.Log("appendVariant found " + variant);
            }

            if (url.IndexOf("?") == -1)
            {
                url += "?";
            }
            else
            {
                url += "&";
            }

            url += "qfv=" + this.Service.Encode(variant);
            return url;
        }

        public string AppendExtraToRedirectLocation(QueueFairSettings.Queue queue, string url)
        {
            if (this.Extra == null)
            {
                return url;
            }

            if (this.d)
            {
                this.Log("appendExtra found Extra " + this.Extra);
            }

            if (url.IndexOf("?") == -1)
            {
                url += "?";
            }
            else
            {
                url += "&";
            }

            url += "qfx=" + this.Service.Encode(this.Extra);
            return url;
        }

        public string LoadURL(string url)
        {
            WebRequest request = WebRequest.Create(url);
            request.Timeout = QueueFairConfig.ReadTimeout * 1000;

            WebResponse response = request.GetResponse();
            string ret = string.Empty;

            if (response == null || ((HttpWebResponse)response).StatusCode != HttpStatusCode.OK)
            {
                if (this.d)
                {
                    this.Log("LoadURL got null response or invalid response status code.");
                }

                return string.Empty;
            }

            using (Stream dataStream = response.GetResponseStream())
            {
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);

                // Read the content.
                ret = reader.ReadToEnd();

                // Display the content.
            }

            response.Close();
            return ret;
        }

        public void Log(string message)
        {
            if (this.Service == null)
            {
                return;
            }

            if (!QueueFairConfig.Debug)
            {
                return;
            }

            if (QueueFairConfig.DebugIPAddress != null
               && this.Service != null
               && this.RemoteIP != QueueFairConfig.DebugIPAddress)
            {
                return;
            }

            this.Service.Log(message);
        }

        public void Err(Exception exception)
        {
            this.Service.Err(exception);
        }

        private static uint[] CreateLookup32()
        {
            var result = new uint[256];
            string s;
            for (int i = 0; i < 256; i++)
            {
                s = i.ToString("x2");
                result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
            }

            return result;
        }

        private static string ByteArrayToHexViaLookup32(byte[] bytes)
        {
            var tLookup32 = Lookup32;
            var result = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                var val = tLookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[(2 * i) + 1] = (char)(val >> 16);
            }

            return new string(result);
        }
    }
}
