using DG.Tweening;

namespace GameAnimation
{
    public interface IAnim
    {
        AnimType AnimType { get; }

        Tween CreateTween(AnimContext context);
    }
}
