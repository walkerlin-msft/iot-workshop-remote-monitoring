using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using paas_demo.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace paas_demo.Controllers
{
    public class SettingsController : Controller
    {
        /* For Blob */
        private const string STORAGEACCOUNT_PROTOCOL = "https";// We use HTTPS to access the storage account
        private const string CONTAINER_NAME = "devicerules";// It's hard-coded for this workshop
        private const string BLOB_NAME = "devicerules.json";// It's hard-coded for this workshop
        private const double CUTOUTSPEED = 14f;
        private const double REPAIR = 0.5f;
        private readonly string storageAccountConnectionString;

        /* For Service Bus */
        private const string QueueName = "cloud2device";// It's hard-coded for this workshop
        private readonly string serviceBusConnectionString;

        public SettingsController()
        {
            string storageAccountName = ConfigurationManager.AppSettings["StorageAccount:Name"];
            string storageAccountKey = ConfigurationManager.AppSettings["StorageAccount:Key"];
            this.storageAccountConnectionString = CombineConnectionString(storageAccountName, storageAccountKey);

            this.serviceBusConnectionString = ConfigurationManager.AppSettings["ServiceBus:ConnectionString"];
        }

        private string CombineConnectionString(string storageAccountName, string storageAccountKey)
        {
            return "DefaultEndpointsProtocol=" + STORAGEACCOUNT_PROTOCOL + ";" +
                "AccountName=" + storageAccountName + ";" +
                "AccountKey=" + storageAccountKey;
        }

        [HttpGet]
        public ActionResult EnableWindTurbine(string deviceId, Boolean on)
        {
            System.Diagnostics.Debug.WriteLine("EnableWindTurbine deviceId={0}, on={1}", deviceId, on);

            AlarmMessage alarmMessage = new AlarmMessage();
            alarmMessage.ioTHubDeviceID = deviceId;
            alarmMessage.messageID = "";
            alarmMessage.alarmType = "EnableWindTurbine";
            alarmMessage.reading = on ? "1" : "0";
            alarmMessage.threshold = "";
            alarmMessage.localTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            alarmMessage.createdAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            var messageString = JsonConvert.SerializeObject(alarmMessage);
            SendAlarmMessageToServiceBusQueue(messageString);

            return File(Server.MapPath("/Views/html/") + "emptyresult.html", "text/html");
        }

        [HttpGet]
        public ActionResult ResetDepreciation(string deviceId)
        {
            System.Diagnostics.Debug.WriteLine("ResetDepreciation deviceId=" + deviceId);

            AlarmMessage alarmMessage = new AlarmMessage();
            alarmMessage.ioTHubDeviceID = deviceId;
            alarmMessage.messageID = "";
            alarmMessage.alarmType = "ResetDepreciation";
            alarmMessage.reading = "";
            alarmMessage.threshold = "";
            alarmMessage.localTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            alarmMessage.createdAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            var messageString = JsonConvert.SerializeObject(alarmMessage);
            SendAlarmMessageToServiceBusQueue(messageString);

            return File(Server.MapPath("/Views/html/") + "emptyresult.html", "text/html");
        }

        private void SendAlarmMessageToServiceBusQueue(string msg)
        {
            var client = QueueClient.CreateFromConnectionString(this.serviceBusConnectionString, QueueName);
            var message = new BrokeredMessage(msg);
            client.Send(message);
        }

        [HttpGet]
        public ActionResult ApplyDeviceRules(int cutOutSpeed, double depreciation)
        {
            System.Diagnostics.Debug.WriteLine("ApplyDeviceRules cutOutSpeed=" + cutOutSpeed + ", depreciation=" + depreciation);

            // Update the device rules of reference blob 
            UpdateReferenceBlob(cutOutSpeed, depreciation);

            return File(Server.MapPath("/Views/html/") + "emptyresult.html", "text/html");
        }

        private void UpdateReferenceBlob(int cutOutSpeed, double depreciation)
        {
            System.Diagnostics.Debug.WriteLine("StorageConnectionString={0}", this.storageAccountConnectionString);
            System.Diagnostics.Debug.WriteLine("ContainerName={0}", CONTAINER_NAME);

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(this.storageAccountConnectionString);

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container.
            CloudBlobContainer container = blobClient.GetContainerReference(CONTAINER_NAME);

            // Create the container if it doesn't already exist.
            container.CreateIfNotExists();

            CreateAndUploadBlob(container, GetBlobFileName(), cutOutSpeed, depreciation);
        }

        private static void CreateAndUploadBlob(CloudBlobContainer container, string blobName, int cutOutSpeed, double depreciation)
        {
            System.Diagnostics.Debug.WriteLine("container={0}, blobName={1}", container.Name, blobName);
            // Retrieve reference to a blob named "myblob".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
            String blobContent = CreateDeviceRules(cutOutSpeed, depreciation);
            System.Diagnostics.Debug.WriteLine("blobContent={0}", blobContent);

            byte[] content = ASCIIEncoding.ASCII.GetBytes(blobContent);
            blockBlob.UploadFromByteArrayAsync(content, 0, content.Count()).Wait();
            System.Diagnostics.Debug.WriteLine("upload successful content.Count()={0}", content.Count());
        }

        private static String CreateDeviceRules(int cutOutSpeed, double depreciation)
        {
            DeviceRule deviceRule1 = new DeviceRule();
            deviceRule1.DeviceID = "LinuxTurbine";
            deviceRule1.CutOutSpeed = cutOutSpeed;
            deviceRule1.Repair = depreciation;
            deviceRule1.Altitude = 239.6648864f;
            deviceRule1.Latitude = 25.037531f;
            deviceRule1.Longitude = 121.5639969f;

            DeviceRule deviceRule2 = new DeviceRule();
            deviceRule2.DeviceID = "WinTurbine";
            deviceRule2.CutOutSpeed = cutOutSpeed;
            deviceRule2.Repair = depreciation;
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
            DateTime saveDate = DateTime.UtcNow.AddSeconds(blobSaveSecondsInTheFuture);
            string dateString = saveDate.ToString("d", _formatInfo);
            string timeString = saveDate.ToString("t", _formatInfo);
            string blobName = string.Format(@"{0}\{1}\{2}", dateString, timeString, BLOB_NAME);

            return blobName;
        }
    }
}