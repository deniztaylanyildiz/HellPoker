using HellPoker.Presentation.Abstractions;
using UnityEngine;

namespace HellPoker.Presentation
{
    /// <summary>The player's mail app and clipboard (Unity's).</summary>
    public sealed class UnityLinks : IExternalLinks
    {
        public bool Open(string url)
        {
            if (Application.isBatchMode) return false;   // the tests: no mail app is opened
            try
            {
                Application.OpenURL(url);
                return true;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("Hell Poker: the mail app could not be opened: " + exception.Message);
                return false;
            }
        }

        public void Copy(string text) => GUIUtility.systemCopyBuffer = text ?? "";
    }
}