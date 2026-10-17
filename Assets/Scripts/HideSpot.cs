
using UnityEngine;

public class HideSpot : MonoBehaviour
{
    [Header("Hide Settings")]
    public Transform hidePosition;

    [Header("Prompt")]
    public string hideMessage = "[E] ซ่อน";
    public string exitMessage = "[E] ออก";

    public Transform HidePosition => hidePosition;

    public string HideMessage => hideMessage;
    public string ExitMessage => exitMessage;
}
