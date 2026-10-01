using HellPoker.Presentation.Abstractions;
using UnityEngine;

namespace HellPoker.Presentation
{
    public sealed class UnityApplicationQuitter : IApplicationQuitter
    {
        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
