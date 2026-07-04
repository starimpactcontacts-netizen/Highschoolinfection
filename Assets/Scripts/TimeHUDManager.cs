using UnityEngine;
using TMPro;

/// <summary>
/// Drives the upper-right HUD: formatted clock, activity state, and day-of-week.
/// Fully self-contained. Advances an in-game clock and pushes formatted strings
/// to three TextMeshPro fields. No dependency on player or camera.
/// </summary>
public class TimeHUDManager : MonoBehaviour
{
    [Header("TMP References")]
    [SerializeField] private TMP_Text clockText;      // "7:10 AM"
    [SerializeField] private TMP_Text activityText;   // "Before School"
    [SerializeField] private TMP_Text dayText;        // "Monday"

    [Header("Clock Settings")]
    [Tooltip("In-game minutes that pass per real second.")]
    [SerializeField] private float minutesPerSecond = 1f;
    [SerializeField] private int startHour = 7;
    [SerializeField] private int startMinute = 10;

    [Header("Calendar")]
    [SerializeField] private string[] weekDays =
        { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };
    [SerializeField] private int startDayIndex = 0;

    // Internal clock state, tracked as total minutes since midnight.
    private float currentMinutes;
    private int dayIndex;

    private void Awake()
    {
        currentMinutes = (startHour * 60) + startMinute;
        dayIndex = Mathf.Clamp(startDayIndex, 0, weekDays.Length - 1);
    }

    private void Update()
    {
        AdvanceClock(Time.deltaTime);
        RefreshHUD();
    }

    private void AdvanceClock(float deltaTime)
    {
        currentMinutes += minutesPerSecond * deltaTime;

        // Roll over to next day at midnight.
        if (currentMinutes >= 24 * 60)
        {
            currentMinutes -= 24 * 60;
            dayIndex = (dayIndex + 1) % weekDays.Length;
        }
    }

    private void RefreshHUD()
    {
        if (clockText != null)
            clockText.text = FormatClock(currentMinutes);

        if (activityText != null)
            activityText.text = ResolveActivity(currentMinutes);

        if (dayText != null)
            dayText.text = weekDays[dayIndex];
    }

    /// <summary>Converts total minutes into a "7:10 AM" style 12-hour string.</summary>
    private string FormatClock(float totalMinutes)
    {
        int hour24 = Mathf.FloorToInt(totalMinutes / 60f) % 24;
        int minute = Mathf.FloorToInt(totalMinutes % 60f);

        string suffix = hour24 >= 12 ? "PM" : "AM";
        int hour12 = hour24 % 12;
        if (hour12 == 0) hour12 = 12;

        return $"{hour12}:{minute:00} {suffix}";
    }

    /// <summary>Maps the current time to a coarse activity label.</summary>
    private string ResolveActivity(float totalMinutes)
    {
        float hour = totalMinutes / 60f;

        if (hour < 8f)  return "Before School";
        if (hour < 13f) return "Class Time";
        if (hour < 14f) return "Lunch";
        if (hour < 16f) return "Class Time";
        if (hour < 18f) return "After School";
        return "Go Home";
    }

    // --- Public API for other systems, if you ever need it ---
    public void SetTime(int hour, int minute) => currentMinutes = (hour * 60) + minute;
    public void SetDay(int index) => dayIndex = Mathf.Clamp(index, 0, weekDays.Length - 1);
}
