using UnityEngine;
using TMPro;
using System;
using UnityEngine.Events;
using System.Collections.Generic;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class DailyStreakMaster : MonoBehaviour
{
    public TextMeshPro StreakDisplay;
    public TextMeshPro TimerDisplay;
    public bool ResetOnMissedDay = true;
    public string HandTag = "HandTag";

    [Header("Events")]
    public List<HitStreak> OnHitStreak;
    public UnityEvent OnIncreaseStreak;

    [System.Serializable]
    public class HitStreak
    {
        public int StreakNumber;
        public UnityEvent Event;
    }

    int streak = 0;

    private void Awake()
    {
        streak = PlayerPrefs.GetInt("DailyStreak", 0);
        string lastCollected = PlayerPrefs.GetString("LastCollectedDate", "");

        if (!string.IsNullOrEmpty(lastCollected))
        {
            DateTime lastTime = DateTime.Parse(lastCollected);
            if ((DateTime.Now - lastTime).TotalHours > 48 && ResetOnMissedDay)
            {
                streak = 0;
                PlayerPrefs.SetInt("DailyStreak", streak);
                PlayerPrefs.Save();
            }
        }

        StreakDisplay.text = streak.ToString();

        foreach (var s in OnHitStreak)
        {
            if (streak >= s.StreakNumber && s.Event != null)
            {
                s.Event.Invoke();
            }
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(HandTag))
        {
            TryIncrease();
        }
    }

    public void TryIncrease()
    {
        string lastCollected = PlayerPrefs.GetString("LastCollectedDate", "");
        string today = DateTime.Now.ToString("yyyy-MM-dd");

        if (lastCollected != today)
        {
            IncreaseStreak(today);
        }
    }

    public void IncreaseStreak(string today)
    {
        streak++;
        PlayerPrefs.SetInt("DailyStreak", streak);
        PlayerPrefs.SetString("LastCollectedDate", today);
        PlayerPrefs.Save();

        if (OnIncreaseStreak != null)
        {
            OnIncreaseStreak.Invoke();
        }

        foreach (var s in OnHitStreak)
        {
            if (streak >= s.StreakNumber && s.Event != null)
            {
                s.Event.Invoke();
            }
        }

        StreakDisplay.text = streak.ToString();
    }

    private void Update()
    {
        string lastCollected = PlayerPrefs.GetString("LastCollectedDate", "");
        if (!string.IsNullOrEmpty(lastCollected))
        {
            DateTime lastTime = DateTime.Parse(lastCollected);
            DateTime nextAvailable = lastTime.AddHours(24);
            TimeSpan remaining = nextAvailable - DateTime.Now;

            if (remaining.TotalSeconds > 0)
                TimerDisplay.text = $"{remaining.Hours:D2}h {remaining.Minutes:D2}m";
            else
                TimerDisplay.text = "Available!";
        }
    }

}

#if UNITY_EDITOR

[CustomEditor(typeof(DailyStreakMaster))]
public class DailyStreakMasterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DailyStreakMaster s = (DailyStreakMaster)target;
        base.OnInspectorGUI();

        if (GUILayout.Button("Test Increase streak"))
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            s.IncreaseStreak(today);
        }
        GUILayout.Label("This one is like in game, only one time per day");
        if (GUILayout.Button("Test Try Increase streak"))
        {
            s.TryIncrease();
        }
    }
}


#endif