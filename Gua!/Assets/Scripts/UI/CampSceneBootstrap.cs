using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrogCamp.UI
{
    public static class CampSceneBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BuildInitialScene()
        {
            Build(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Build(scene);
        }

        private static void Build(Scene scene)
        {
            if (GameObject.Find("CampRuntimeUI") != null) return;
            GameObject root = new GameObject("CampRuntimeUI");
            if (scene.name == CampScenes.Start) root.AddComponent<StartSceneController>();
            else if (scene.name == CampScenes.Lobby) root.AddComponent<LobbySceneController>();
            else if (scene.name == CampScenes.Game) root.AddComponent<GameSceneController>();
            else Object.Destroy(root);
        }
    }
}
