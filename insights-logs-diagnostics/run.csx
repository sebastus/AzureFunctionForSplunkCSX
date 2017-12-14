#load "../shared/sendToSplunk.csx"

public static void Run(string[] messages, TraceWriter log)
{
    log.Info($"Received {messages.Length} messages from event hub.");

    SendMessagesToSplunk(messages, log).Wait();

}
