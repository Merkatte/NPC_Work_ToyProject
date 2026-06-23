using UnityEngine;

// 가드·적 공통으로 부착하는 공격력 제공 컴포넌트.
// IAttackPower 계약을 구현하여 AttackAction에 주입된다.
public class AttackPower : MonoBehaviour, IAttackPower
{
    [SerializeField] private int _attackPower = 1;

    public int GetAttackPower() => _attackPower;
}
