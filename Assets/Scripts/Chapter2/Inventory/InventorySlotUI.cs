using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>ช่องไอเท็ม 1 ช่อง (Button + Image icon + TMP_Text ชื่อ/เลขปุ่ม ไม่บังคับ)</summary>
public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] Image icon;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text hotkeyText;

    public bool IsEmpty { get; private set; } = true;

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (IsEmpty) Clear();
    }

    public void Setup(ItemData item, int index, Action<ItemData> onClick)
    {
        IsEmpty = false;

        if (icon != null)
        {
            icon.sprite = item.icon;
            icon.enabled = item.icon != null;
            icon.preserveAspect = true;
        }
        if (nameText != null) nameText.text = item.displayName;
        if (hotkeyText != null) hotkeyText.text = (index + 1).ToString();

        if (button != null)
        {
            button.interactable = true;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick(item));
        }
    }

    /// <summary>ทำให้เป็นช่องว่าง (ยังเห็นกรอบสีปกติ แต่ไม่มีไอคอน และคลิกแล้วไม่เกิดอะไร)</summary>
    public void Clear()
    {
        IsEmpty = true;
        if (icon != null) icon.enabled = false;
        if (nameText != null) nameText.text = "";
        if (hotkeyText != null) hotkeyText.text = "";
        if (button != null)
        {
            // ไม่ปิด interactable เพราะ Unity จะย้อมกรอบเป็นสีจาง (Disabled Color)
            // ช่องว่างไม่มี Listener อยู่แล้ว คลิกไปก็ไม่เกิดอะไร
            button.onClick.RemoveAllListeners();
            button.interactable = true;
        }
    }
}
