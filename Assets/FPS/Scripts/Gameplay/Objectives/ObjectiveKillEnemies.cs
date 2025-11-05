using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    // Survive mode: show a running timer instead of kill counter.
    public class ObjectiveSurvive : Objective
    {
        [Tooltip("0이면 끝없이 생존(클리어 조건 없음). 0보다 크면 해당 시간 생존 시 목표 완료.")]
        public float SecondsToSurvive = 0f;

        [Tooltip("타이머를 Time.timeScale의 영향을 받게 할지 여부 (일시정지 시 멈춤 권장)")]
        public bool UseScaledTime = true;

        float m_Elapsed;
        bool m_IsRunning = true;

        protected override void Start()
        {
            base.Start();

            EventManager.AddListener<PlayerDeathEvent>(OnPlayerDeath);

            if (string.IsNullOrEmpty(Title))
                Title = SecondsToSurvive > 0f
                    ? $"Survive for {FormatTime(SecondsToSurvive)}"
                    : "Survive as long as possible";

            if (string.IsNullOrEmpty(Description))
                Description = GetUpdatedCounterAmount();
        }

        void Update()
        {
            if (IsCompleted || !m_IsRunning)
                return;

            // 스케일드/언스케일드 시간 선택
            m_Elapsed += UseScaledTime ? Time.deltaTime : Time.unscaledDeltaTime;
            SurvivalTimer.Time = m_Elapsed;

            // 매 프레임 HUD 타이머 업데이트
            UpdateObjective(string.Empty, GetUpdatedCounterAmount(), string.Empty);

            // 목표 시간이 설정된 경우, 달성 체크
            if (SecondsToSurvive > 0f && m_Elapsed >= SecondsToSurvive)
            {
                m_IsRunning = false;
                CompleteObjective(string.Empty, GetUpdatedCounterAmount(),
                    "Objective complete : " + Title);
            }
        }

        void OnPlayerDeath(PlayerDeathEvent evt)
        {
            if (IsCompleted) return;

            m_IsRunning = false;

            // 무한 생존 모드: 최종 기록만 남기고 종료 알림
            // 제한 시간 생존 모드: 죽으면 실패로 처리하고 종료 알림 (FailObjective가 없다면 알림만)
#if UNITY_FPS_FAILOBJECTIVE_EXISTS
            if (SecondsToSurvive > 0f)
                FailObjective("You died", GetUpdatedCounterAmount(), "Objective failed : " + Title);
            else
                UpdateObjective(string.Empty, $"Final time  {FormatTime(m_Elapsed)}", "Run ended");
#else
            string note = SecondsToSurvive > 0f ? "Objective failed : " + Title : "Run ended";
            UpdateObjective(string.Empty, $"Final time  {FormatTime(m_Elapsed)}", note);
#endif
        }

        string GetUpdatedCounterAmount()
        {
            // HUD에 표시할 본문: "mm:ss.mmm / (목표시간)" 또는 "mm:ss.mmm"
            if (SecondsToSurvive > 0f)
                return $"{FormatTime(m_Elapsed)} / {FormatTime(SecondsToSurvive)}";
            return $"{FormatTime(m_Elapsed)}";
        }

        static string FormatTime(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            int millis = Mathf.FloorToInt((seconds - Mathf.Floor(seconds)) * 1000f);
            return $"{mins:00}:{secs:00}.{millis:000}";
        }

        void OnDestroy()
        {
            EventManager.RemoveListener<PlayerDeathEvent>(OnPlayerDeath);
        }
    }
}
