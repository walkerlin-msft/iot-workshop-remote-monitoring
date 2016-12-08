using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RefBlobConsoleApp
{
    class Program
    {
        private const string STORAGEACCOUNT_PROTOCOL = "https";// We use HTTPS to access the storage account
        private const double CUTOUTSPEED = 14f;
        private const double REPAIR = 0.5f;

        private const string CONTAINER_NAME = "devicerules";// It's hard-coded for this workshop
        private const string BLOB_NAME = "devicerules.json";// It's hard-coded for this workshop
        private const string DEVICEID_WINDOWS_TURBINE = "WinTurbine";// It's hard-coded for this workshop
        private const string DEVICEID_LINUX_TURBINE = "LinuxTurbine";// It's hard-coded for this workshop

        static void Main(string[] args)
        {
            Console.WriteLine("Console App for creating the reference blob...\n");

            /* Load the settings from App.config */
            string storageAccountName = ConfigurationManager.AppSettings["StorageAccountName"];
            string storageAccountKey = ConfigurationManager.AppSettings["StorageAccountKey"];

            string connectionString = CombineConnectionString(storageAccountName, storageAccountKey);
            Console.WriteLine("connectionString={0}\n", connectionString);

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container.
            CloudBlobContainer container = blobClient.GetContainerReference(CONTAINER_NAME);

            // Create the container if it doesn't already exist.
            container.CreateIfNotExists();

            // List all blobs of this container
            ListAllblobs(blobClient, CONTAINER_NAME, true);

            // Create and upload the blob
            CreateAndUploadBlob(container, GetBlobFileName());
            
            Console.ReadLine();

        }

        private static string CombineConnectionString(string storageAccountName, string storageAccountKey)
        {
            return "DefaultEndpointsProtocol=" + STORAGEACCOUNT_PROTOCOL + ";" +
                "AccountName=" + storageAccountName + ";" +
                "AccountKey=" + storageAccountKey;
        }

        private static void ListAllblobs(CloudBlobClient blobClient, string containerName, Boolean useFlatBlobListing)
        {
            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            if (container.ListBlobs(null, false).Count() == 0)
            {
                Console.WriteLine("No any blob was found in {0}\n", containerName);
            }

            // Loop over items within the container and output the length and URI.
            foreach (IListBlobItem item in container.ListBlobs(null, useFlatBlobListing))
            {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;

                    Console.WriteLine("Block blob of length {0}: {1}\n", blob.Properties.Length, blob.Uri);

                }
                else if (item.GetType() == typeof(CloudPageBlob))
                {
                    CloudPageBlob pageBlob = (CloudPageBlob)item;

                    Console.WriteLine("Page blob of length {0}: {1}\n", pageBlob.Properties.Length, pageBlob.Uri);

                }
                else if (item.GetType() == typeof(CloudBlobDirectory))
                {
                    CloudBlobDirectory directory = (CloudBlobDirectory)item;

                    Console.WriteLine("Directory: {0}\n", directory.Uri);
                }
            }
        }

        private static void CreateAndUploadBlob(CloudBlobContainer container, string blobName)
        {
            Console.WriteLine("container={0}, blobName={1}\n", container.Name, blobName);
            // Retrieve reference to a blob named
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

            // Create the device rules for the content of blob
            String blobContent = CreateDeviceRules();
            Console.WriteLine("blobContent={0}\n", blobContent);

            byte[] content = ASCIIEncoding.ASCII.GetBytes(blobContent);
            blockBlob.UploadFromByteArrayAsync(content, 0, content.Count()).Wait();
            Console.WriteLine("upload successful content.Count()={0}\n", content.Count());
        }

        private static String CreateDeviceRules()
        {
            DeviceRule deviceRule1 = new DeviceRule();
            deviceRule1.DeviceID = DEVICEID_LINUX_TURBINE;
            deviceRule1.CutOutSpeed = CUTOUTSPEED;
            deviceRule1.Repair = REPAIR;
            deviceRule1.Altitude = 239.6648864f;
            deviceRule1.Latitude = 25.037531f;
            deviceRule1.Longitude = 121.5639969f;

            DeviceRule deviceRule2 = new DeviceRule();
            deviceRule2.DeviceID = DEVICEID_WINDOWS_TURBINE;
            deviceRule2.CutOutSpeed = CUTOUTSPEED;
            deviceRule2.Repair = REPAIR;
            deviceRule2.Altitude = 239.6648864f;
            deviceRule2.Latitude = 22.6235806f;
            deviceRule2.Longitude = 120.2913016f;

            List<DeviceRule> deviceRules = new List<DeviceRule>();
            deviceRules.Add(deviceRule1);
            deviceRules.Add(deviceRule2);
            return JsonConvert.SerializeObject(deviceRules);
        }

        //When we save data to the blob storage for use as ref data on an ASA job, ASA picks that
        //data up based on the current time, and the data must be finished uploading before that time.
        //
        //From the Azure Team: "What this means is your blob in the path 
        //<...>/devicerules/2015-09-23/15-24/devicerules.json needs to be uploaded before the clock 
        //strikes 2015-09-23 15:25:00 UTC, preferably before 2015-09-23 15:24:00 UTC to be used when 
        //the clock strikes 2015-09-23 15:24:00 UTC"
        //
        //If we have many devices, an upload could take a measurable amount of time.
        //
        //Also, it is possible that the ASA clock is not precisely in sync with the
        //server clock. We want to store our update on a path slightly ahead of the current time so
        //that by the time ASA reads it we will no longer be making any updates to that blob -- i.e.
        //all current changes go into a future blob. We will choose two minutes into the future. In the
        //worst case, if we make a change at 12:03:59 and our write is delayed by ten seconds (until 
        //12:04:09) it will still be saved on the path {date}\12-05 and will be waiting for ASA to 
        //find in one minute.
        private const int blobSaveMinutesInTheFuture = 2;
        private const int blobSaveSecondsInTheFuture = 20;
        private static DateTimeFormatInfo _formatInfo;

        private static string GetBlobFileName()
        {
            // note: InvariantCulture is read-only, so use en-US and hardcode all relevant aspects
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            _formatInfo = culture.DateTimeFormat;
            _formatInfo.ShortDatePattern = @"yyyy-MM-dd";
            _formatInfo.ShortTimePattern = @"HH-mm";

            //DateTime saveDate = DateTime.UtcNow.AddMinutes(blobSaveMinutesInTheFuture);
            DateTime saveDate = DateTime.UtcNow.AddSeconds(blobSaveSecondsInTheFuture);// for workshop
            string dateString = saveDate.ToString("d", _formatInfo);
            string timeString = saveDate.ToString("t", _formatInfo);
            string blobName = string.Format(@"{0}\{1}\{2}", dateString, timeString, BLOB_NAME);

            return blobName;
        }
    }
}
