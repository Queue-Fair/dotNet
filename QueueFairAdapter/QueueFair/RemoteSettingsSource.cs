//-----------------------------------------------------------------------
// <copyright file="RemoteSettingsSource.cs" company="Matt King for Orderly Telecoms">
// Copyright Matt King. All rights Reserved
// </copyright>
//-----------------------------------------------------------------------
namespace QueueFair.Adapter
{
    using System;
    using Newtonsoft.Json;

    public class RemoteSettingsSource : IQueueFairSettingsSource
    {
        private int lastDownload = -1;
        private QueueFairSettings saved = null;
        private string lockStr = "Lock";

        public QueueFairSettings GetSettings(QueueFairAdapter adapter)
        {
            if (Environment.TickCount - this.lastDownload < QueueFairConfig.SettingsCacheLifetimeMinutes * 60000 && this.saved != null)
            {
                if (QueueFairConfig.Debug)
                {
                    adapter.Log("QF GetSettings returning saved settings");
                }

                return this.saved;
            }

            lock (this.lockStr)
            {
                if (Environment.TickCount - this.lastDownload < QueueFairConfig.SettingsCacheLifetimeMinutes * 60000 && this.saved != null)
                {
                    if (QueueFairConfig.Debug)
                    {
                        adapter.Log("QF GetSettings returning saved settings in lock");
                    }

                    return this.saved;
                }

                this.DownloadSettings(adapter);
            }

            return this.saved;
        }

        public void DownloadSettings(QueueFairAdapter adapter)
        {
            adapter.Log("DownloadSettings downloading...");
            var url = QueueFairAdapter.Protocol
                        + "://" + QueueFairConfig.FilesServer
                        + "/" + QueueFairConfig.Account
                        + "/" + QueueFairConfig.AccountSecret
                        + "/queue-fair-settings.json";

            for (int i = 0; i < 3; i++)
            {
                var content = adapter.LoadURL(url);
                if (content != string.Empty)
                {
                    if (QueueFairConfig.Debug)
                    {
                        adapter.Log("DownloadSettings got " + content);
                    }

                    this.lastDownload = Environment.TickCount;
                    dynamic data = JsonConvert.DeserializeObject(content);
                    if (data != null)
                    {
                        this.saved = new QueueFairSettings(data);
                    }

                    return;
                }

                if (QueueFairConfig.Debug)
                {
                    adapter.Log("DownloadSettings couldn't download try " + i);
                }
            }

            if (QueueFairConfig.Debug)
            {
                adapter.Log("DownloadSettings couldn't download.");
            }

            return;
        }
    }
}