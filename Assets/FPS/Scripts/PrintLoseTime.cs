using UnityEngine;
using TMPro;
using System;

public class PrintLoseTime : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // TextMeshProUGUI 컴포넌트 가져와서 Text에 SurviveTimeManager.time 출력
        // You LOSE
        // Survive Time
        // 00 : 00
        TextMeshProUGUI text = GetComponent<TextMeshProUGUI>();
        TimeSpan t = TimeSpan.FromSeconds(SurvivalTimer.Time);
        string timeText = $"{t.Minutes:00}:{t.Seconds:00}";
        text.text = $"You LOSE\nSurvive Time\n{timeText}";
    }
}
