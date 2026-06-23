using UnityEngine;

// selector가 소유하고 SeekAction에 주입하는 핸드오프 홀더.
// 외부(적) 타깃 정보가 WorkerActionContext로 새지 않도록 selector↔action 경계에서 전달을 담당한다.
public class CombatTargetHolder
{
    public IDamageable Target { get; private set; }
    public Transform TargetTransform { get; private set; }

    public bool HasLiveTarget => Target != null && Target.IsAlive;

    public void SetTarget(IDamageable target, Transform targetTransform)
    {
        Target = target;
        TargetTransform = targetTransform;
    }

    public void Clear()
    {
        Target = null;
        TargetTransform = null;
    }
}
