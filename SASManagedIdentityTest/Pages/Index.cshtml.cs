using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Logging;
using Azure.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage.Sas;
using Azure.Storage;

namespace SASManagedIdentityTest.Pages
{

    /// <summary>
    /// https://docs.microsoft.com/en-us/azure/app-service/overview-managed-identity?tabs=dotnet
    /// https://joonasw.net/view/azure-ad-authentication-with-azure-storage-and-managed-service-identity
    /// https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-user-delegation-sas-create-dotnet#get-a-user-delegation-sas-for-a-blob
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        public string UrlWithSas { get; set; }

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public async Task OnGet()
        {
            var azureCred = new DefaultAzureCredential();
            var blobServiceClient = new BlobServiceClient(new Uri("https://mitestsweisfel.blob.core.windows.net"), azureCred);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient("test");
            var blobClient = blobContainerClient.GetBlobClient("Blob Storage.svg");

            // Get a user delegation key for the Blob service that's valid for 7 days.
            // You can use the key to generate any number of shared access signatures 
            // over the lifetime of the key.
            var userDelegationKey =
                await blobServiceClient.GetUserDelegationKeyAsync(DateTimeOffset.UtcNow,
                                                                  DateTimeOffset.UtcNow.AddDays(7));

            // Create a SAS token that's also valid for 7 days.
            BlobSasBuilder sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = blobClient.BlobContainerName,
                BlobName = blobClient.Name,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddDays(7)
            };

            // Specify read and write permissions for the SAS.
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            // Add the SAS token to the blob URI.
            BlobUriBuilder blobUriBuilder = new BlobUriBuilder(blobClient.Uri)
            {
                // Specify the user delegation key.
                Sas = sasBuilder.ToSasQueryParameters(userDelegationKey,
                                                      blobServiceClient.AccountName)
            };

            UrlWithSas = blobUriBuilder.ToUri().ToString();

        }
    }
}
