#load "newEvent.csx"
#load "getEnvironmentVariable.csx"

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Relay;

public static async Task obRelay(string[] standardizedEvents, TraceWriter log)
{

    string newClientContent = "[";
    foreach (string item in standardizedEvents)
    {
        if (newClientContent.Length != 1) newClientContent += ",";
        newClientContent += item;
    }
    newClientContent += "]";

    bool Done = false;
    while (!Done)
    {
        try
        {
            Done = HybridAsync(newClientContent, log).GetAwaiter().GetResult();
        }
        catch (EndpointNotFoundException)
        {
            log.Info("Waiting...");
            Thread.Sleep(1000);
        }
        catch (RelayException)
        {
            log.Info("Connection forcibly closed.");
        }
        catch (Exception ex)
        {
            log.Error("Error executing function: " + ex.Message);
        }
    }
}

static async Task<bool> HybridAsync(string newClientContent, TraceWriter log)
{
    string RelayNamespace = getEnvironmentVariable("relayNamespace") + ".servicebus.windows.net";
    string ConnectionName = getEnvironmentVariable("relayPath");
    string KeyName = getEnvironmentVariable("policyName");
    string Key = getEnvironmentVariable("policyKey");
    if (RelayNamespace.Length == 0 || ConnectionName.Length == 0 || KeyName.Length == 0 || Key.Length == 0) {
        log.Error("Values must be specified for RelayNamespace, relayPath, KeyName and Key.");
        return true;
    }

    var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(KeyName, Key);
    var client = new HybridConnectionClient(new Uri(String.Format("sb://{0}/{1}", RelayNamespace, ConnectionName)), tokenProvider);

    // Initiate the connection
    var relayConnection = await client.CreateConnectionAsync();
    log.Verbose("Connection accepted.");

    int bufferSize = newClientContent.Length;
    log.Info($"newClientContent byte count: {bufferSize}");

    var writes = Task.Run(async () => {
        var writer = new StreamWriter(relayConnection, Encoding.UTF8, bufferSize) { AutoFlush = true };
        await writer.WriteAsync(newClientContent);
    });

    // Wait for both tasks to complete
    await Task.WhenAll(writes);

    await relayConnection.CloseAsync(CancellationToken.None);
    log.Verbose("Connection closed.");

    return true;
}

