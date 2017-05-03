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

    /*
     * Default Subs
     * 

    /r/gadgets/
    /r/sports/
    /r/gaming/
    /r/pics/
    /r/worldnews/
    /r/videos/
    /r/AskReddit/
    /r/aww/
    /r/Music/
    /r/funny/
    /r/news/
    /r/movies/
    /r/blog/
    /r/books/
    /r/history/
    /r/food/
    /r/philosophy/
    /r/television/
    /r/Jokes/
    /r/Art/
    /r/DIY/
    /r/space/
    /r/Documentaries/
    /r/askscience/
    /r/nottheonion/
    /r/todayilearned/
    /r/personalfinance/
    /r/gifs/
    /r/listentothis/
    /r/IAmA/
    /r/announcements/
    /r/TwoXChromosomes/
    /r/creepy/
    /r/nosleep/
    /r/GetMotivated/
    /r/WritingPrompts/
    /r/LifeProTips/
    /r/EarthPorn/
    /r/explainlikeimfive/
    /r/Showerthoughts/
    /r/Futurology/
    /r/photoshopbattles/
    /r/mildlyinteresting/
    /r/dataisbeautiful/
    /r/tifu/
    /r/OldSchoolCool/
    /r/UpliftingNews/
    /r/InternetIsBeautiful/
    /r/science/

    */

    public class RedditBot
    {
        RedditSession RedditSession;

        public RedditBot()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
            Configuration config;

            using (FileStream fileStream = new FileStream("config.xml", FileMode.Open))
            {
                config = (Configuration)serializer.Deserialize(fileStream);
            }

            RedditSession = new RedditSession(config);
            Start();
        }

        public void Start()
        {
            //RedditSession.SendMessage("-", "This is a test subject message", "This is a message body");
            while (true)
            {
                List<JsonCommentsRequestContentBaseDataComment> newComments = RedditSession.GetNewComments(100);
                foreach (JsonCommentsRequestContentBaseDataComment comment in newComments)
                {
                    Console.WriteLine(comment.data.created_utc + " - " + comment.data.name + " - " + comment.data.body);
                }
                Console.WriteLine("---");
                Console.ReadLine();
            }

            /*
            Console.ReadLine();
            */
        }
    }
}
