namespace GameAnimation
{
    public interface IAnimSet
    {
        bool TryGetAnim(AnimType animType, out IAnim anim);
    }
}
