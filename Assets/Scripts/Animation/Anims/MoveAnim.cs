using DG.Tweening;
using UnityEngine;

namespace GameAnimation
{
    public sealed class MoveAnim : IAnim
    {
        private const float DefaultJumpHeight = 0.15f;
        private const float DefaultJumpDuration = 0.3f;
        private const float StretchXMultiplier = 0.94f;
        private const float StretchYMultiplier = 1.06f;
        private const float SquashXMultiplier = 1.06f;
        private const float SquashYMultiplier = 0.94f;

        private readonly float jumpHeight;
        private readonly float jumpDuration;

        public AnimType AnimType => AnimType.Move;

        public MoveAnim(
            float jumpHeight = DefaultJumpHeight,
            float jumpDuration = DefaultJumpDuration)
        {
            this.jumpHeight = Mathf.Max(0f, jumpHeight);
            this.jumpDuration = Mathf.Max(0.01f, jumpDuration);
        }

        public Tween CreateTween(AnimContext context)
        {
            Transform target = context.Transform;
            if (!target)
                return null;

            Vector3 startPosition = target.localPosition;
            Vector3 startScale = GetOrientedScale(target.localScale, context.FlipX);
            Vector3 stretchScale = new Vector3(
                startScale.x * StretchXMultiplier,
                startScale.y * StretchYMultiplier,
                startScale.z);
            Vector3 squashScale = new Vector3(
                startScale.x * SquashXMultiplier,
                startScale.y * SquashYMultiplier,
                startScale.z);
            float halfDuration = jumpDuration * 0.5f;

            target.localScale = startScale;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(target.DOLocalMoveY(startPosition.y + jumpHeight, halfDuration)
                .SetEase(Ease.OutQuad));
            sequence.Join(target.DOScale(stretchScale, halfDuration)
                .SetEase(Ease.OutQuad));
            sequence.Append(target.DOLocalMoveY(startPosition.y, halfDuration)
                .SetEase(Ease.InQuad));
            sequence.Join(target.DOScale(squashScale, halfDuration)
                .SetEase(Ease.InQuad));
            sequence.Append(target.DOScale(startScale, halfDuration * 0.5f)
                .SetEase(Ease.OutQuad));
            sequence.SetLoops(-1, LoopType.Restart);
            sequence.SetLink(target.gameObject, LinkBehaviour.KillOnDestroy);
            sequence.OnKill(() => ResetTarget(target, startPosition, startScale));
            return sequence;
        }

        private static Vector3 GetOrientedScale(Vector3 scale, bool flipX)
        {
            scale.x = Mathf.Abs(scale.x) * (flipX ? -1f : 1f);
            return scale;
        }

        private static void ResetTarget(Transform target, Vector3 position, Vector3 scale)
        {
            if (!target)
                return;

            target.localPosition = position;
            target.localScale = scale;
        }
    }
}
