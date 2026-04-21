namespace LogicApps.Management.Repository.StorageAccount;

public interface IBlobStorageSender
{
    /// <summary>
    /// Asynchronously uploads the specified content to a blob storage container with the given file name.
    /// </summary>
    /// <param name="container">The name of the target blob storage container. Cannot be null or empty.</param>
    /// <param name="fileName">The name of the file to be created or overwritten in the container. Cannot be null or empty.</param>
    /// <param name="content">The HTTP content to upload as the blob. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous upload operation.</returns>
    /// <exception cref="HttpRequestException">Thrown if the upload operation fails due to an unsuccessful HTTP response.</exception>
    Task UploadAsync(string container, string fileName, HttpContent content);
}