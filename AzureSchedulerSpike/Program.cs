using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Scheduler;
using Microsoft.WindowsAzure.Management.Scheduler.Models;
using Microsoft.WindowsAzure.Scheduler;
using Microsoft.WindowsAzure.Scheduler.Models;

namespace AzureSchedulerSpike
{
    internal class Program
    {
        private const string FuturedonticsDevSubscriptionId = "5d3a761c-ef06-473b-a2e8-4e1ce00048ad";

        private static Options Options
        {
            get
            {
                return new Options
                {
                    CertificateFilePath = @"D:\GitHub\AzureSchedulerSpike\AzureManagementCertificate-LSotomayor.pfx",
                    CertificatePassword = "1800Dentist!",
                    SubscriptionId = FuturedonticsDevSubscriptionId,
                    CloudServiceName = "ls-test-cloud-service",
                    GeoRegion = GeoRegion.WestUs,
                    JobCollectionName = "ls-test-collection",
                    JobCollectionPlan = JobCollectionPlan.Free,
                    JobActionType = JobActionType.Http,
                    JobId = "ls-job-123"
                };
            }
        }

        private static void Main(string[] args)
        {
            var options = Options;

            var credentials = GetCertificateCredentials(
                options.SubscriptionId,
                options.CertificateFilePath,
                options.CertificatePassword);

            var cloudServiceWasCreated = CreateCloudService(
                credentials,
                options.CloudServiceName,
                options.GeoRegion);

            if (!cloudServiceWasCreated)
            {
                Console.Error.WriteLine("Cloud service could not be created");
                return;
            }

            var jobCollectionWasCreated = CreateJobCollection(credentials,
                                                              options.CloudServiceName,
                                                              options.JobCollectionName,
                                                              options.JobCollectionPlan);

            if (!jobCollectionWasCreated)
            {
                Console.Error.WriteLine("Job collection could not be created");
                return;
            }

            var jobWasCreated = CreateJob(credentials,
                                          options.CloudServiceName,
                                          options.JobCollectionName,
                                          options.JobId,
                                          options.JobActionType);

            if (!jobWasCreated)
            {
                Console.Error.WriteLine("Job could not be created");
                return;
            }

            Console.WriteLine("Job was created successfully");
        }

        private static SubscriptionCloudCredentials GetCertificateCredentials(
            string subscriptionId,
            string filePath,
            string password)
        {
            var certificate = new X509Certificate2(filePath, password);
            return new CertificateCloudCredentials(subscriptionId, certificate);
        }

        private static bool CreateCloudService(
            SubscriptionCloudCredentials credentials,
            string cloudServiceName,
            string geoRegion)
        {
            var client = new CloudServiceManagementClient(credentials);
            var exists = client.CloudServices
                               .List()
                               .Any(x => x.Name == cloudServiceName);

            if (exists)
            {
                Console.WriteLine("Cloud service {0} already exists", cloudServiceName);
                return true;
            }

            var response = client.CloudServices.Create(cloudServiceName,
                                                       new CloudServiceCreateParameters
                                                       {
                                                           GeoRegion = geoRegion,
                                                           Description = cloudServiceName,
                                                           Label = cloudServiceName
                                                       });

            return response.Status == CloudServiceOperationStatus.Succeeded;
        }

        private static bool CreateJobCollection(
            SubscriptionCloudCredentials credentials,
            string cloudServiceName,
            string jobCollectionName,
            JobCollectionPlan plan)
        {
            var client = new SchedulerManagementClient(credentials);
            var exists = !client.JobCollections
                                .CheckNameAvailability(cloudServiceName, jobCollectionName)
                                .IsAvailable;

            if (exists)
            {
                Console.WriteLine("Job collection {0} already exists", jobCollectionName);
                return true;
            }

            var intrinsicSettings = new JobCollectionIntrinsicSettings
            {
                Plan = plan
            };

            var response = client.JobCollections.Create(cloudServiceName,
                                                        jobCollectionName,
                                                        new JobCollectionCreateParameters
                                                        {
                                                            IntrinsicSettings = intrinsicSettings,
                                                            Label = jobCollectionName
                                                        });

            return response.Status == SchedulerOperationStatus.Succeeded;
        }

        private static bool CreateJob(
            SubscriptionCloudCredentials credentials,
            string cloudServiceName,
            string jobCollectionName,
            string jobId,
            JobActionType jobActionType)
        {
            var client = new SchedulerClient(
                cloudServiceName,
                jobCollectionName,
                credentials);

            var exists = client.Jobs
                               .List(new JobListParameters())
                               .Any(x => x.Id == jobId);

            if (exists)
            {
                Console.WriteLine("Job {0} already exists. Will be deleted", jobId);
                client.Jobs.Delete(jobId);
            }

            var jobAction = jobActionType == JobActionType.StorageQueue
                                ? GetQueueJobAction(TODO, TODO, TODO, TODO)
                                : GetHttpJobAction();

            var jobRecurrence = new JobRecurrence
            {
                Frequency = JobRecurrenceFrequency.Hour,
                Interval = 1,
                EndTime = DateTime.UtcNow.AddHours(5)
            };

            var jobCreateParameters = new JobCreateOrUpdateParameters
            {
                Action = jobAction,
                Recurrence = jobRecurrence
            };

            var jobCreateResponse = client.Jobs.CreateOrUpdate(jobId, jobCreateParameters);
            return jobCreateResponse.StatusCode == HttpStatusCode.Created;
        }

        private static JobAction GetHttpJobAction()
        {
            return new JobAction
            {
                Type = JobActionType.Http,
                Request = new JobHttpRequest
                {
                    Uri = new Uri("http://google.com"),
                    Method = "GET"
                }
            };
        }

        private static JobAction GetQueueJobAction(
            string message,
            string queueName,
            string sasToken,
            string storageAccountName)
        {
            return new JobAction
            {
                Type = JobActionType.StorageQueue,
                QueueMessage = new JobQueueMessage
                {
                    Message = message,
                    QueueName = queueName,
                    SasToken = sasToken,
                    StorageAccountName = storageAccountName
                }
            };
        }
    }
}