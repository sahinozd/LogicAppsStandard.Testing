using System.Globalization;
using System.Text;

namespace LogicApps.Management.Repository.StorageAccount;

/// <summary>
/// Provides methods for constructing HTTP content for Azure Blob Storage requests.
/// </summary>
/// <remarks>This static class is intended to simplify the creation of properly formatted HTTP content for uploading blobs to Azure Blob Storage.
/// It sets required headers such as the blob type, content type, and version, ensuring compatibility with the Azure Blob Storage REST API.</remarks>
public static class BlobRequestBuilder
{
    /// <summary>
    /// Creates an HTTP content object for uploading a block blob with the specified payload and file name, including required Azure Blob Storage headers.
    /// </summary>
    /// <remarks>The returned content includes headers such as 'x-ms-date', 'x-ms-version', 'x-ms-blob-type', and 'x-ms-blob-content-type',
    /// which are required for Azure Blob Storage REST API operations. The MIME type is determined based on the provided file name.</remarks>
    /// <param name="payload">The string payload to include in the HTTP content body. Represents the data to be uploaded as the blob.</param>
    /// <param name="fileName">The name of the file to be associated with the blob. Used to determine the MIME type for the content.</param>
    /// <returns>An instance of HttpContent containing the payload and headers required for Azure Blob Storage block blob upload.</returns>
    public static HttpContent Build(string payload, string fileName)
    {
        var content = new StringContent(payload, Encoding.UTF8);

        content.Headers.Add("x-ms-date", DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture));
        content.Headers.Add("x-ms-version", "2023-11-03");
        content.Headers.Add("x-ms-blob-type", "BlockBlob");
        content.Headers.Add("x-ms-blob-content-type", MimeTypes.GetMimeType(fileName));

        return content;
    }
}