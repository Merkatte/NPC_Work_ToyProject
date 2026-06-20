namespace GameAnimation
{
    public interface IAnimPlayer
    {
        bool FlipX { get; }

        bool TryPlay(AnimType animType);
        bool TryPlay(AnimType animType, bool flipX);
        bool Stop(AnimType animType);
        void Stop();
    }
}
