#r "Newtonsoft.Json"
#r "System.Net.Http"

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

static async Task SendMessagesToSplunk(string[] messages, TraceWriter log, string sourceType = "azure_monitor_logs")
{

    string newEvent(string json) {
        var s = "{";
        s += "\"sourcetype\": \"" + sourceType + "\",";
        s += "\"event\": " + json;
        s += "}";
        return s;
    }

    var converter = new ExpandoObjectConverter();

    ServicePointManager.ServerCertificateValidationCallback =
    new System.Net.Security.RemoteCertificateValidationCallback(
        delegate { return true; });

    var client = new SingleHttpClientInstance();

    string newClientContent = "";
    foreach (var message in messages)
    {
        try
        {
            dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(message, converter);

            if ( ((IDictionary<string, object>)obj).Keys.Contains("records")) 
            { 
                foreach (var record in obj.records)
                {
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(record);
                    newClientContent += newEvent(json);
                }
            } else
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
                newClientContent += newEvent(json);
            }
        }
        catch (Exception e)
        {
            var errorMessage = "";
            if (e.InnerException == null)
            {
                errorMessage = e.Message;
            }
            else
            {
                errorMessage = e.InnerException.Message;
            }
            log.Info($"Error {errorMessage} caught while parsing message: {message}");
        }

    }

    log.Verbose($"New events going to Splunk: {newClientContent}");

    var splunkAddress = GetEnvironmentVariable("splunkAddress");
    var splunkToken = GetEnvironmentVariable("splunkToken");

    try
    {
        HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, splunkAddress);
        req.Headers.Accept.Clear();
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        req.Headers.Add("Authorization", "Splunk " + splunkToken);
        req.Content = new StringContent(newClientContent, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await SingleHttpClientInstance.SendToSplunk(req);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            log.Error($"StatusCode from Splunk: {response.StatusCode}, and reason: {response.ReasonPhrase}");
        }
    }
    catch (System.Net.Http.HttpRequestException e)
    {
        log.Error($"Error: \"{e.InnerException.Message}\" was caught while sending to Splunk. Is the Splunk service running?");
    }
    catch (Exception f)
    {
        log.Error($"Error \"{f.InnerException.Message}\" was caught while sending to Splunk. Unplanned exception.");
    }
}

public static string GetEnvironmentVariable(string name)
{
    return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
}