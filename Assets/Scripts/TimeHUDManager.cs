using System;
using TMPro;
using UnityEngine;

/// <summary>
/// One block of the day/activity schedule: while the simulated clock falls within
/// [startHour, endHour), <see cref="label"/> is shown as the activity state.
/// </summary>
[Serializable]
public struct ActivityWindow
{
    [Tooltip("Inclusive start hour in 24-hour time, e.g. 7.5 = 7:30 AM.")]
    public float startHour;
    [Tooltip("Exclusive end hour in 24-hour time.")]
    public float endHour;
    public string label;
}

/// <summary>
/// Owns a simulated time-of-day (real seconds -> in-game hours) and writes formatted
/// strings into three TextMeshPro fields: clock ("7:10 AM"), activity state
/// ("Before School"), and day ("Monday"). Decoupled from player/camera scripts —
/// place the driven TextMeshProUGUI objects in a Canvas anchored to the upper right.
/// </summary>
public class TimeHUDManager : MonoBehaviour
{
    [Header("Text Targets")]
    [SerializeField] private TextMeshProUGUI clockLabel;
    [SerializeField] private TextMeshProUGUI activityLabel;
    [SerializeField] private TextMeshProUGUI dayLabel;

    [Header("Time Simulation")]
    [Tooltip("24-hour start time, e.g. 7f = 7:00 AM.")]
    [SerializeField] private float startHour = 7f;
    [Tooltip("How many real-world minutes a full in-game day (24h) takes.")]
    [SerializeField] private float realMinutesPerDay = 10f;
    [Tooltip("Index into dayNames the simulation starts on.")]
    [SerializeField] private int startDayIndex = 0;
    [SerializeField]
    private string[] dayNames =
    {
        "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"
    };

    [Header("Activity Schedule")]
    [Tooltip("Checked in order; the first window containing the current hour wins.")]
    [SerializeField]
    private ActivityWindow[] schedule =
    {
        new ActivityWindow { startHour = 0f, endHour = 8f, label = "Before School" },
        new ActivityWindow { startHour = 8f, endHour = 12f, label = "Class" },
        new ActivityWindow { startHour = 12f, endHour = 13f, label = "Lunch" },
        new ActivityWindow { startHour = 13f, endHour = 15f, label = "Class" },
        new ActivityWindow { startHour = 15f, endHour = 24f, label = "After School" },
    };
    [SerializeField] private string fallbackActivityLabel = "After School";

    private float currentHour;
    private int currentDayIndex;

    private void Awake()
    {
        currentHour = startHour;
        currentDayIndex = startDayIndex;
    }

    private void Update()
    {
        AdvanceTime(Time.deltaTime);
        Refresh();
    }

    private void AdvanceTime(float deltaSeconds)
    {
        if (realMinutesPerDay <= 0f) return;

        float hoursPerSecond = 24f / (realMinutesPerDay * 60f);
        currentHour += deltaSeconds * hoursPerSecond;

        while (currentHour >= 24f)
        {
            currentHour -= 24f;
            currentDayIndex = (currentDayIndex + 1) % Mathf.Max(dayNames.Length, 1);
        }
    }

    private void Refresh()
    {
        if (clockLabel != null) clockLabel.text = FormatClock(currentHour);
        if (activityLabel != null) activityLabel.text = ResolveActivityLabel(currentHour);
        if (dayLabel != null) dayLabel.text = dayNames.Length > 0 ? dayNames[currentDayIndex] : string.Empty;
    }

    private static string FormatClock(float hour24)
    {
        int totalMinutes = Mathf.FloorToInt(hour24 * 60f) % (24 * 60);
        if (totalMinutes < 0) totalMinutes += 24 * 60;

        int hour = totalMinutes / 60;
        int minute = totalMinutes % 60;
        string meridiem = hour >= 12 ? "PM" : "AM";

        int hour12 = hour % 12;
        if (hour12 == 0) hour12 = 12;

        return $"{hour12}:{minute:00} {meridiem}";
    }

    private string ResolveActivityLabel(float hour24)
    {
        foreach (ActivityWindow window in schedule)
        {
            if (hour24 >= window.startHour && hour24 < window.endHour)
                return window.label;
        }
        return fallbackActivityLabel;
    }

    /// <summary>Jump directly to a given in-game hour (0-24), e.g. for scripted events.</summary>
    public void SetHour(float hour24) => currentHour = Mathf.Repeat(hour24, 24f);

    /// <summary>Jump directly to a day index into the configured day names.</summary>
    public void SetDayIndex(int index)
    {
        if (dayNames.Length == 0) return;
        currentDayIndex = ((index % dayNames.Length) + dayNames.Length) % dayNames.Length;
    }
}
