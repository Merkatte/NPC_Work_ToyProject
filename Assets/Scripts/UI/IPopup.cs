namespace UI
{
    // Shared popup contract. Every UI panel opened through UIManager must implement this.
    public interface IPopup
    {
        bool IsOpen { get; }
        void Open();
        void Close();
    }
}
