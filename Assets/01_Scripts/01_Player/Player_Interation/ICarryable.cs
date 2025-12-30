public interface ICarryable : IInteractable // 옮길 수 있는 물체 전용 인터페이스
{
    void Drop(Player player);
}