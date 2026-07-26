using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FrogCamp.UI
{
    /// <summary>
    /// Automatically gives every Unity UI Button the same click sound.
    /// </summary>
    public sealed class UiButtonClickSound : MonoBehaviour
    {
        private const string ClickSoundResource = "Sound/点击音效";
        private const string SfxVolumeKey = "FrogCamp.SfxVolume";
        private const float RefreshInterval = 0.5f;

        private static UiButtonClickSound instance;

        private readonly HashSet<Button> registeredButtons = new HashSet<Button>();
        private AudioClip clickSound;
        private AudioSource audioSource;
        private float nextRefreshTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (instance != null) return;

            GameObject soundObject = new GameObject("UI Button Click Sound");
            instance = soundObject.AddComponent<UiButtonClickSound>();
            DontDestroyOnLoad(soundObject);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            clickSound = Resources.Load<AudioClip>(ClickSoundResource);
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            RegisterAllButtons();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshTime) return;
            nextRefreshTime = Time.unscaledTime + RefreshInterval;
            RegisterAllButtons();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            registeredButtons.RemoveWhere(button => button == null);
            RegisterAllButtons();
        }

        private void RegisterAllButtons()
        {
            Button[] buttons = Object.FindObjectsOfType<Button>(true);
            foreach (Button button in buttons)
            {
                if (!registeredButtons.Add(button)) continue;
                button.onClick.AddListener(PlayClickSound);
            }
        }

        private void PlayClickSound()
        {
            if (clickSound == null || audioSource == null) return;
            audioSource.volume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
            audioSource.PlayOneShot(clickSound);
        }

        private void OnDestroy()
        {
            if (instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }
}
