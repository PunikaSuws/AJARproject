using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class MenuCreditsMusic : MonoBehaviour
{
    const string MainMenuScene = "MainMenu";
    const string CreditsScene = "Credits";

    static MenuCreditsMusic instance;

    [SerializeField] AudioClip music;

    AudioSource source;
    float baseVolume;
    bool invalidInstance;

    void Awake()
    {
        if (!IsMenuOrCredits(SceneManager.GetActiveScene().name) ||
            (instance != null && instance != this))
        {
            invalidInstance = true;
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        source = GetComponent<AudioSource>();
        baseVolume = source.volume;
        source.clip = music;
        source.loop = true;
        source.playOnAwake = false;
        ApplyVolume();

        if (music == null)
            Debug.LogWarning("Assign a background music clip to MenuCreditsMusic.", this);
        else
            source.Play();
    }

    void OnEnable()
    {
        if (invalidInstance) return;
        GameAudio.Changed += ApplyVolume;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        GameAudio.Changed -= ApplyVolume;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void ApplyVolume()
    {
        if (source != null) source.volume = baseVolume * GameAudio.MusicVolume;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsMenuOrCredits(scene.name)) Destroy(gameObject);
    }

    static bool IsMenuOrCredits(string sceneName)
    {
        return sceneName == MainMenuScene || sceneName == CreditsScene;
    }
}
