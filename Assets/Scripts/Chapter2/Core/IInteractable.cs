/// <summary>วัตถุที่ผู้เล่นกด E ได้</summary>
public interface IInteractable
{
    /// <summary>ข้อความที่แสดงตอนเล็งโดน เช่น "[E] หยิบหวี"</summary>
    string Prompt { get; }

    /// <summary>false = ตอนนี้ยังกดไม่ได้ (จะไม่แสดงข้อความ)</summary>
    bool CanInteract { get; }

    void Interact();
}
