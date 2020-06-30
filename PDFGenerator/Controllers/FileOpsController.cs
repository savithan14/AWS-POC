using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WkHtmlToPdf;
namespace PDFGenerator.Controllers
{
    [Route("api/[controller]")]

    [ApiController]
    public class FileOpsController : ControllerBase
    {

        #region Config
        private static string S3_AccessKey = "";
        private static string S3_SecretKey = "";
        private static RegionEndpoint S3_RegionEndpoint = RegionEndpoint.USEast1;
        private string bucketName = "ondemand-patientstatement";
        private string keyName = string.Empty;
        private string filePath = @"C:\Savitha\POC\junk1.html";
        #endregion

        // GET api/FileOps
        [HttpGet]
        public string Get()
        {
            keyName = GetFileName();
            return UploadFile();
        }
        private string GetFileName()
        {
            return @"savvytest_"+ DateTime.Now.DayOfYear + "/" + DateTime.Now.ToShortTimeString().Replace(" ", "").Replace(":", "") + ".html";
        }

        private string UploadFile()
        {
            var client = new AmazonS3Client(S3_AccessKey, S3_SecretKey, S3_RegionEndpoint);

            try
            {
                PutObjectRequest putRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = keyName,
                    ContentType = @"application/octet-stream",
                    //ContentBody = System.IO.File.ReadAllText(filePath),
                    InputStream = new MemoryStream(System.IO.File.ReadAllBytes(filePath)),
                    //AutoCloseStream = true,
                    CannedACL = S3CannedACL.PublicRead,
                    //StorageClass = S3StorageClass.ReducedRedundancy
                };

                var task = client.PutObjectAsync(putRequest);
                task.Wait();
                var response = task.Result;
                if(response.HttpStatusCode == HttpStatusCode.OK)
                {
                    string url = client.GetPreSignedURL(
                        new GetPreSignedUrlRequest() 
                        {
                           BucketName = bucketName, 
                            Key = keyName, 
                            Expires = DateTime.Now.AddMinutes(10)
                        });
                    return url;

                }
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null &&
                    (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId")
                    ||
                    amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    throw new Exception("Check the provided AWS Credentials.");
                }
                else
                {
                    throw new Exception("Error occurred: " + amazonS3Exception.Message);
                }
            }
            return string.Empty;
        }


    }
}