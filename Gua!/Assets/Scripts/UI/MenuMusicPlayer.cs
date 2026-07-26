using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrogCamp.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class MenuMusicPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioSource musicSource;

        private const string MasterVolumeKey = "FrogCamp.MasterVolume";
        private const string MusicVolumeKey = "FrogCamp.MusicVolume";
        private static MenuMusicPlayer instance;

        public void Configure(AudioClip clip, AudioSource source)
        {
            menuMusic = clip;
            musicSource = source;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;

            if (musicSource == null) musicSource = GetComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            if (musicSource.clip == null) musicSource.clip = menuMusic;

            AudioListener.volume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
            musicSource.volume = PlayerPrefs.GetFloat(MusicVolumeKey, 0.3f);
            if (IsMenuScene(SceneManager.GetActiveScene().name) &&
                musicSource.clip != null && !musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsMenuScene(scene.name)) return;
            musicSource.Stop();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }

        private static bool IsMenuScene(string sceneName)
        {
            return sceneName == CampScenes.Initial ||
                sceneName == CampScenes.Start ||
                sceneName == CampScenes.Lobby;
        }
    }
}
