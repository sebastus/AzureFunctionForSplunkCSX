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

public static async Task Run(string[] myEventHubMessages, TraceWriter log)
{ 
    string[] unpackedMessages = unpackMessages(myEventHubMessages, log);

    List<string> listOfStandardizedEvents = new List<string>();
    foreach (string item in unpackedMessages)
    {
        string standardizedMessage = addStandardProperties(item, log);
        listOfStandardizedEvents.Add(newEvent(standardizedMessage));
    }
    string[] standardizedEvents = listOfStandardizedEvents.ToArray();

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


