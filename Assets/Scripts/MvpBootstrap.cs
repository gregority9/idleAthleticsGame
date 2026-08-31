using UnityEngine;

namespace TrackDynasty.Mvp02
{
    public static class MvpBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            MvpGame existing = Object.FindFirstObjectByType<MvpGame>();
            if (existing != null) return;

            GameObject root = new GameObject("TrackDynasty_MVP02");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<MvpGame>();
        }
    }
}
