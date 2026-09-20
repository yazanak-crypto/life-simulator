namespace LifeSimulator
{
    public interface IInteractable
    {
        string Prompt { get; }
        void Interact(PlayerInteraction player);
    }
}
