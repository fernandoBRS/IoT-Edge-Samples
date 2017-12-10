namespace FilterModule
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using FilterModule.Models;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Shared;
    using Newtonsoft.Json;

    public class Program
    {
        

        static void Main(string[] args)
        {
            InitializeDeviceClient();

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();

            AssemblyLoadContext.Default.Unloading += ctx => cts.Cancel();

            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();

            WhenCancelled(cts.Token).Wait(cts.Token);
        }

        /// <summary>
        /// Creates and configures a device client object
        /// </summary>
        private static void InitializeDeviceClient()
        {
            var connectionString = Environment.GetEnvironmentVariable("EdgeHubConnectionString");

            // Cert verification is not yet fully functional when using Windows OS for the container
            var bypassCertVerification = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            if (!bypassCertVerification)
            {
                InstallCert();
            }

            Init(connectionString, bypassCertVerification).Wait();
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);

            return tcs.Task;
        }

        /// <summary>
        /// Add certificate in local cert store for use by client for secure connection to IoT Edge runtime
        /// </summary>
        private static void InstallCert()
        {
            var certPath = Environment.GetEnvironmentVariable("EdgeModuleCACertificateFile");

            if (string.IsNullOrWhiteSpace(certPath))
            {
                // We cannot proceed further without a proper cert file
                Console.WriteLine($"Missing path to certificate collection file: {certPath}");
                throw new InvalidOperationException("Missing path to certificate file.");
            }

            if (!File.Exists(certPath))
            {
                // We cannot proceed further without a proper cert file
                Console.WriteLine($"Missing path to certificate collection file: {certPath}");
                throw new InvalidOperationException("Missing certificate file.");
            }

            // Storing a new certificate
            var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);

            store.Open(OpenFlags.ReadWrite);
            store.Add(new X509Certificate2(X509Certificate.CreateFromCertFile(certPath)));
            store.Close();

            Console.WriteLine("Added Cert: " + certPath);
        }
        
        /// <summary>
        /// Initializes the DeviceClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        private static async Task Init(string connectionString, bool bypassCertVerification = false)
        {
            Console.WriteLine("Connection String {0}", connectionString);

            var client = new ModuleClient(connectionString, bypassCertVerification);

            await client.StartClientAsync();
        }
    }
}
