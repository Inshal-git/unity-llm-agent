using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Very simple chat window using Unity's IMGUI (OnGUI).
/// Good enough for testing on the desktop - no Canvas setup needed.
/// </summary>
public class ChatUI : MonoBehaviour
{
    public AgentBrain brain;
    public string agentName = "Mia";

    private string input = "";
    private List<string> lines = new List<string>();
    private Vector2 scroll;

    void OnEnable()
    {
        brain.OnAgentReply += HandleReply;
    }

    void OnDisable()
    {
        brain.OnAgentReply -= HandleReply;
    }

    void HandleReply(string text)
    {
        lines.Add(agentName + ": " + text);
        scroll.y = float.MaxValue;   // jump to the newest message
    }

    void Send()
    {
        if (brain.IsBusy || string.IsNullOrWhiteSpace(input)) return;
        lines.Add("Du: " + input);
        brain.Ask(input);
        input = "";
        scroll.y = float.MaxValue;
    }

    void OnGUI()
    {
        float width = 420f;
        float height = 260f;
        GUILayout.BeginArea(new Rect(10, Screen.height - height - 10, width, height), GUI.skin.box);

        scroll = GUILayout.BeginScrollView(scroll);
        foreach (string line in lines)
        {
            GUILayout.Label(line);
        }
        if (brain.IsBusy) GUILayout.Label(agentName + " denkt nach ...");
        GUILayout.EndScrollView();

        // send with the Enter key
        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "ChatInput")
        {
            Send();
            e.Use();
        }

        GUILayout.BeginHorizontal();
        GUI.SetNextControlName("ChatInput");
        input = GUILayout.TextField(input, GUILayout.Width(width - 90));
        if (GUILayout.Button(brain.IsBusy ? "..." : "Senden")) Send();
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }
}
