using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;

namespace Reddit_Bot
{
    public class AccessTokenResponse
    {
        public string AccessToken { get; set; }
    }

    public class AccessToken
    {
        public string Token;
        public bool IsValid
        {
            get
            {
                return DateTime.UtcNow.Subtract(TimeOfIssue).TotalSeconds < 3600;
            }
        }

        private DateTime TimeOfIssue;

        public AccessToken(string token)
        {
            Token = token;
            TimeOfIssue = DateTime.UtcNow;
        }
    }

    public class RedditSession
    {
        public Configuration Config;

        private AccessToken RedditAccessToken;
        private RestClient OauthRestClient;

        public RedditSession(Configuration config)
        {
            Config = config;
        }

        public void Authenticate()
        {
            var client = new RestClient("https://www.reddit.com");
            client.Authenticator = new RestSharp.Authenticators.HttpBasicAuthenticator(Config.AppDetails.Id, Config.AppDetails.Secret);

            var request = new RestRequest(Method.POST);
            request.AddHeader("User-Agent", Config.AppDetails.UserAgent);
            request.Resource = "api/v1/access_token";
            request.AddParameter("grant_type", "password");
            request.AddParameter("username", Config.UserAccount.Username);
            request.AddParameter("password", Config.UserAccount.Password);

            var intialTokenResponse = client.Execute<AccessTokenResponse>(request);
            AccessTokenResponse tokenResponse = intialTokenResponse.Data;

            RedditAccessToken = new AccessToken(tokenResponse.AccessToken);

            OauthRestClient = new RestClient("https://oauth.reddit.com");
        }

        public void SendMessage(string username, string subject, string message)
        {
            var request = new RestRequest(Method.POST);
            request.AddHeader("User-Agent", Config.AppDetails.UserAgent);
            request.AddHeader("Authorization", "bearer " + RedditAccessToken);
            request.Resource = "api/compose";
            request.AddParameter("to", username);
            request.AddParameter("subject", subject);
            request.AddParameter("text", message);

            var response = OauthRestClient.Execute(request);
        }
    }
}
