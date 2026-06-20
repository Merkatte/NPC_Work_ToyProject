using System.Collections.Generic;

namespace GameAnimation
{
    public sealed class AnimSet : IAnimSet
    {
        private readonly Dictionary<AnimType, IAnim> _animations = new Dictionary<AnimType, IAnim>();

        public AnimSet()
        {
            Register(new MoveAnim());
            Register(new WorkAnim());
        }

        public bool TryGetAnim(AnimType animType, out IAnim anim)
        {
            return _animations.TryGetValue(animType, out anim);
        }

        private void Register(IAnim anim)
        {
            if (anim != null)
                _animations[anim.AnimType] = anim;
        }
    }
}
