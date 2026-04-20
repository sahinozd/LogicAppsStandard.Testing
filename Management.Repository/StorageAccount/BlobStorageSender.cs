using System.Net;

namespace LogicApps.Management.Repository.StorageAccount;

/// <summary>
/// Provides functionality to upload content to Azure Blob Storage containers using an underlying Azure management repository.
/// </summary>
/// <remarks>This class is intended for scenarios where blob storage uploads are managed via HTTP requests.
///  encapsulates error handling for common HTTP status codes returned by the storage service.</remarks>
/// <param name="repository">The Azure management repository used to perform HTTP operations against blob storage. Cannot be null.</param>
public class BlobStorageSender(IAzureManagementRepository repository) : IBlobStorageSender
{
    /// <summary>
    /// Asynchronously uploads the specified content to a blob storage container with the given file name.
    /// </summary>
    /// <param name="container">The name of the target blob storage container. Cannot be null or empty.</param>
    /// <param name="fileName">The name of the file to be created or overwritten in the container. Cannot be null or empty.</param>
    /// <param name="content">The HTTP content to upload as the blob. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous upload operation.</returns>
    /// <exception cref="HttpRequestException">Thrown if the upload operation fails due to an unsuccessful HTTP response.</exception>
    public async Task UploadAsync(string container, string fileName, HttpContent content)
    {
        var path = new Uri($"{container}/{fileName}", UriKind.Relative);
        var result = await repository.PutAsync(path, content).ConfigureAwait(false);

        if (!result.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to upload blob. Status {(int)result.StatusCode}: {result.ReasonPhrase}. {GetErrorMessage(result.StatusCode)}");
        }
    }

    /// <summary>
    /// Returns a user-friendly error message corresponding to the specified HTTP status code.
    /// </summary>
    /// <param name="statusCode">The HTTP status code for which to retrieve the error message.</param>
    /// <returns>A string containing a descriptive error message for the given status code. Returns a generic message if the status code is not recognized.</returns>
    private static string GetErrorMessage(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Created => "Payload successfully sent to blob container.",
        HttpStatusCode.BadRequest => "Bad request.",
        HttpStatusCode.Unauthorized => "Authorization failure.",
        HttpStatusCode.Forbidden => "Not authorized to overwrite blob.",
        HttpStatusCode.Conflict => "Blob already exists or conflict occurred.",
        HttpStatusCode.InternalServerError => "Internal error.",
        _ => "Unexpected error."
    };
}