using DG.Tweening;
using UnityEngine;

namespace GameAnimation
{
    public sealed class WorkAnim : IAnim
    {
        private const float DefaultPulseDuration = 0.18f;
        private const float SquashXMultiplier = 1.08f;
        private const float SquashYMultiplier = 0.92f;

        private readonly float pulseDuration;

        public AnimType AnimType => AnimType.Work;

        public WorkAnim(float pulseDuration = DefaultPulseDuration)
        {
            this.pulseDuration = Mathf.Max(0.01f, pulseDuration);
        }

        public Tween CreateTween(AnimContext context)
        {
            Transform target = context.Transform;
            if (!target)
                return null;

            Vector3 startScale = GetOrientedScale(target.localScale, context.FlipX);
            Vector3 squashScale = new Vector3(
                startScale.x * SquashXMultiplier,
                startScale.y * SquashYMultiplier,
                startScale.z);

            target.localScale = startScale;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(target.DOScale(squashScale, pulseDuration)
                .SetEase(Ease.InOutSine));
            sequence.Append(target.DOScale(startScale, pulseDuration)
                .SetEase(Ease.InOutSine));
            sequence.SetLoops(-1, LoopType.Restart);
            sequence.SetLink(target.gameObject, LinkBehaviour.KillOnDestroy);
            sequence.OnKill(() => ResetTarget(target, startScale));
            return sequence;
        }

        private static Vector3 GetOrientedScale(Vector3 scale, bool flipX)
        {
            scale.x = Mathf.Abs(scale.x) * (flipX ? -1f : 1f);
            return scale;
        }

        private static void ResetTarget(Transform target, Vector3 scale)
        {
            if (target)
                target.localScale = scale;
        }
    }
}
