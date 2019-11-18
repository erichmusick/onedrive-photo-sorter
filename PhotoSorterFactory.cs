using System;
using System.Threading.Tasks;
using ErichMusick.Tools.OneDrive.PhotoSorter.Controllers;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using Newtonsoft.Json;

namespace ErichMusick.Tools.OneDrive.PhotoSorter
{
    static class PhotoSorterFactory
    {
        internal static async Task<PhotoSorter> Create(bool runSampleRequest = false)
        {
            var config = AuthenticationConfig.ReadFromJsonFile("appsettings.json");

            string[] scopes = new string[]
            {
                "https://graph.microsoft.com/files.readwrite"
            };

            IPublicClientApplication PublicClientApp;
            PublicClientApp = PublicClientApplicationBuilder.Create(config.ClientId)
                .WithRedirectUri("http://localhost")
                .WithAuthority(AzureCloudInstance.AzurePublic, config.Tenant)
                .Build();

            InteractiveAuthenticationProvider authenticationProvider = new InteractiveAuthenticationProvider(PublicClientApp, scopes);
            GraphServiceClient graphServiceClient = new GraphServiceClient(authenticationProvider);

            // Verify
            if (runSampleRequest)
            {
                var me = await graphServiceClient.Me.Request().WithForceRefresh(true).GetAsync();
                Console.WriteLine("Me==>");
                Console.WriteLine(JsonConvert.SerializeObject(me));
                Console.WriteLine("<==");

                var drive = await graphServiceClient.Me.Drive.Root.Request().WithForceRefresh(true).GetAsync();
                Console.WriteLine("Drive==>");
                Console.WriteLine(JsonConvert.SerializeObject(drive));
                Console.WriteLine("<==");
            }

            var controller = new ItemsController(graphServiceClient);
            return new PhotoSorter(controller);
        }
    }
}