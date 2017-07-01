#r "Newtonsoft.Json"

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

static string[] unpackMessages(string[] messages, TraceWriter log)
{
    var converter = new ExpandoObjectConverter();

    List<string> listOfUnpackedMessages = new List<string>();
    foreach (var message in messages)
    {
        dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(message, converter);
        if (((IDictionary<string, object>)obj).Keys.Contains("records"))
        {
            foreach (var record in obj.records)
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(record);
                listOfUnpackedMessages.Add(json);
            }
        }
        else
        {
            listOfUnpackedMessages.Add(message);
        }
    }
    string[] returnedMessages = listOfUnpackedMessages.ToArray();
    return returnedMessages;
}