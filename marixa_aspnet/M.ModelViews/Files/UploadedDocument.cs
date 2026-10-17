namespace M.ModelViews.Files;

/// <summary>File metadata and a lazily opened stream, independent of ASP.NET model binding.</summary>
public sealed record UploadedDocument(string FileName, long Length, Func<Stream> OpenReadStream);
