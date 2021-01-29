using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3.Transfer;

namespace DatabaseBackup
{
    class S3Worker
    {
        private static readonly RegionEndpoint bucketRegion = RegionEndpoint.USEast2;
        public string uploadProgress = "";
        public StringBuilder uploadStatusBuilder = new StringBuilder();
        public StringBuilder cleanUpStatusBuilder = new StringBuilder();
        public StringBuilder listObjectStatusBuilder = new StringBuilder();
        private string _awsS3AccessKeyID;
        private string _awsS3AccessKeySecret;
        private string _awsS3Region;
        private string _awsS3BucketName;
        private string _awsS3BucketFolder;
        private IAmazonS3 client;
        private int _backupFileCleanupAfterDays;

        public S3Worker()
        {
            _awsS3AccessKeyID = AppSettings.GetConfiguration<string>("awsS3AccessKeyID");
            _awsS3AccessKeySecret = AppSettings.GetConfiguration<string>("awsS3AccessKeySecret");
            _awsS3Region = AppSettings.GetConfiguration<string>("awsS3Region");
            _awsS3BucketName = AppSettings.GetConfiguration<string>("awsS3BucketName");
            _awsS3BucketFolder = AppSettings.GetConfiguration<string>("awsS3BucketFolder");
            _backupFileCleanupAfterDays = AppSettings.GetConfiguration<int>("backupFileCleanupAfterDays");
            client = new AmazonS3Client(_awsS3AccessKeyID, _awsS3AccessKeySecret, RegionEndpoint.GetBySystemName(_awsS3Region));
        }

        #region "Upload"
        public void Upload(string localFullFilePath, string fileName)
        {
            WritingAnObjectAsync(localFullFilePath, fileName).Wait();
        }

        private async Task WritingAnObjectAsync(string localFullFilePath, string fileName)
        {
            try
            {
                TransferUtility fileTransferUtility = new TransferUtility(client);

                TransferUtilityUploadRequest fileTransferUtilityRequest = new TransferUtilityUploadRequest
                {
                    BucketName = _awsS3BucketName,
                    FilePath = localFullFilePath,
                    Key = string.Format("{0}/{1}", _awsS3BucketFolder, fileName),
                };

                //subscribe the event to trace upload progress
                fileTransferUtilityRequest.UploadProgressEvent +=
                    new EventHandler<UploadProgressArgs>
                        (uploadRequest_UploadPartProgressEvent);

                await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);
            }
            catch (AmazonS3Exception e)
            {
                appendUploadProgress();
                
                uploadStatusBuilder.AppendLine(
                    string.Format("Error encountered ***. Message:'{0}' when writing an object", e.Message));
            }
            catch (Exception e)
            {
                appendUploadProgress();

                uploadStatusBuilder.AppendLine(
                    string.Format("Unknown Error encountered ***. Message:'{0}' when writing an object", e.Message));
            }
        }

        private void uploadRequest_UploadPartProgressEvent(object sender, UploadProgressArgs e)
        {
            // Process event.
            Console.Write("\rUploading file {0}: {1}%    ", ((TransferUtilityUploadRequest)sender).Key, Math.Round(((decimal)e.TransferredBytes / (decimal)e.TotalBytes) * 100, 2));
            if (Math.Round(((decimal)e.TransferredBytes / (decimal)e.TotalBytes) * 100, 2) == 100)
            {
                Console.WriteLine("");
            }
            uploadProgress = string.Format("Uploading file {0}: {1}%/100%", ((TransferUtilityUploadRequest)sender).Key, Math.Round(((decimal)e.TransferredBytes / (decimal)e.TotalBytes) * 100, 2));
        }

        private void appendUploadProgress()
        {
            if (!string.IsNullOrEmpty(uploadProgress))
            {
                uploadStatusBuilder.AppendLine(string.Format("Upload Progress: {0}", uploadProgress));
            }
        }

        #endregion

        #region "CleanUp"

        public void CleanUp()
        {
            List<S3Object> s3Backups = ListingObjectsAsync().Result;

            foreach (S3Object backupFile in s3Backups)
            {
                //clean up old backup files that last modified are too old, based configuration in appsettings.json
                if (backupFile.LastModified < (DateTime.Now.AddDays(-1 * _backupFileCleanupAfterDays)))
                {
                    ObjectDeleteAsync(backupFile.Key).Wait();
                }
            }
        }

        private async Task ObjectDeleteAsync(string s3FilePath)
        {
            try
            {
                cleanUpStatusBuilder.AppendLine(string.Format("S3 CleanUp - Deleting {0}", s3FilePath));
                var deleteObjectRequest = new DeleteObjectRequest
                {
                    BucketName = _awsS3BucketName,
                    Key = s3FilePath
                };

                //Console.WriteLine(string.Format("Deleting {0}", s3FilePath));
                await client.DeleteObjectAsync(deleteObjectRequest);
            }
            catch (AmazonS3Exception e)
            {
                cleanUpStatusBuilder.AppendLine(
                    string.Format("Error encountered ***. Message:'{0}' during clean up", e.Message));
            }
            catch (Exception e)
            {
                cleanUpStatusBuilder.AppendLine(
                    string.Format("Unknown Error encountered ***. Message:'{0}' during clean up", e.Message));
            }
        }
        #endregion

        #region "List Objects"
        public void ListS3Objects()
        {

            List<S3Object> s3Backups = ListingObjectsAsync().Result;

            foreach (S3Object s3Backup in s3Backups)
            {
                Console.WriteLine(s3Backup.Key);
            }

        }

        private async Task<List<S3Object>> ListingObjectsAsync()
        {
            List<S3Object> s3ObjectList = new List<S3Object>();
            try
            {
                ListObjectsV2Request request = new ListObjectsV2Request
                {
                    BucketName = _awsS3BucketName,
                    MaxKeys = 10,
                    Prefix = string.Format("{0}/", _awsS3BucketFolder)
                };
                ListObjectsV2Response response;
                do
                {
                    response = await client.ListObjectsV2Async(request);

                    s3ObjectList = response.S3Objects;

                } while (response.IsTruncated);
            }
            catch (AmazonS3Exception e)
            {
                listObjectStatusBuilder.AppendLine(
                    string.Format("Error encountered ***. Message:'{0}' during S3 object retrieval", e.Message));
            }
            catch (Exception e)
            {
                listObjectStatusBuilder.AppendLine(
                    string.Format("Unknown Error encountered ***. Message:'{0}' during S3 object retrieval", e.Message));
            }

            return s3ObjectList;
        }
        #endregion
    }
}
