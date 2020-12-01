using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using ImageRecognition;

namespace Contracts
{
    public class LibraryClient
    {
        private string SERVER_URI;
        private HttpClient client;
        public LibraryClient(string SERVER_URI)
        {
            this.SERVER_URI = SERVER_URI;
            this.client = new HttpClient();
        }

        public async Task<Tuple<List<PredictionResponse>, List<PredictionRequest>>> PostOld(string SelectedPath, CancellationTokenSource cts)
        {
            var t = await Task.Run(async () => {
                var tmp = Directory.GetFiles(SelectedPath).Select(i => new PredictionRequest(i)).ToList();
                var DataAsString = JsonConvert.SerializeObject(tmp);
                var content = new StringContent(DataAsString);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var response = await client.PostAsync(SERVER_URI + "/Old", content, cts.Token);

                var buf = JsonConvert.DeserializeObject<Tuple<List<PredictionResponse>, List<PredictionRequest>>>(response.Content.ReadAsStringAsync().Result);
                return buf;
            });
            return t;
        }

        public async Task<List<PredictionResult>> GetNew(List<PredictionRequest> prq, CancellationTokenSource cts)
        {
            var t = await Task.Run(async () => {
                var DataAsString = JsonConvert.SerializeObject(prq);
                var content = new StringContent(DataAsString);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var response = await client.PostAsync(SERVER_URI + "/New", content, cts.Token);
                var buf = JsonConvert.DeserializeObject<List<PredictionResult>>(response.Content.ReadAsStringAsync().Result);
                return buf.ToList();
            });
            return t;
        }

        public async Task<List<Tuple<string, int>>> GetStats()
        {
            var t = await Task.Run(async () => {
                var response = client.GetAsync(SERVER_URI).Result;
                var stats = JsonConvert.DeserializeObject<List<Tuple<string, int>>>(response.Content.ReadAsStringAsync().Result);
                return stats;
            });
            return t;
        }

        public void Delete()
        {
            var t = Task.Run(async () => { 
                var response = client.DeleteAsync(SERVER_URI).Result;
                return 0;
            });
        }
    }
}
