using System;
using System.IO;
using System.Reflection;
using System.Xml;

namespace common
{
    public class Settings
    {
        public bool _SettingsOK;
        public String _BucketName;
        public String _AccessKeyId;
        public String _SecretAccessKey;
    }

    public class xmlsettings
    {
        static public Settings ReadSettings()
        {
            Settings settings = new Settings();
            settings._SettingsOK = false;
            String tag = "";

            String fileName = Assembly.GetEntryAssembly().Location.ToLower();
            fileName = fileName.Replace(".exe", ".xml");
            if (File.Exists(fileName))
            {
                XmlTextReader reader = new XmlTextReader(fileName);
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            tag = reader.Name;
                            break;
                        case XmlNodeType.Text:
                            switch (tag)
                            {
                                case "bucketname":
                                    settings._BucketName = reader.Value;
                                    break;
                                case "access_key_id":
                                    settings._AccessKeyId = reader.Value;
                                    break;
                                case "secret_access_key":
                                    settings._SecretAccessKey = reader.Value;
                                    break;
                            }
                            break;
                    }
                } // while
                reader.Close();
                settings._SettingsOK = true;
            }

            return settings;
        }

        static public void ShowVersion()
        {
            String assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            Console.WriteLine("Textract Geeometry tool v" + assemblyVersion);
        }
    }
}
