using System;

static string newEvent(string json, string sourceType = "azure_monitor_logs")
{
    var s = "{";
    s += "\"sourcetype\": \"" + sourceType + "\",";
    s += "\"event\": " + json;
    s += "}";
    return s;
}
