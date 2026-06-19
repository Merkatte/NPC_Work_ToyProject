using UnityEngine;

namespace GameAnimation
{
    public readonly struct AnimContext
    {
        public Transform Transform { get; }
        public bool FlipX { get; }

        public AnimContext(Transform transform, bool flipX = false)
        {
            Transform = transform;
            FlipX = flipX;
        }
    }
}
