using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonJuice : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler,
    ISelectHandler, IDeselectHandler
{
    [SerializeField] float hoverScale = 1.04f;
    [SerializeField] float pressScale = 0.97f;
    [SerializeField] float flickerAmount = 0.1f;
    [SerializeField] AudioSource sfx;
    [SerializeField] AudioClip hoverClip;
    [SerializeField] AudioClip clickClip;

    Button button;
    Image image;
    Vector3 baseScale;
    bool hover, pressed;
    float seed;

    void Awake()
    {
        button = GetComponent<Button>();
        image = GetComponent<Image>();
        baseScale = transform.localScale;
        seed = Random.value * 10f;
    }

    void OnDisable()
    {
        hover = pressed = false;
        transform.localScale = baseScale;
        if (image) image.color = Color.white;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;     // ใช้ได้ตอนหยุดเกม
        bool active = button.interactable;

        float s = 1f;
        if (active && pressed) s = pressScale;
        else if (active && hover) s = hoverScale;
        transform.localScale = Vector3.Lerp(transform.localScale, baseScale * s, dt * 18f);

        // แสงสั่นเหมือนไฟตะเกียง เฉพาะตอน Hover
        float b = 1f;
        if (active && hover)
            b = 1f - flickerAmount * Mathf.PerlinNoise(Time.unscaledTime * 9f, seed);
        image.color = new Color(b, b, b, 1f);
    }

    void SetHover(bool value)
    {
        if (value && !hover && sfx && hoverClip) sfx.PlayOneShot(hoverClip);
        hover = value;
    }

    public void OnPointerEnter(PointerEventData e) => SetHover(true);
    public void OnPointerExit(PointerEventData e) { SetHover(false); pressed = false; }
    public void OnPointerDown(PointerEventData e)
    {
        pressed = true;
        if (sfx && clickClip && button.interactable) sfx.PlayOneShot(clickClip);
    }
    public void OnPointerUp(PointerEventData e) => pressed = false;
    public void OnSelect(BaseEventData e) => SetHover(true);
    public void OnDeselect(BaseEventData e) => SetHover(false);
}