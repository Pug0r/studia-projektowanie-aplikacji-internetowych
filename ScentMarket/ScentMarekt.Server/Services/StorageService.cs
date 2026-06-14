using Minio;
using Minio.DataModel.Args;

namespace ScentMarekt.Server.Services;

/// <summary>
/// Wraps the MinIO client. Handles bucket bootstrapping and object uploads.
/// </summary>
public sealed class StorageService
{
    private readonly IMinioClient _minio;
    private readonly string _bucket;
    private readonly string _publicEndpoint;

    public StorageService(IConfiguration config)
    {
        var endpoint      = config["Minio:Endpoint"]!;
        var accessKey     = config["Minio:AccessKey"]!;
        var secretKey     = config["Minio:SecretKey"]!;
        _bucket           = config["Minio:BucketName"]!;
        _publicEndpoint   = config["Minio:PublicEndpoint"]!.TrimEnd('/');

        _minio = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(false)
            .Build();
    }

    /// <summary>
    /// Ensures the configured bucket exists and is publicly readable.
    /// Safe to call multiple times (idempotent).
    /// </summary>
    public async Task EnsureBucketAsync()
    {
        var exists = await _minio.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_bucket));

        if (!exists)
        {
            await _minio.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_bucket));

            // Allow anonymous GET on all objects — browsers can load images directly
            var publicReadPolicy = $$"""
                {
                  "Version": "2012-10-17",
                  "Statement": [{
                    "Effect": "Allow",
                    "Principal": {"AWS": ["*"]},
                    "Action": ["s3:GetObject"],
                    "Resource": ["arn:aws:s3:::{{_bucket}}/*"]
                  }]
                }
                """;

            await _minio.SetPolicyAsync(
                new SetPolicyArgs().WithBucket(_bucket).WithPolicy(publicReadPolicy));
        }
    }

    /// <summary>
    /// Uploads raw bytes to the bucket under <paramref name="objectName"/>.
    /// Returns the public browser-accessible URL.
    /// </summary>
    public async Task<string> UploadAsync(
        string objectName,
        byte[] data,
        string contentType = "image/svg+xml")
    {
        using var stream = new MemoryStream(data);

        await _minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucket)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(data.Length)
            .WithContentType(contentType));

        return $"{_publicEndpoint}/{_bucket}/{objectName}";
    }
}
