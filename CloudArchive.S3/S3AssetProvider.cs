using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.IO;
using Amazon.S3.Model;
using CloudArchive.Configuration;
using Nalarium;
using Nalarium.Cryptography;
using Nalarium.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace CloudArchive.S3
{
    public class S3AssetProvider :
        IAssetReadProvider,
        IAssetWriteProvider
    {
        public async Task<AssetStatusCode> Check(string area, string selector, string hash)
        {
            if (string.IsNullOrEmpty(area))
            {
                return AssetStatusCode.Error;
            }

            var storageConfig = StorageConfig.Get(area);
            var credentials = new BasicAWSCredentials(storageConfig.Key1, storageConfig.Key2);

            area = area.ToLower(CultureInfo.CurrentCulture);

            using (var client = new AmazonS3Client(credentials, RegionEndpoint.USEast1))
            {
                var bucketName = storageConfig.Name;
                var key = area + "/" + selector;
                var request = new GetObjectMetadataRequest
                {
                    BucketName = bucketName,
                    Key = key
                };

                var fi = new S3FileInfo(client, bucketName, key);
                if (!fi.Exists)
                {
                    return AssetStatusCode.DoesNotExist;
                }

                var response = await client.GetObjectMetadataAsync(request);

                var storedHash = response.Metadata["x-amz-meta-content-md5"];
                if (string.IsNullOrEmpty(storedHash))
                {
                    return AssetStatusCode.DoesNotExist;
                }

                return storedHash.Equals(hash) ? AssetStatusCode.Same : AssetStatusCode.Different;
            }
        }

        public async Task<Tuple<string, Stream>> Stream(string area, string selector)
        {
            if (string.IsNullOrEmpty(area))
            {
                return null;
            }

            var storageConfig = StorageConfig.Get(area);
            var credentials = new BasicAWSCredentials(storageConfig.Key1, storageConfig.Key2);

            area = area.ToLower(CultureInfo.CurrentCulture);

            using (var client = new AmazonS3Client(credentials, RegionEndpoint.USEast1))
            {
                var request = new GetObjectRequest
                {
                    BucketName = storageConfig.Name,
                    Key = area + "/" + selector
                };

                using (var response = client.GetObject(request))
                {
                    return await Task.FromResult(new Tuple<string, Stream>(response.Headers["ContentType"], response.ResponseStream));
                }
            }
        }

        public async Task<byte[]> Read(string area, string selector)
        {
            if (string.IsNullOrEmpty(area))
            {
                return null;
            }

            var storageConfig = StorageConfig.Get(area);
            var credentials = new BasicAWSCredentials(storageConfig.Key1, storageConfig.Key2);

            area = area.ToLower(CultureInfo.CurrentCulture);

            using (var client = new AmazonS3Client(credentials, RegionEndpoint.USEast1))
            {
                var request = new GetObjectRequest
                {
                    BucketName = storageConfig.Name,
                    Key = area + "/" + selector
                };

                using (var response = client.GetObject(request))
                {
                    return await Task.FromResult(StreamConverter.GetStreamByteArray(response.ResponseStream));
                }
            }
        }

        public async Task<string> GetUrl(string area, string selector)
        {
            if (string.IsNullOrEmpty(area))
            {
                return null;
            }

            var storageConfig = StorageConfig.Get(area);
            var credentials = new BasicAWSCredentials(storageConfig.Key1, storageConfig.Key2);

            area = area.ToLower(CultureInfo.CurrentCulture);

            using (var client = new AmazonS3Client(credentials, RegionEndpoint.USEast1))
            {
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = storageConfig.Name,
                    Key = area + "/" + selector,
                    Expires = DateTime.Now.AddMinutes(30)
                };

                return await Task.FromResult(client.GetPreSignedURL(request));
            }
        }

        [Comment("S3 buckets are not like Azure containers; S3 buckets are like Azure accounts; thus, S3 doesn't use area")]
        public async Task EnsureAccess(string area)
        {
            var storageConfig = StorageConfig.Get(area);
            var credentials = new BasicAWSCredentials(storageConfig.Key1, storageConfig.Key2);

            using (var client = new AmazonS3Client(credentials, RegionEndpoint.USEast1))
            {
                var configuration = new CORSConfiguration
                {
                    Rules = new List<CORSRule>
                    {
                        new CORSRule
                        {
                            Id = "all",
                            AllowedMethods = new List<string> {"GET"},
                            AllowedOrigins = new List<string> {"*"},
                            MaxAgeSeconds = 3000
                        }
                    }
                };

                var request = new PutCORSConfigurationRequest
                {
                    BucketName = storageConfig.Name,
                    Configuration = configuration
                };

                await client.PutCORSConfigurationAsync(request);
            }
        }

        public async Task<string> Update(string area, string selector, string contentType, byte[] buffer)
        {
            if (string.IsNullOrEmpty(area))
            {
                return null;
            }

            var storageConfig = StorageConfig.Get(area);
            var credentials = new BasicAWSCredentials(storageConfig.Key1, storageConfig.Key2);

            area = area.ToLower(CultureInfo.CurrentCulture);

            var hash = QuickHash.Hash(buffer, HashMethod.SHA256);
            using (var client = new AmazonS3Client(credentials, RegionEndpoint.USEast1))
            {
                var putObjectRequest = new PutObjectRequest
                {
                    BucketName = storageConfig.Name,
                    Key = area + "/" + selector,
                    InputStream = new MemoryStream(buffer),
                    ContentType = contentType,
                    Metadata = { ["Content-MD5"] = hash },
                    CannedACL = S3CannedACL.PublicRead
                };

                await client.PutObjectAsync(putObjectRequest);
            }

            return hash;
        }

        public string CleanSelector(string selector)
        {
            return selector.Replace("/", "_").Replace(".", "=");
        }
    }
}
