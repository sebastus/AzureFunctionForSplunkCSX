#load "unpackMessages.csx"
#load "addStandardProperties.csx"
#load "getEnvironmentVariable.csx"
#load "obRelay.csx"
#load "obHEC.csx"
#load "newEvent.csx"
 
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks; 

public static async Task SendMessagesToSplunk(string[] myEventHubMessages, TraceWriter log)
{
    string[] unpackedMessages = new string[1];
    try {
        unpackedMessages = unpackMessages(myEventHubMessages, log);
    } catch (Exception ex) {
        log.Error(String.Format("Error {0} caught in unpackMessages.", ex));
    }

    string[] standardizedEvents = new string[1];
    try {
        List<string> listOfStandardizedEvents = new List<string>();
        foreach (string item in unpackedMessages)
        {
            string standardizedMessage = addStandardProperties(item, log);
            listOfStandardizedEvents.Add(newEvent(standardizedMessage));
        }
        standardizedEvents = listOfStandardizedEvents.ToArray();
    } catch (Exception ex) {
        log.Error(String.Format("Error {0} caught while adding standard properties.", ex));
    }

    var outputBinding = getEnvironmentVariable("outputBinding");
    log.Info(String.Format("The output binding is {0}", outputBinding));

    if (outputBinding == "Relay") 
    {
        await obRelay(standardizedEvents, log);
    }
    else if (outputBinding == "HEC")
    {
        await obHEC(standardizedEvents, log);
    }
}


