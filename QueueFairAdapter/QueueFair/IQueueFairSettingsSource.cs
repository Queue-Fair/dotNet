//-----------------------------------------------------------------------
// <copyright file="IQueueFairSettingsSource.cs" company="Matt King for Orderly Telecoms">
// Copyright Matt King. All rights Reserved
// </copyright>
//-----------------------------------------------------------------------
namespace QueueFair.Adapter
{
    public interface IQueueFairSettingsSource
    {
        QueueFairSettings GetSettings(QueueFairAdapter adapter);
    }
}