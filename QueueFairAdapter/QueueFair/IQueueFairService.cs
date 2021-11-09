//-----------------------------------------------------------------------
// <copyright file="IQueueFairSettingsSource.cs" company="Matt King for Orderly Telecoms">
// Copyright Matt King. All rights Reserved
// </copyright>
//-----------------------------------------------------------------------
namespace QueueFair.Adapter
{
    using System;
    public interface IQueueFairService
    {
        void Log(string message);
        void Err(Exception exception);
        void AddHeader(string name, string value);
        string Encode(string value);
        string Decode(string value);
        string GetQueryParameter(string name);
        void Redirect(string loc);
        string GetCookie(string cname);
        void SetCookie(string cookieName, string value, int lifetimeSeconds, string cookieDomain, bool isSecure);
        long UnixTimeSeconds();
    }
}
