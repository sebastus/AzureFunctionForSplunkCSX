#r "Newtonsoft.Json"
#r "System.Net.Http"

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
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

    ServicePointManager.Expect100Continue = true;
    ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
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
                    object resourceId;
                    ((IDictionary<string, object>)record).TryGetValue("resourceId", out resourceId);

                    var standardValues = GetStandardValues(((string)resourceId).ToUpper());
                    record.am_subscriptionId = standardValues["subscriptionId"];
                    record.am_resourceGroup = standardValues["resourceGroup"];
                    record.am_resourceType = standardValues["resourceType"];
                    record.am_resourceName = standardValues["resourceName"];

                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(record);
                    newClientContent += newEvent(json);
                }
            } else
            {
                object resourceId;
                ((IDictionary<string, object>)obj).TryGetValue("resourceId", out resourceId);

                var standardValues = GetStandardValues(((string)resourceId).ToUpper());
                obj.am_subscriptionId = standardValues["subscriptionId"];
                obj.am_resourceGroup = standardValues["resourceGroup"];
                obj.am_resourceType = standardValues["resourceType"];
                obj.am_resourceName = standardValues["resourceName"];

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

public static System.Collections.Generic.Dictionary<string, string> GetStandardValues(string resourceId)
{
    var patternSubscriptionId = "SUBSCRIPTIONS\\/(.*?)\\/";
    var patternResourceGroup = "SUBSCRIPTIONS\\/(?:.*?)\\/RESOURCEGROUPS\\/(.*?)\\/";
    var patternResourceType = "PROVIDERS\\/(.*?\\/.*?)(?:\\/)";
    var patternResourceName = "PROVIDERS\\/(?:.*?\\/.*?\\/)(.*?)(?:\\/|$)";

    System.Collections.Generic.Dictionary<string, string> values = new System.Collections.Generic.Dictionary<string, string>();
    
    Match m = Regex.Match(resourceId, patternSubscriptionId);
    var subscriptionID = m.Groups[1].Value;
    values.Add("subscriptionId", subscriptionID);

    m = Regex.Match(resourceId, patternResourceGroup);
    var resourceGroup = m.Groups[1].Value;
    values.Add("resourceGroup", resourceGroup);

    m = Regex.Match(resourceId, patternResourceType);
    var resourceType = m.Groups[1].Value;
    values.Add("resourceType", resourceType);

    m = Regex.Match(resourceId, patternResourceName);
    var resourceName = m.Groups[1].Value;
    values.Add("resourceName", resourceName);

    return values;
}