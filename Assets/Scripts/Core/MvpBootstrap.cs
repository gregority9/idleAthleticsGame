using UnityEngine;

namespace TrackDynasty.Mvp03.Core
{
    public static class MvpBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Object.FindFirstObjectByType<GameManager>() != null) return;
            GameObject root = new GameObject("TrackDynasty_MVP03");
            root.AddComponent<GameManager>();
        }
    }
}
