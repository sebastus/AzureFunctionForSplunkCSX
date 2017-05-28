#r "Newtonsoft.Json"

using System;
using System.Dynamic;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public class SingleHttpClientInstance
{
    private static readonly HttpClient HttpClient;

    static SingleHttpClientInstance()
    {
        HttpClient = new HttpClient();
    }

    public static async Task<HttpResponseMessage> SendToSplunk(HttpRequestMessage req)
    {
        HttpResponseMessage response = await HttpClient.SendAsync(req);
        return response;
    }
}

static async Task SendMessagesToSplunk(string[] messages, TraceWriter log)
{

    var converter = new ExpandoObjectConverter();

    ServicePointManager.ServerCertificateValidationCallback =
    new System.Net.Security.RemoteCertificateValidationCallback(
        delegate { return true; });

    var client = new SingleHttpClientInstance();

    string newClientContent = "";
    foreach (var message in messages)
    {
        try {
            dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(message, converter);

            foreach (var record in obj.records)
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(record);
                newClientContent += "{\"event\": " + json + "}";
            }

        } catch (Exception e) {
            log.Info($"Error {e} caught while parsing message: {message}");
        }

    }
    HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, "https://asplunktest.westus.cloudapp.azure.com:8088/services/collector/event");
    req.Headers.Accept.Clear();
    req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    req.Headers.Add("Authorization", "Splunk 73A24AB7-60DD-4235-BF71-D892AE47F49D");
    req.Content = new StringContent(newClientContent, Encoding.UTF8, "application/json");
    HttpResponseMessage response = await SingleHttpClientInstance.SendToSplunk(req);
}
