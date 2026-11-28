using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;

// Playtest analytics. Needs no scene object: MorningSequenceManager creates it.
// Every playthrough gets a new random ID, used as the Unity Analytics user ID and
// written to a local CSV backup. No personal data is recorded.
//
// Get the tester's consent BEFORE pressing Play: collection starts straight away.
public class AnalyticsManager : MonoBehaviour
{
    public const string AlarmTask = "alarm_off";
    public const string BrushingTask = "brush_teeth";

    // game_completed is sent once all of these are done. Add the curtains here later.
    private static readonly string[] requiredTasks = { AlarmTask, BrushingTask };

    private static AnalyticsManager instance;

    private readonly HashSet<string> completedTasks = new HashSet<string>();
    private readonly List<CustomEvent> pendingEvents = new List<CustomEvent>();
    private string playerId;
    private readonly System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
    private bool ready;
    private bool gameCompleted;

    private static string LogPath => Path.Combine(Application.persistentDataPath, "playtest_log.csv");

    public static void BeginPlaythrough()
    {
        if (instance != null) return;
        instance = new GameObject("AnalyticsManager").AddComponent<AnalyticsManager>();
    }

    // Safe to call repeatedly: each task counts once per playthrough.
    public static void CompleteTask(string taskName)
    {
        if (instance != null) instance.RecordTask(taskName);
    }

    private async void Awake()
    {
        playerId = "anon-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        timer.Start(); // not Time.*, which Unity resets when Play mode stops

        Debug.Log("[Analytics] Playthrough ID: " + playerId + "   Backup log: " + LogPath);
        WriteLog("session_start", "", 0f);

        try
        {
            UnityServices.ExternalUserId = playerId; // must be set before data collection starts
            await UnityServices.InitializeAsync();
            if (this == null) return; // Play mode ended while waiting

            // Works in this package version; the newer consent API isn't needed here
#pragma warning disable 0618
            AnalyticsService.Instance.StartDataCollection();
#pragma warning restore 0618

            ready = true;
            foreach (CustomEvent e in pendingEvents) AnalyticsService.Instance.RecordEvent(e);
            pendingEvents.Clear();
            AnalyticsService.Instance.Flush();
        }
        catch (Exception e)
        {
            // The game and the CSV backup keep working without Unity Analytics
            Debug.LogWarning("[Analytics] Could not start, using the CSV backup only: " + e.Message);
        }
    }

    private void OnDestroy()
    {
        WriteLog("session_end", gameCompleted ? "completed" : "not_completed", Elapsed());
        if (instance == this) instance = null;
    }

    private void RecordTask(string taskName)
    {
        if (!completedTasks.Add(taskName)) return;

        float elapsed = Elapsed();
        WriteLog("task_completed", taskName, elapsed);
        Send(new CustomEvent("task_completed") { { "task_name", taskName } });

        if (gameCompleted) return;
        foreach (string task in requiredTasks)
            if (!completedTasks.Contains(task)) return;

        gameCompleted = true;
        WriteLog("game_completed", "", elapsed);
        Send(new CustomEvent("game_completed") { { "total_time", elapsed } });
    }

    private void Send(CustomEvent e)
    {
        if (!ready)
        {
            pendingEvents.Add(e);
            return;
        }

        try
        {
            AnalyticsService.Instance.RecordEvent(e);
            AnalyticsService.Instance.Flush(); // don't lose events when Play mode stops
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[Analytics] Event not sent (it is in the CSV backup): " + ex.Message);
        }
    }

    private float Elapsed() => (float)timer.Elapsed.TotalSeconds;

    private void WriteLog(string eventName, string detail, float seconds)
    {
        try
        {
            if (!File.Exists(LogPath))
                File.WriteAllText(LogPath, "utc_time,player_id,event,detail,seconds_since_start\n");

            File.AppendAllText(LogPath, string.Format(CultureInfo.InvariantCulture,
                "{0:yyyy-MM-dd HH:mm:ss},{1},{2},{3},{4:F1}\n",
                DateTime.UtcNow, playerId, eventName, detail, seconds));
        }
        catch (Exception e)
        {
            // Usually the CSV is open in Excel
            Debug.LogWarning("[Analytics] Could not write the CSV backup: " + e.Message);
        }
    }
}
