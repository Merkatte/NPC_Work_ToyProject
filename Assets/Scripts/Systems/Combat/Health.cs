using System;
using UnityEngine;

// Worker·적·건물·작물이 공통으로 부착하는 재사용 피해/사망 컴포넌트.
// IDamageable의 유일한 진입점이며, WorkerAI는 이 컴포넌트에 IDamageable 책임을 위임한다.
// 방어력·회피는 현재 직렬화 필드로 단순화한다(향후 별도 에셋/전략으로 분리 예정).
public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private int _maxHp = 10;
    [SerializeField] private int _defense = 0;
    [SerializeField] [Range(0f, 1f)] private float _dodgeChance = 0f;

    private int _currentHp;
    private bool _dead;

    public bool IsAlive => !_dead;
    public int CurrentHp => _currentHp;
    public int MaxHp => _maxHp;

    // 매니저 주입 경로: WorkerAIManager에서 스폰 후 호출하거나 직렬화 기본값을 그대로 사용.
    public void Init(int maxHp, int defense, float dodgeChance)
    {
        _maxHp = maxHp;
        _defense = defense;
        _dodgeChance = Mathf.Clamp01(dodgeChance);
        _currentHp = _maxHp;
        _dead = false;
    }

    // 중복 사망 방지를 위해 OnDied는 정확히 한 번만 발행된다.
    public event Action OnDied;

    private void Awake()
    {
        _currentHp = _maxHp;
    }

    public void TakeDamage(int amount)
    {
        if (_dead)
            return;

        if (_dodgeChance > 0f && UnityEngine.Random.value < _dodgeChance)
            return;

        int applied = Mathf.Max(1, amount - _defense);
        _currentHp -= applied;

        if (_currentHp <= 0)
        {
            _currentHp = 0;
            Die();
        }
    }

    private void Die()
    {
        if (_dead)
            return;

        _dead = true;
        OnDied?.Invoke();
    }
}
