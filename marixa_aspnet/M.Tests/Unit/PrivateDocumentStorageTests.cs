using M.API.Storage;
using M.Contract.Services.Storage;
using M.ModelViews.Files;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;

namespace M.Tests.Unit;

public sealed class PrivateDocumentStorageTests
{
    [Theory]
    [InlineData("uploads/handovers/test.pdf")]
    [InlineData("uploads//handovers/test.pdf")]
    public void Static_file_provider_blocks_legacy_private_documents_after_path_normalization(string path)
    {
        using var fixture = new StorageFixture();
        var privateDirectory = Path.Combine(fixture.Root, "uploads", "handovers");
        Directory.CreateDirectory(privateDirectory);
        File.WriteAllText(Path.Combine(privateDirectory, "test.pdf"), "private test data");
        File.WriteAllText(Path.Combine(fixture.Root, "public.txt"), "public test data");
        using var physical = new PhysicalFileProvider(fixture.Root);
        var provider = new PublicFilesProvider(physical, privateDirectory);
        Assert.False(provider.GetFileInfo(path).Exists);
        Assert.True(provider.GetFileInfo("public.txt").Exists);
    }

    [Fact]
    public async Task Private_storage_keeps_files_outside_web_root_and_preserves_existing_file_on_collision()
    {
        using var fixture = new StorageFixture();
        var content = new UploadedDocument("report.pdf", 3, () => new MemoryStream([1, 2, 3]));
        await fixture.Storage.SaveAsync(DocumentArea.EmployeeReports, null, "test.pdf", content);
        Assert.False(Directory.Exists(Path.Combine(fixture.Root, "wwwroot")));
        await Assert.ThrowsAsync<IOException>(() => fixture.Storage.SaveAsync(DocumentArea.EmployeeReports, null, "test.pdf", content));
        var download = fixture.Storage.Open(DocumentArea.EmployeeReports, null, "test.pdf", "report.pdf");
        using var stream = download.Content;
        Assert.Equal(3, stream.Length);
        Assert.Equal("application/pdf", download.ContentType);
    }

    [Fact]
    public async Task Private_storage_rejects_traversal_and_cleans_partial_file_when_length_is_wrong()
    {
        using var fixture = new StorageFixture();
        var content = new UploadedDocument("report.pdf", 1, () => new MemoryStream([1, 2, 3]));
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Storage.SaveAsync(DocumentArea.EmployeeReports, null, "../escape.pdf", content));
        await Assert.ThrowsAsync<M.Core.Base.BaseException.ErrorException>(() => fixture.Storage.SaveAsync(DocumentArea.EmployeeReports, null, "partial.pdf", content));
        Assert.Empty(Directory.GetFiles(fixture.Root, "*", SearchOption.AllDirectories));
    }

    private sealed class StorageFixture : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "marixa-private-storage-tests", Guid.NewGuid().ToString("N"));
        public LocalPrivateDocumentStorage Storage { get; }

        public StorageFixture()
        {
            Storage = new LocalPrivateDocumentStorage(new TestEnvironment { ContentRootPath = Root },
                NullLogger<LocalPrivateDocumentStorage>.Instance);
        }

        public void Dispose()
        {
            var testRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "marixa-private-storage-tests")) + Path.DirectorySeparatorChar;
            if (Path.GetFullPath(Root).StartsWith(testRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(Root))
                Directory.Delete(Root, true);
        }
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "M.Tests";
        public string EnvironmentName { get; set; } = "Testing";
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
