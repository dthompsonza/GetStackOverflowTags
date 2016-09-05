using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using GetTags.Models;
using Newtonsoft.Json;

namespace GetTags
{
    public class App
    {
        public GetState currentState;
        
        public App(string configFolder)
        {
            Log.Information("App started");

            string s = "";
            _tagFilepath = Path.Combine(configFolder, "tags.txt");
            _currentStateFilepath = Path.Combine(configFolder, "state.txt");

            if (File.Exists(_currentStateFilepath))
            {
                Log.Information("Current state file exists {CurrentStateFile}", _currentStateFilepath);
                string c = File.ReadAllText(_currentStateFilepath);
                currentState = JsonConvert.DeserializeObject<GetState>(c);
            }
            else
            {
                Log.Warning("Current state file does not exist {CurrentStateFile}", _currentStateFilepath);
                currentState = new GetState()
                {
                    currentPage = 0,
                    pageSize = 100,
                    has_more = true
                };
            }
            Log.Verbose("Current state is {CurrentState}", currentState);

            if (File.Exists(_tagFilepath))
            {
                Log.Information("Tag file exists {TagFile}", _tagFilepath);
                var tagData = File.ReadAllLines(_tagFilepath);
                _tags = new List<string>(tagData);
            }

        }

        public bool Run()
        {

            do
            {
                Console.WriteLine(currentState.quota_remaining);
                ResponsePayload response;

                try
                {
                    response = GetTags().Result;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Problem while getting tags from Stackoverflow");
                    return false;
                }

                Log.Information("Reponse was {@Response}", response);

                foreach (var item in response.items)
                {
                    if (!_tags.Contains(item.name))
                    {
                        _tags.Add(item.name);
                    }
                }

                try
                {
                    File.WriteAllLines(_tagFilepath, _tags);

                    string c = JsonConvert.SerializeObject(currentState);
                    File.WriteAllText(_currentStateFilepath, c);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to write tags to file");
                    return false;
                }

            } while (currentState.has_more && currentState.quota_remaining > 0);

            return true;
        }

        private async Task<ResponsePayload> GetTags()
        {
            var newState = currentState;
            newState.currentPage++;

            var uri = $"/2.2/tags?page={newState.currentPage}&pagesize={newState.pageSize}&order=asc&sort=name&site=stackoverflow";

            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            using (var client = new HttpClient(handler))
            {
                ServicePointManager.ServerCertificateValidationCallback = delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };

                client.BaseAddress = new Uri("https://api.stackexchange.com");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json; charset=utf-8");
                client.DefaultRequestHeaders.Add("X-Content-Type-Options", "nosniff");

                var response = await client.GetAsync(uri);
                var json = await response.Content.ReadAsStringAsync();
                var tags = JsonConvert.DeserializeObject<ResponsePayload>(json);

                newState.has_more = tags.has_more;
                newState.quota_remaining = tags.quota_remaining;
                newState.quota_max = tags.quota_max;

                currentState = newState;
                return tags;
            }
        }

        private readonly string _tagFilepath;
        private readonly string _currentStateFilepath;
        private List<string> _tags = new List<string>();

    }
}
