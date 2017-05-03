using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using Newtonsoft.Json;

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
        private RestClient RedditClient;
        private RestClient OauthRestClient;

        private List<JsonCommentsRequestContentBaseDataComment> TrackedComments;
        private double LastScannedUTCValue;

        public RedditSession(Configuration config)
        {
            Config = config;
            LastScannedUTCValue = 0;
            TrackedComments = new List<JsonCommentsRequestContentBaseDataComment>();

            RedditClient = new RestClient("https://www.reddit.com");
            RedditClient.Authenticator = new RestSharp.Authenticators.HttpBasicAuthenticator(Config.AppDetails.Id, Config.AppDetails.Secret);

            OauthRestClient = new RestClient("https://oauth.reddit.com");

            Authenticate();
        }

        public void Authenticate()
        {
            var request = new RestRequest(Method.POST);
            request.AddHeader("User-Agent", Config.AppDetails.UserAgent);
            request.Resource = "api/v1/access_token";
            request.AddParameter("grant_type", "password");
            request.AddParameter("username", Config.UserAccount.Username);
            request.AddParameter("password", Config.UserAccount.Password);

            var intialTokenResponse = RedditClient.Execute<AccessTokenResponse>(request);
            AccessTokenResponse tokenResponse = intialTokenResponse.Data;

            RedditAccessToken = new AccessToken(tokenResponse.AccessToken);
        }

        public List<JsonCommentsRequestContentBaseDataComment> GetNewComments(int maxComments = 100)
        {
            // At any point we can't guarantee we've seen every comment in the second in which we request the data
            // Therefore we need to re-request that second of comments
            // Therefore we need to track the comments we have already scanned for that second to remove them

            List<JsonCommentsRequestContentBaseDataComment> untrackedComments = new List<JsonCommentsRequestContentBaseDataComment>();
            
            bool caughtUp = false;
            string after = "";

            while (!caughtUp && untrackedComments.Count < maxComments)
            {
                JsonCommentsRequestContentBase jsonDataRequest = RequestComments(2, after);
                List<JsonCommentsRequestContentBaseDataComment> strippedJsonData = StripProcessedComments(jsonDataRequest);

                if (strippedJsonData.Count + untrackedComments.Count > maxComments)
                {
                    strippedJsonData = strippedJsonData.GetRange(0, maxComments - untrackedComments.Count);
                }
                untrackedComments.AddRange(strippedJsonData);

                after = jsonDataRequest.data.after;

                if (strippedJsonData.Count != jsonDataRequest.data.children.Count || strippedJsonData.Count == 0 || after == null)
                {
                    // We have stripped some comments, so we have reached comments we have already scanned
                    // No more requests are required
                    caughtUp = true;
                }

                //Console.WriteLine("Found " + untrackedComments.Count + " total new comments");
                //Console.WriteLine("Found " + (jsonDataRequest.data.children.Count - strippedJsonData.Count) + " comments already tracked in this scan");
            }

            TrackedComments.InsertRange(0,untrackedComments);
            if (untrackedComments.Count > 0)
            {
                LastScannedUTCValue = untrackedComments[0].data.created_utc;
            }
            TrimTrackedComments();

            return untrackedComments;
        }

        public List<JsonCommentsRequestContentBaseDataComment> StripProcessedComments(JsonCommentsRequestContentBase jsonData)
        {
            List<JsonCommentsRequestContentBaseDataComment> unstrippedComments = new List<JsonCommentsRequestContentBaseDataComment>();
            for (int i = 0; i < jsonData.data.children.Count; i++)
            {
                if (!jsonData.data.children[i].isOlderThan(LastScannedUTCValue))
                {
                    // Not older than our last tracked comment
                    // Check if already scanned

                    bool alreadyScanned = false;

                    for (int x = 0; x < TrackedComments.Count; x++)
                    {
                        if (jsonData.data.children[i].Equals(TrackedComments[x]))
                        {
                            alreadyScanned = true;
                        }
                    }

                    if (!alreadyScanned)
                    {
                        unstrippedComments.Add(jsonData.data.children[i]);
                    }
                }
            }
            return unstrippedComments;
        }

        public void TrimTrackedComments()
        {
            for (int i = 0; i < TrackedComments.Count;)
            {
                if (TrackedComments[i].isOlderThan(LastScannedUTCValue))
                {
                    TrackedComments.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        /* API Calls */
        
        public void CheckToken()
        {
            if (!RedditAccessToken.IsValid || true)
            {
                Authenticate();
            }
        }

        public JsonCommentsRequestContentBase CallAPI(Method protocol, List<KeyValuePair<string, string>> headers, string resource, List<KeyValuePair<string, string>> parameters)
        {
            CheckToken();

            var request = new RestRequest(protocol);

            foreach (KeyValuePair<string, string> header in headers)
            {
                request.AddHeader(header.Key, header.Value);
            }

            request.Resource = resource;

            foreach (KeyValuePair<string, string> parameter in parameters)
            {
                request.AddParameter(parameter.Key, parameter.Value);
            }

            var response = OauthRestClient.Execute(request);
            JsonCommentsRequestContentBase jsonDecoded = JsonConvert.DeserializeObject<JsonCommentsRequestContentBase>(response.Content);

            return jsonDecoded;
        }

        public void SendMessage(string username, string subject, string message)
        {
            List<KeyValuePair<string, string>> headers = new List<KeyValuePair<string, string>>();
            List<KeyValuePair<string, string>> parameters = new List<KeyValuePair<string, string>>();

            Method protocol = Method.POST;
            headers.Add(new KeyValuePair<string, string>("User-Agent", Config.AppDetails.UserAgent));
            headers.Add(new KeyValuePair<string, string>("Authorization", "bearer " + RedditAccessToken.Token));
            string resource = "api/compose";
            parameters.Add(new KeyValuePair<string, string>("to", username));
            parameters.Add(new KeyValuePair<string, string>("subject", subject));
            parameters.Add(new KeyValuePair<string, string>("text", message));

            CallAPI(protocol, headers, resource, parameters);
        }

        public JsonCommentsRequestContentBase RequestComments(int limit = -1, string after = "")
        {
            string resourceParameters = "";

            if (limit != -1)
            {
                resourceParameters += "limit=" + limit + "&";
            }
            if (after != "")
            {
                resourceParameters += "after=" + after + "&";
            }

            List<KeyValuePair<string, string>> headers = new List<KeyValuePair<string, string>>();
            List<KeyValuePair<string, string>> parameters = new List<KeyValuePair<string, string>>();

            Method protocol = Method.GET;
            headers.Add(new KeyValuePair<string, string>("User-Agent", Config.AppDetails.UserAgent));
            headers.Add(new KeyValuePair<string, string>("Authorization", "bearer " + RedditAccessToken.Token));
            string resource = "comments?" + parameters;

            JsonCommentsRequestContentBase jsonDecoded = CallAPI(protocol, headers, resource, parameters);
            return jsonDecoded;
        }
    }
}
