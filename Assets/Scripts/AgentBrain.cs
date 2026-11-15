using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// The "brain" of the agent: keeps the persona (system prompt) and the
/// conversation history, talks to the LLM and runs tool calls.
/// </summary>
public class AgentBrain : MonoBehaviour
{
    public LLMClient llm;
    public AgentActions actions;
    public InteractionLogger logger;

    [TextArea(4, 10)]
    public string persona =
        "Du bist Mia, eine freundliche virtuelle Assistentin in einem kleinen 3D-Raum. " +
        "Antworte kurz (höchstens 2-3 Sätze) und in der Sprache der Nutzerin oder des Nutzers. " +
        "Du kannst winken und die Lichtfarbe im Raum ändern. Nutze diese Funktionen, " +
        "wenn es passt, z. B. winke bei einer Begrüßung.";

    public int maxHistoryMessages = 20;   // keeps the request small
    public int maxToolRounds = 3;         // safety limit for the tool loop

    public bool IsBusy { get; private set; }
    public event Action<string> OnAgentReply;

    private JArray history = new JArray();

    void Start()
    {
        history.Add(Message("system", persona));
    }

    public void Ask(string userText)
    {
        if (IsBusy || string.IsNullOrWhiteSpace(userText)) return;
        StartCoroutine(Conversation(userText));
    }

    IEnumerator Conversation(string userText)
    {
        IsBusy = true;
        Stopwatch timer = Stopwatch.StartNew();
        List<string> toolsUsed = new List<string>();
        int historyCountBefore = history.Count;

        history.Add(Message("user", userText));

        string finalText = null;
        string error = null;

        // 1) ask the model  2) if it wants tools -> run them and ask again  3) stop at a text answer
        for (int round = 0; round < maxToolRounds && finalText == null && error == null; round++)
        {
            JObject reply = null;
            yield return llm.SendChat(history, actions.GetToolDefinitions(),
                r => reply = r,
                e => error = e);

            if (error != null) break;

            JArray toolCalls = reply["tool_calls"] as JArray;
            if (toolCalls != null && toolCalls.Count > 0)
            {
                // the assistant message with the tool calls must stay in the history
                history.Add(reply);

                foreach (JObject call in toolCalls)
                {
                    string name = (string)call["function"]["name"];
                    string args = (string)call["function"]["arguments"];
                    Debug.Log("Tool call: " + name + " " + args);

                    string result = actions.Execute(name, args);
                    toolsUsed.Add(name);

                    JObject toolMessage = new JObject();
                    toolMessage["role"] = "tool";
                    toolMessage["tool_call_id"] = (string)call["id"];
                    toolMessage["content"] = result;
                    history.Add(toolMessage);
                }
            }
            else
            {
                finalText = (string)reply["content"] ?? "";
                history.Add(Message("assistant", finalText));
            }
        }

        if (error != null)
        {
            Debug.LogError("LLM error: " + error);
            // remove this turn again so the history stays valid
            while (history.Count > historyCountBefore) history.RemoveAt(history.Count - 1);
            finalText = "(Fehler: keine Antwort vom Sprachmodell - siehe Console)";
        }
        else if (finalText == null)
        {
            finalText = "(Keine Textantwort nach " + maxToolRounds + " Runden)";
        }

        timer.Stop();
        if (logger != null)
            logger.Log(userText, finalText, string.Join("|", toolsUsed), timer.ElapsedMilliseconds);

        TrimHistory();
        IsBusy = false;
        OnAgentReply?.Invoke(finalText);
    }

    // Keep the system prompt (index 0) and only the newest messages
    void TrimHistory()
    {
        while (history.Count > maxHistoryMessages)
        {
            history.RemoveAt(1);
        }
        // a "tool" message without its assistant message would cause an API error
        while (history.Count > 1 && (string)history[1]["role"] == "tool")
        {
            history.RemoveAt(1);
        }
    }

    static JObject Message(string role, string content)
    {
        JObject msg = new JObject();
        msg["role"] = role;
        msg["content"] = content;
        return msg;
    }
}
