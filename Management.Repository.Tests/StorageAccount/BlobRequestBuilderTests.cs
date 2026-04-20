using LogicApps.Management.Repository.StorageAccount;
using NUnit.Framework;

namespace LogicApps.Management.Repository.Tests.StorageAccount;

[TestFixture]
internal sealed class BlobRequestBuilderTests
{
    [Test]
    public void Build_Should_Create_HttpContent_With_Payload()
    {
        // Arrange
        const string payload = "payload content";
        const string fileName = "file.json";

        // Act
        var content = BlobRequestBuilder.Build(payload, fileName);

        // Assert
        Assert.That(content, Is.Not.Null);
        var contentString = content.ReadAsStringAsync().Result;
        Assert.That(contentString, Is.EqualTo(payload));
    }

    [Test]
    public void Build_Should_Add_XMsDate_Header()
    {
        // Arrange
        const string payload = "payload content";
        const string fileName = "file.txt";

        // Act
        var content = BlobRequestBuilder.Build(payload, fileName);

        // Assert
        var headers = content.Headers.GetValues("x-ms-date");
        var headersList = headers.ToList();
        Assert.That(headersList, Is.Not.Null);
        Assert.That(headersList, Has.Exactly(1).Items);
    }

    [Test]
    public void Build_Should_Add_XMsVersion_Header_With_Correct_Value()
    {
        // Arrange
        const string payload = "payload content";
        const string fileName = "file.txt";

        // Act
        var content = BlobRequestBuilder.Build(payload, fileName);

        // Assert
        var version = content.Headers.GetValues("x-ms-version").FirstOrDefault();
        Assert.That(version, Is.EqualTo("2023-11-03"));
    }

    [Test]
    public void Build_Should_Add_XMsBlobType_Header_As_BlockBlob()
    {
        // Arrange
        const string payload = "payload content";
        const string fileName = "file.txt";

        // Act
        var content = BlobRequestBuilder.Build(payload, fileName);

        // Assert
        var blobType = content.Headers.GetValues("x-ms-blob-type").FirstOrDefault();
        Assert.That(blobType, Is.EqualTo("BlockBlob"));
    }

    [Test]
    public void Build_Should_Add_XMsBlobContentType_Header()
    {
        // Arrange
        const string payload = "payload content";
        const string fileName = "file.json";

        // Act
        var content = BlobRequestBuilder.Build(payload, fileName);

        // Assert
        var headers = content.Headers.GetValues("x-ms-blob-content-type");
        var headersList = headers.ToList();
        Assert.That(headersList, Is.Not.Null);
        Assert.That(headersList, Has.Exactly(1).Items);
    }

    [Test]
    public void Build_Should_Set_ContentType_To_TextPlain_With_UTF8_Encoding()
    {
        // Arrange
        const string payload = "payload content";
        const string fileName = "file.txt";

        // Act
        var content = BlobRequestBuilder.Build(payload, fileName);

        // Assert
        Assert.That(content.Headers.ContentType, Is.Not.Null);
        Assert.That(content.Headers.ContentType!.MediaType, Does.Contain("text/plain"));
        Assert.That(content.Headers.ContentType.CharSet, Is.EqualTo("utf-8"));
    }

    [Test]
    public void Build_Should_Handle_Empty_Payload()
    {
        // Arrange
        const string payload = "";
        const string fileName = "empty.txt";

        // Act
        var content = BlobRequestBuilder.Build(payload, fileName);

        // Assert
        Assert.That(content, Is.Not.Null);
        var contentString = content.ReadAsStringAsync().Result;
        Assert.That(contentString, Is.Empty);
    }

    [Test]
    public void Build_Should_Create_Different_Content_For_Different_FileNames()
    {
        // Arrange
        const string payload = "same content";
        const string fileName1 = "file1.json";
        const string fileName2 = "file2.xml";

        // Act
        var content1 = BlobRequestBuilder.Build(payload, fileName1);
        var content2 = BlobRequestBuilder.Build(payload, fileName2);

        // Assert
        var mimeType1 = content1.Headers.GetValues("x-ms-blob-content-type").FirstOrDefault();
        var mimeType2 = content2.Headers.GetValues("x-ms-blob-content-type").FirstOrDefault();

        // MIME types should be determined by file extension
        Assert.That(mimeType1, Is.Not.EqualTo(mimeType2));
    }

    [Test]
    public void Build_Should_Include_All_Required_Headers()
    {
        // Arrange
        const string payload = "test";
        const string fileName = "test.txt";

        // Act
        var content = BlobRequestBuilder.Build(payload, fileName);

        // Assert
        var headerNames = content.Headers.Select(h => h.Key).ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(headerNames, Does.Contain("x-ms-date"));
            Assert.That(headerNames, Does.Contain("x-ms-version"));
            Assert.That(headerNames, Does.Contain("x-ms-blob-type"));
            Assert.That(headerNames, Does.Contain("x-ms-blob-content-type"));
        }
    }

    [Test]
    public void Build_Should_Handle_Large_Payload()
    {
        // Arrange
        var largePayload = new string('x', 10000);
        const string fileName = "large.txt";

        // Act
        var content = BlobRequestBuilder.Build(largePayload, fileName);

        // Assert
        Assert.That(content, Is.Not.Null);
        var contentString = content.ReadAsStringAsync().Result;
        Assert.That(contentString.Length, Is.EqualTo(10000));
    }

    [Test]
    public void Build_Should_Handle_Special_Characters_In_Payload()
    {
        // Arrange
        const string payload = "Special chars: @#$%^&*(){}[]|\\<>?/~`";
        const string fileName = "special.txt";

        // Act
        var content = BlobRequestBuilder.Build(payload, fileName);

        // Assert
        var contentString = content.ReadAsStringAsync().Result;
        Assert.That(contentString, Is.EqualTo(payload));
    }

    [Test]
    public void Build_Should_Handle_Unicode_Characters_In_Payload()
    {
        // Arrange
        const string payload = "Unicode: 你好 مرحبا здравствуйте";
        const string fileName = "unicode.txt";

        // Act
        var content = BlobRequestBuilder.Build(payload, fileName);

        // Assert
        var contentString = content.ReadAsStringAsync().Result;
        Assert.That(contentString, Is.EqualTo(payload));
    }
}