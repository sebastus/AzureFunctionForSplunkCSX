#r "Newtonsoft.Json"

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

static string addStandardProperties(string message, TraceWriter log)
{
    var converter = new ExpandoObjectConverter();
    dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(message, converter);

    object resourceId;
    ((IDictionary<string, object>)obj).TryGetValue("resourceId", out resourceId);
    if (resourceId == null) {
        log.Info("resourceId not found in incoming message.");
        return "{}";
    }

    var standardProperties = getStandardProperties(((string)resourceId).ToUpper());

    if (standardProperties["subscriptionId"] == "" ||
        standardProperties["resourceGroup"] == "" || 
        standardProperties["resourceType"] == "" || 
        standardProperties["resourceName"] == "") {
        log.Info("Incorrect message format, badly formed resourceId");
        return "{}";
    }

    obj.am_subscriptionId = standardProperties["subscriptionId"];
    obj.am_resourceGroup = standardProperties["resourceGroup"];
    obj.am_resourceType = standardProperties["resourceType"];
    obj.am_resourceName = standardProperties["resourceName"];

    string json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);

    return json;
}

public static System.Collections.Generic.Dictionary<string, string> getStandardProperties(string resourceId)
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