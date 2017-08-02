using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Text;
using System.IO;

namespace Consumer
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        static CloudQueue queueNotification;

        public static void ConnectToQueue()
        {
            var connectionString = "DefaultEndpointsProtocol=https;AccountName=acmestorageaccount;AccountKey=S5qfkwPaILx8dj2RGSreusmN+peF2S9mxriTmM51mXBLiemaWwb7nRvqdBMkffAeV5EXlAMRA3/7E0bY/OFhxg==;EndpointSuffix=core.windows.net";
            CloudStorageAccount cloudStorageAccount;

            if (!CloudStorageAccount.TryParse(connectionString, out cloudStorageAccount))
            {
                Console.WriteLine("Expected connection string 'Azure Storage Account to be a valid Azure" +
                    " Storage Connection  String'");
            }

            var cloudQueueClient = cloudStorageAccount.CreateCloudQueueClient();
            queueNotification = cloudQueueClient.GetQueueReference("queuenotification");

            queueNotification.CreateIfNotExists();
        }

        public void GetMessageFromQueue()
        {
            CloudQueueMessage cloudQueueMessage = queueNotification.GetMessage();
            if (cloudQueueMessage == null)
            {
                return;
            }

            Trace.TraceInformation("Get message from Queue and make the push notification");
            String message = cloudQueueMessage.AsString;

            MessageQueue messageQueue = Newtonsoft.Json.JsonConvert.DeserializeObject<MessageQueue>(message);
            String userToken = messageQueue.user.token.Trim();
            if (null != userToken && userToken.Length > 0) {
                var postData = "{\"data\": " + message + ", \"to\":\"" + messageQueue.user.token + "\"}";
                var request = (HttpWebRequest)WebRequest.Create("https://gcm-http.googleapis.com/gcm/send");
                request.Method = "POST";
                request.ContentLength = postData.Length;
                request.ContentType = "application/json";
                request.PreAuthenticate = true;
                request.Headers.Add("Authorization", "key=AAAAMBsEGmw:APA91bHLRhf7yNfbcBcskn-BJm0lqgOqzdpHTONEhM5l7zki1Mjn2mxpWmFkkir6NC2CDsBQkh3-u7prv-to433N0gA5UepcWnDTK1J6uz1EaAJhQzujPmcJ5VMn-ZsQ0u-yJQ1VnhK2");

                var data = Encoding.ASCII.GetBytes(postData);
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                var response = (HttpWebResponse)request.GetResponse();
                //var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    queueNotification.DeleteMessage(cloudQueueMessage);
                }
            }
        }

        public override void Run()
        {
            Trace.TraceInformation("Consumer is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Definir o número máximo de conexões simultâneas
            ServicePointManager.DefaultConnectionLimit = 12;

            // Para obter informações sobre como tratar as alterações de configuração
            // veja o tópico do MSDN em https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("Consumer has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("Consumer is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("Consumer has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: substitua o item a seguir pela sua própria lógica.
            ConnectToQueue();
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");

                GetMessageFromQueue();

                await Task.Delay(1000);
            }
        }
    }
}
