using Microsoft.WindowsAzure.Management.Scheduler.Models;
using Microsoft.WindowsAzure.Scheduler.Models;

namespace AzureSchedulerSpike
{
    internal class Options
    {
        public string SubscriptionId { get; set; }
        public string CertificateFilePath { get; set; }
        public string CertificatePassword { get; set; }
        public string JobCollectionName { get; set; }
        public string CloudServiceName { get; set; }
        public string GeoRegion { get; set; }
        public JobCollectionPlan JobCollectionPlan { get; set; }
        public string JobId { get; set; }
        public JobActionType JobActionType { get; set; }
    }
}