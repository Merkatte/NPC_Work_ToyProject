using DG.Tweening;
using UnityEngine;

namespace GameAnimation
{
    public sealed class ActorAnimationController : IAnimPlayer
    {
        private readonly IAnimSet _animSet;
        private readonly Transform _visualRoot;

        private Tween _activeTween;
        private AnimType? _activeAnimType;

        public bool FlipX { get; private set; }

        public ActorAnimationController(IAnimSet animSet, Transform visualRoot)
        {
            _animSet = animSet;
            _visualRoot = visualRoot;
        }

        public bool TryPlay(AnimType animType)
        {
            return TryPlay(animType, FlipX);
        }

        public bool TryPlay(AnimType animType, bool flipX)
        {
            if (_animSet == null || !_visualRoot)
                return false;

            if (!_animSet.TryGetAnim(animType, out IAnim anim))
                return false;

            Stop();
            FlipX = flipX;

            _activeTween = anim.CreateTween(new AnimContext(_visualRoot, FlipX));
            if (_activeTween == null)
                return false;

            _activeAnimType = animType;
            return true;
        }

        public bool Stop(AnimType animType)
        {
            if (_activeAnimType != animType)
                return false;

            Stop();
            return true;
        }

        public void Stop()
        {
            Tween tween = _activeTween;
            _activeTween = null;
            _activeAnimType = null;
            tween?.Kill();
        }
    }
}
