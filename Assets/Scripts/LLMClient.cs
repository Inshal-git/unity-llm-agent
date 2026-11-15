using System;
using System.Collections;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

// Settings that are read from llm_config.json (project root, NOT committed to Git)
[Serializable]
public class LLMConfig
{
    public string apiUrl;   // e.g. https://api.openai.com/v1/chat/completions
    public string apiKey;
    public string model;    // e.g. gpt-4o-mini
}

/// <summary>
/// Sends chat messages to an OpenAI-compatible Chat Completions API
/// (works with OpenAI and with a local Ollama server).
/// </summary>
public class LLMClient : MonoBehaviour
{
    public string configFileName = "llm_config.json";
    [Range(0f, 1.5f)] public float temperature = 0.7f;

    private LLMConfig config;

    void Awake()
    {
        // Application.dataPath = .../Assets, so ".." is the project root
        string path = Path.Combine(Application.dataPath, "..", configFileName);
        if (!File.Exists(path))
        {
            Debug.LogError("Config file not found: " + path + " (copy llm_config.example.json and add your key)");
            return;
        }
        config = JsonUtility.FromJson<LLMConfig>(File.ReadAllText(path));
        Debug.Log("LLM config loaded. Model: " + config.model);
    }

    /// <summary>
    /// Sends the conversation (and the available tools) to the model.
    /// onSuccess gets the "message" object of the first choice.
    /// </summary>
    public IEnumerator SendChat(JArray messages, JArray tools, Action<JObject> onSuccess, Action<string> onError)
    {
        if (config == null)
        {
            onError("No LLM config loaded.");
            yield break;
        }

        JObject body = new JObject();
        body["model"] = config.model;
        body["messages"] = messages.DeepClone();
        body["temperature"] = temperature;
        if (tools != null && tools.Count > 0)
        {
            body["tools"] = tools.DeepClone();
        }

        byte[] data = Encoding.UTF8.GetBytes(body.ToString(Formatting.None));

        using (UnityWebRequest request = new UnityWebRequest(config.apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(data);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + config.apiKey);
            request.timeout = 60;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError(request.error + "\n" + request.downloadHandler.text);
                yield break;
            }

            JObject json;
            try
            {
                json = JObject.Parse(request.downloadHandler.text);
            }
            catch (Exception e)
            {
                onError("Could not parse response: " + e.Message);
                yield break;
            }

            JObject message = json["choices"]?[0]?["message"] as JObject;
            if (message == null)
            {
                onError("Unexpected response: " + request.downloadHandler.text);
                yield break;
            }
            onSuccess(message);
        }
    }
}
