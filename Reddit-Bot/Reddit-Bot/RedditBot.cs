using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

namespace Reddit_Bot
{
    [XmlRoot("Configuration")]
    public class Configuration
    {
        [XmlElement("UserAccount")]
        public UserAccount UserAccount;
        [XmlElement("AppDetails")]
        public AppDetails AppDetails;
    }

    public class UserAccount
    {
        [XmlElement("Username")]
        public string Username;

        [XmlElement("Password")]
        public string Password;
    }

    public class AppDetails
    {
        [XmlElement("UserAgent")]
        public string UserAgent;

        [XmlElement("Id")]
        public string Id;

        [XmlElement("Secret")]
        public string Secret;
    }

    public class RedditBot
    {
        public RedditBot()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
            Configuration config;

            using (FileStream fileStream = new FileStream("config.xml", FileMode.Open))
            {
                config = (Configuration)serializer.Deserialize(fileStream);
            }

            RedditSession RedditSession = new RedditSession(config);
            RedditSession.Authenticate();
            RedditSession.SendMessage("-", "This is a test subject message", "This is a message body");
        }
    }
}
