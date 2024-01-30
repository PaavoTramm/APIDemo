using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;

namespace ApiDemo
{
    internal class DocumentAPI
    {
        public DocumentAPI(string url, string username, string password)
        {
            Url = url;
            UserName = username;
            Password = password;
        }

        public async Task<bool> Authenticate()
        {
            if (null == auth || auth.access_token_expires_at < DateTime.UtcNow.AddMinutes(5))
            {
                auth = null;

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "authenticate");
                request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{UserName}:{Password}")));

                HttpResponseMessage res = await Client.SendAsync(request);
                if (res.StatusCode != System.Net.HttpStatusCode.OK)
                    return false;

                string data = await res.Content.ReadAsStringAsync();

                auth = JsonSerializer.Deserialize<AuthVal?>(data);

                if (auth?.access_token == null)
                    return false;

                Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth?.access_token);
            }
            return true;
        }

        public async Task<bool> UnAuthenticate()
        {
            if (null == auth || auth.access_token_expires_at > DateTime.UtcNow)
                return true;

            HttpResponseMessage res = await Client.DeleteAsync("authenticate");
            if (!res.IsSuccessStatusCode) // System.Net.HttpStatusCode.NoContent
                return false;

            return true;
        }

        public async Task<Info?> GetInfo()
        {
            bool connected = await Authenticate();
            if (!connected)
                return null;

            HttpResponseMessage res = await Client.GetAsync("");
            if (res == null)
                return null;

            string data = await res.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<Info?>(data);
        }

        public async Task<List<Service>?> GetServices()
        {
            bool connected = await Authenticate();
            if (!connected)
                return null;

            HttpResponseMessage res = await Client.GetAsync("services");
            if (res == null)
                return null;

            if (!res.IsSuccessStatusCode) // System.Net.HttpStatusCode.OK
                return null;

            string data = await res.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<List<Service>>(data);
        }

        public async Task<Service?> GetService(string service)
        {
            bool connected = await Authenticate();
            if (!connected)
                return null;

            HttpResponseMessage res = await Client.GetAsync($"services/{service}");
            if (res == null)
                return null;

            if (!res.IsSuccessStatusCode) // System.Net.HttpStatusCode.OK
                return null;

            string data = await res.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<Service>(data);
        }

        public async Task<Created?> CreateService(string name)
        {
            bool connected = await Authenticate();
            if (!connected)
                return null;

            Service record = new Service()
            {
                type = "merge",
                shape = name,
                active = true,
                version = "1.0"
            };

            var content = new StringContent(JsonSerializer.Serialize(record), Encoding.UTF8, "application/json");

            HttpResponseMessage res = await Client.PostAsync($"services", content);
            if (res == null)
                return null;

            if (!res.IsSuccessStatusCode) // System.Net.HttpStatusCode.Created
                return null;

            string data = await res.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<Created?>(data);
        }

        public async Task<bool> DeleteService(string service)
        {
            bool connected = await Authenticate();
            if (!connected)
                return false;

            HttpResponseMessage res = await Client.DeleteAsync($"services/{service}");

            if (!res.IsSuccessStatusCode) // System.Net.HttpStatusCode.NoContent
                return false;

            return true;
        }

        public async Task<List<Resource>?> GetResources(string service)
        {
            bool connected = await Authenticate();
            if (!connected)
                return null;

            HttpResponseMessage res = await Client.GetAsync($"services/{service}/resources");
            if (res == null)
                return null;

            if (!res.IsSuccessStatusCode) // System.Net.HttpStatusCode.OK
                return null;

            string data = await res.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<List<Resource>>(data);
        }

        public async Task<bool> SendResource(string service, string file, string contenttype)
        {
            bool connected = await Authenticate();
            if (!connected)
                return false;

            HttpContent content = new ByteArrayContent(System.IO.File.ReadAllBytes(file));
            content.Headers.Add("Content-Type", contenttype);
            content.Headers.Add("Content-Disposition", $"attachment; filename=\"{System.IO.Path.GetFileName(file)}\"");

            string uri = $"services/{service}/resources";

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Content = content;

            HttpResponseMessage res = await Client.SendAsync(request);
            if (!res.IsSuccessStatusCode) // Created
                return false;

            return true;
        }

        public async Task<bool> UpdateResource(string service, string resource, string file, string contenttype)
        {
            bool connected = await Authenticate();
            if (!connected)
                return false;

            HttpContent content = new ByteArrayContent(System.IO.File.ReadAllBytes(file));
            content.Headers.Add("Content-Type", contenttype);
            content.Headers.Add("Content-Disposition", $"attachment; filename=\"{System.IO.Path.GetFileName(file)}\"");

            string uri = $"services/{service}/resources/{resource}";

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, uri);
            request.Content = content;

            HttpResponseMessage res = await Client.SendAsync(request);
            if (!res.IsSuccessStatusCode) // NoContent
                return false;

            return true;
        }

        public async Task<bool> DeleteResource(string service, string resource)
        {
            bool connected = await Authenticate();
            if (!connected)
                return false;

            HttpResponseMessage res = await Client.DeleteAsync($"services/{service}/resources/{resource}");
            if (!res.IsSuccessStatusCode)
                return false;

            return true;
        }

        public async Task<Created?> CreateJob(string service, string name, string contenttype)
        {
            bool connected = await Authenticate();
            if (!connected)
                return null;

            JobInput record = new JobInput()
            {
                type = "siso",
                active = true,
                output = new JobOutput() 
                { 
                    name = name, 
                    content_type = contenttype 
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(record), Encoding.UTF8, "application/json");

            HttpResponseMessage res = await Client.PostAsync($"services/{service}/jobs", content);
            if (res == null)
                return null;

            if (!res.IsSuccessStatusCode) // System.Net.HttpStatusCode.Created
                return null;

            string data = await res.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<Created?>(data);
        }

        public async Task<List<Job>?> GetJobs(string service)
        {
            bool connected = await Authenticate();
            if (!connected)
                return null;

            HttpResponseMessage res = await Client.GetAsync($"services/{service}/jobs");
            if (res == null)
                return null;

            if (!res.IsSuccessStatusCode) // System.Net.HttpStatusCode.OK
                return null;

            string data = await res.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<List<Job>>(data);
        }

        public async Task<Job?> ReadJob(string service, string job)
        {
            bool connected = await Authenticate();
            if (!connected)
                return null;

            HttpResponseMessage res = await Client.GetAsync($"services/{service}/jobs/{job}");

            if (!res.IsSuccessStatusCode) // System.Net.HttpStatusCode.OK
                return null;

            string data = await res.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<Job>(data);
        }

        public async Task<bool> DeleteJob(string service, string job)
        {
            bool connected = await Authenticate();
            if (!connected)
                return false;

            HttpResponseMessage res = await Client.DeleteAsync($"services/{service}/jobs/{job}");

            if (!res.IsSuccessStatusCode) // System.Net.HttpStatusCode.NoContent
                return false;

            return true;
        }

        public async Task<bool> SendJobInput(string service, string job, string file, string contenttype)
        {
            bool connected = await Authenticate();
            if (!connected)
                return false;

            HttpContent content = new ByteArrayContent(System.IO.File.ReadAllBytes(file));
            content.Headers.Add("Content-Type", contenttype);
            content.Headers.Add("Content-Disposition", $"attachment; filename=\"{System.IO.Path.GetFileName(file)}\"");

            string uri = $"services/{service}/jobs/{job}/input";

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Content = content;

            HttpResponseMessage res = await Client.SendAsync(request);
            if (!res.IsSuccessStatusCode) // System.Net.HttpStatusCode.Accepted
                return false;

            return true;
        }

        public async Task<bool> ReadJobOutput(string service, string job, string file)
        {
            bool connected = await Authenticate();
            if (!connected)
                return false;

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"services/{service}/jobs/{job}/output");
            request.Headers.Add("Accept", "*/*");

            HttpResponseMessage res = await Client.SendAsync(request);
            if (!res.IsSuccessStatusCode) // System.Net.HttpStatusCode.OK
                return false;

            byte[] data = await res.Content.ReadAsByteArrayAsync();

            System.IO.File.WriteAllBytes(file, data);
            return true;
        }

        string url = "";
        string username = "";
        string password = "";

        AuthVal? auth = null;
        HttpClient? client = null;

        private HttpClient Client
        {
            get
            {
                if (client == null)
                {
                    client = new HttpClient();
                    client.BaseAddress = new Uri(Url);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                }

                return client;
            }
        }

        public string Url { get => url; set => url = value; }
        public string UserName { get => username; set => username = value; }
        public string Password { get => password; set => password = value; }

        public class AuthVal
        {
            public User user { get; set; } = new User();
            public string access_token { get; set; } = "";
            public DateTime access_token_expires_at { get; set; } = DateTime.Now;
            public string refresh_token { get; set; } = "";
            public DateTime refresh_token_expires_at { get; set; } = DateTime.Now;
            public string _id { get; set; } = "";
        }

        public class User
        {
            public string name { get; set; } = "";
            public bool active { get; set; } = false;
            public string password { get; set; } = "";
            public string realname { get; set; } = "";
            public string role { get; set; } = "";
            public string[] scopes { get; set; } = new string[] { };
            public string id { get; set; } = "";
        }

        public class Service
        {
            public string type { get; set; } = "";
            public bool active { get; set; } = false;
            public string shape { get; set; } = "";
            public Timestamps timestamps { get; set; } = new Timestamps();
            public string version { get; set; } = "";
            public string id { get; set; } = "";
        }

        public class Info
        {
            public string name { get; set; } = "";
            public string version { get; set; } = "";
        }

        public class Created
        {
            public string id { get; set; } = "";
        }

        public class Resource
        {
            public string id { get; set; } = "";
            [JsonPropertyName("content-type")]
            public string content_type { get; set; } = "";
            [JsonPropertyName("content-length")]
            public int content_length { get; set; } = 0;
            public string name { get; set; } = "";
        }

        public class Timestamps
        {
            public DateTime? created { get; set; }
            public DateTime? modified { get; set; }
        }

        public class JobOutput
        {
            public string name { get; set; } = "";
            [JsonPropertyName("content-type")]
            public string content_type { get; set; } = "";
        }

        public class JobInput
        {
            public string type { get; set; } = "siso";
            public bool active { get; set; } = false;
            public JobOutput output { get; set; } = new JobOutput();
        }

        public class Job
        {
            public string type { get; set; } = "siso";
            public bool active { get; set; } = false;
            public JobOutput output { get; set; } = new JobOutput();
            public string service_id { get; set; } = "";
            public string status { get; set; } = "";
            public Timestamps timestamps { get; set; } = new Timestamps();
            public string id { get; set; } = "";
        }
    }
}
