using UnityEngine;
using System.Collections;

// https://stackoverflow.com/questions/67704820/how-do-i-print-unitys-debug-log-to-the-screen-gui
public class Logger : MonoBehaviour
{
    public bool Visible = false;
    uint qsize = 15;  // number of messages to keep
    Queue myLogQueue = new Queue();

    void Start() {
        Debug.Log("Started up logging.");
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.F12))
            Visible = !Visible;
    }

    void OnEnable() {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable() {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type) {
        myLogQueue.Enqueue("[" + type + "] : " + logString);
        if (type == LogType.Exception)
            myLogQueue.Enqueue(stackTrace);
        while (myLogQueue.Count > qsize)
            myLogQueue.Dequeue();
    }

    void OnGUI() {
        if (!Visible)
            return;

        GUILayout.BeginArea(new Rect(Screen.width - 400, 0, 400, Screen.height));
        GUILayout.Label("\n" + string.Join("\n", myLogQueue.ToArray()));
        GUILayout.EndArea();
    }
}