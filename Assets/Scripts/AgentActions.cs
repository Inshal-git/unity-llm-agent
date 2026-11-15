using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// The things the agent can DO in the scene (function calling / "tools").
/// The LLM decides when to call them, Unity executes them.
/// </summary>
public class AgentActions : MonoBehaviour
{
    public Transform agentBody;   // the capsule that represents the agent
    public Light sceneLight;      // the light the agent is allowed to change

    private bool isWaving = false;

    // Tool descriptions in the OpenAI "tools" format.
    // The descriptions are important: the model reads them to decide which tool to use.
    public JArray GetToolDefinitions()
    {
        return JArray.Parse(@"[
          {
            ""type"": ""function"",
            ""function"": {
              ""name"": ""wave"",
              ""description"": ""Der Agent springt kurz und dreht sich, z. B. zur Begrüßung oder zum Abschied."",
              ""parameters"": { ""type"": ""object"", ""properties"": {} }
            }
          },
          {
            ""type"": ""function"",
            ""function"": {
              ""name"": ""set_light_color"",
              ""description"": ""Ändert die Farbe des Lichts im Raum."",
              ""parameters"": {
                ""type"": ""object"",
                ""properties"": {
                  ""color"": {
                    ""type"": ""string"",
                    ""enum"": [""red"", ""green"", ""blue"", ""yellow"", ""white""],
                    ""description"": ""Die neue Lichtfarbe""
                  }
                },
                ""required"": [""color""]
              }
            }
          }
        ]");
    }

    // Runs a tool and returns a short result text that is sent back to the model
    public string Execute(string toolName, string argumentsJson)
    {
        JObject args = string.IsNullOrEmpty(argumentsJson) ? new JObject() : JObject.Parse(argumentsJson);

        switch (toolName)
        {
            case "wave":
                if (!isWaving) StartCoroutine(WaveRoutine());
                return "Der Agent hat gewunken.";

            case "set_light_color":
                string colorName = (string)args["color"];
                Color color;
                if (!TryGetColor(colorName, out color))
                    return "Unbekannte Farbe: " + colorName;
                sceneLight.color = color;
                return "Licht ist jetzt " + colorName + ".";

            default:
                Debug.LogWarning("Unknown tool: " + toolName);
                return "Unbekanntes Tool: " + toolName;
        }
    }

    bool TryGetColor(string name, out Color color)
    {
        switch (name)
        {
            case "red": color = Color.red; return true;
            case "green": color = Color.green; return true;
            case "blue": color = new Color(0.3f, 0.5f, 1f); return true;
            case "yellow": color = Color.yellow; return true;
            case "white": color = Color.white; return true;
            default: color = Color.white; return false;
        }
    }

    // Simple "wave": jump up, turn once, land again (1 second)
    IEnumerator WaveRoutine()
    {
        isWaving = true;
        Vector3 startPos = agentBody.position;
        Quaternion startRot = agentBody.rotation;
        float duration = 1f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            float height = Mathf.Sin(p * Mathf.PI) * 0.5f;  // up and down
            agentBody.position = startPos + Vector3.up * height;
            agentBody.rotation = startRot * Quaternion.Euler(0f, 360f * p, 0f);
            yield return null;
        }

        agentBody.position = startPos;
        agentBody.rotation = startRot;
        isWaving = false;
    }
}
