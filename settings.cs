using System;
using System.Configuration;

namespace common
{
    public class Settings
    {
        public String _AccessKeyId;
        public String _SecretAccessKey;
        public String _BucketName;

        public Settings()
        {
            _AccessKeyId = ConfigurationManager.AppSettings.Get("access_key_id");
            _SecretAccessKey = ConfigurationManager.AppSettings.Get("secret_access_key");
            _BucketName = ConfigurationManager.AppSettings.Get("bucketname");
        }
    }
}
