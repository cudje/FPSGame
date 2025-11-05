using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 플레이어 참조 없이 오직 '적 소환'에만 집중한 스포너.
/// - Rule 0 : Ring 모드 → 중심 Transform 기준 반지름 40 원의 둘레에 랜덤 소환
/// - Rule 1 : Points 모드 → 미리 배치한 8개 스폰 포인트 중 랜덤 소환
/// - 각 Rule은 weight로 소환 확률 조절 (합이 1일 필요 없음, 상대 비율)
/// </summary>
public class FlexibleEnemySpawner : MonoBehaviour
{
    public enum SpawnMode { Ring, Points }

    [System.Serializable]
    public class SpawnRule
    {
        [Header("Common")]
        [Tooltip("소환할 프리팹")]
        public GameObject prefab;

        [Tooltip("스폰 방식: Ring(원둘레) / Points(지정 포인트)")]
        public SpawnMode mode = SpawnMode.Ring;

        [Tooltip("이 규칙이 선택될 상대 확률(가중치). 0 이하면 선택되지 않음")]
        public float weight = 1f;

        [Header("Ring Spawn (for prefab 0)")]
        [Tooltip("원 중심이 되는 Transform")]
        public Transform ringCenter;

        [Tooltip("원 반지름")]
        public float ringRadius = 40f;

        [Tooltip("반지름에 적용되는 작은 랜덤 흔들림(자연스러운 배치용)")]
        public float ringRadiusJitter = 0f;

        [Header("Point Spawn (for prefab 1)")]
        [Tooltip("지정 스폰 포인트들(예: 8개)")]
        public List<Transform> spawnPoints = new();

        [Header("NavMesh")]
        [Tooltip("NavMesh.SamplePosition 검색 반경 (0이면 실패하기 쉬움, 4~8 권장)")]
        public float navSampleDistance = 6f;
    }

    [Header("Spawn Rules (프리팹별 스폰 규칙)")]
    public List<SpawnRule> rules = new List<SpawnRule>();

    [Header("Spawn Timing")]
    [Tooltip("스폰 간격(초)")]
    public float spawnInterval = 2f;

    [Tooltip("동시에 살아 있을 수 있는 적의 최대 수")]
    public int maxAlive = 30;

    private readonly List<GameObject> _alive = new();
    private float _timer;

    void Update()
    {
        _timer += Time.deltaTime;

        if (_timer >= spawnInterval)
        {
            _timer = 0f;
            TrySpawn();
        }
    }

    void TrySpawn()
    {
        // 현재 생존 중 객체 정리
        _alive.RemoveAll(x => x == null);
        if (_alive.Count >= maxAlive) return;

        if (!TryPickRule(out var rule)) return;

        GameObject spawned = null;

        switch (rule.mode)
        {
            case SpawnMode.Ring:
                spawned = SpawnOnRing(rule);
                break;
            case SpawnMode.Points:
                spawned = SpawnOnPoints(rule);
                break;
        }

        if (spawned != null)
            _alive.Add(spawned);
    }

    /// <summary>
    /// 가중치 기반으로 규칙 하나를 선택
    /// </summary>
    bool TryPickRule(out SpawnRule rule)
    {
        rule = null;
        if (rules == null || rules.Count == 0) return false;

        float totalWeight = 0f;
        for (int i = 0; i < rules.Count; i++)
            totalWeight += Mathf.Max(0f, rules[i].weight);

        // 가중치가 모두 0 이하라면 균등 랜덤
        if (totalWeight <= 0f)
        {
            rule = rules[Random.Range(0, rules.Count)];
            return true;
        }

        float roll = Random.value * totalWeight;
        float acc = 0f;
        for (int i = 0; i < rules.Count; i++)
        {
            acc += Mathf.Max(0f, rules[i].weight);
            if (roll <= acc)
            {
                rule = rules[i];
                return true;
            }
        }

        rule = rules[^1];
        return true;
    }

    GameObject SpawnOnRing(SpawnRule rule)
    {
        if (rule.prefab == null || rule.ringCenter == null) return null;

        // 원둘레 상의 임의 각도/반지름 계산
        float radius = Mathf.Max(0.1f, rule.ringRadius + Random.Range(-rule.ringRadiusJitter, rule.ringRadiusJitter));
        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        Vector3 pos = rule.ringCenter.position + offset;

        // NavMesh 위로 스냅
        if (NavMesh.SamplePosition(pos, out var hit, Mathf.Max(0.1f, rule.navSampleDistance), NavMesh.AllAreas))
            return Instantiate(rule.prefab, hit.position, Quaternion.identity);

        return null;
    }

    GameObject SpawnOnPoints(SpawnRule rule)
    {
        if (rule.prefab == null || rule.spawnPoints == null || rule.spawnPoints.Count == 0) return null;

        Transform p = rule.spawnPoints[Random.Range(0, rule.spawnPoints.Count)];
        if (!p) return null;

        if (NavMesh.SamplePosition(p.position, out var hit, Mathf.Max(0.1f, rule.navSampleDistance), NavMesh.AllAreas))
            return Instantiate(rule.prefab, hit.position, p.rotation);

        return null;
    }
}
