#r "System.Net.Http"
#load "getEnvironmentVariable.csx"
#load "newEvent.csx"

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Headers;

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

static async Task obHEC(string[] standardizedEvents, TraceWriter log)
{
    string splunkAddress = getEnvironmentVariable("splunkAddress");
    string splunkToken = getEnvironmentVariable("splunkToken");
    if (splunkAddress.Length == 0 || splunkToken.Length == 0){
        log.Error("Values for splunkAddress and splunkToken are required.");
        return;
    }

    ServicePointManager.Expect100Continue = true;
    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
    ServicePointManager.ServerCertificateValidationCallback =
    new System.Net.Security.RemoteCertificateValidationCallback(
        delegate { return true; });

    string newClientContent = "";
    foreach (string item in standardizedEvents)
    {
        newClientContent += item;
    }
    var client = new SingleHttpClientInstance();
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