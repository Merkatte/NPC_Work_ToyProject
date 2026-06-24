using UnityEngine;

// 적 탐지 공용 Physics 스캐너.
// SeekAction과 PatrolAction이 각자 인스턴스를 소유해 공유 가변 상태 없이 사용한다.
// Physics2D.OverlapCircle(ContactFilter2D) API를 사용해 레거시 NonAlloc 경고를 피한다.
public class EnemyScanner
{
    private readonly Collider2D[] _buffer;

    public EnemyScanner(int bufferSize = 10)
    {
        _buffer = new Collider2D[bufferSize];
    }

    // origin 주변 radius 안에서 mask 레이어의 가장 가까운 살아있는 IDamageable을 찾는다.
    // 발견하면 true와 함께 target, targetTransform을 out으로 반환한다.
    public bool TryFindNearest(
        Vector2 origin,
        float radius,
        LayerMask mask,
        out IDamageable target,
        out Transform targetTransform)
    {
        target = null;
        targetTransform = null;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(mask);
        filter.useTriggers = true;

        int count = Physics2D.OverlapCircle(origin, radius, filter, _buffer);

        float nearestSqDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider2D col = _buffer[i];
            if (!col)
                continue;

            IDamageable candidate = col.GetComponent<IDamageable>();
            if (candidate == null || !candidate.IsAlive)
                continue;

            float sqDist = ((Vector2)col.transform.position - origin).sqrMagnitude;
            if (sqDist >= nearestSqDist)
                continue;

            nearestSqDist = sqDist;
            target = candidate;
            targetTransform = col.transform;
        }

        return target != null;
    }
}
