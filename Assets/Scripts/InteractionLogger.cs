using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Writes every interaction into a CSV file so it can be analysed later
/// (e.g. response times, which tools were used). One file per session.
/// </summary>
public class InteractionLogger : MonoBehaviour
{
    public string participantId = "P01";

    private string filePath;

    void Awake()
    {
        string folder = Path.Combine(Application.persistentDataPath, "logs");
        Directory.CreateDirectory(folder);

        string fileName = "session_" + participantId + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
        filePath = Path.Combine(folder, fileName);

        File.WriteAllText(filePath,
            "timestamp,participant_id,user_input,agent_response,tools_used,response_time_ms\n",
            Encoding.UTF8);
        Debug.Log("Logging interactions to: " + filePath);
    }

    public void Log(string userInput, string agentResponse, string toolsUsed, long responseTimeMs)
    {
        string line = string.Join(",",
            Csv(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
            Csv(participantId),
            Csv(userInput),
            Csv(agentResponse),
            Csv(toolsUsed),
            responseTimeMs.ToString());

        File.AppendAllText(filePath, line + "\n", Encoding.UTF8);
    }

    // Put text in quotes and double inner quotes, so commas/newlines don't break the CSV
    static string Csv(string value)
    {
        if (value == null) value = "";
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
