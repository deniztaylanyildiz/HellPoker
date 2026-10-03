using System;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Ui
{
    /// <summary>
    /// A fixed label that follows the language: the Text is rewritten from its source whenever it is enabled and whenever
    /// <see cref="Lang.Changed"/> fires, so the screen changes language at once, without being rebuilt.
    /// Set up with <see cref="UiFactory.Localized"/>. Text that the presenters write (messages, the action button, the
    /// demon's speech) is not bound: the presenters rewrite it themselves when the language changes.
    /// </summary>
    public sealed class LocalizedText : MonoBehaviour
    {
        private Text _text;
        private Func<string> _source;
        private bool _listening;

        internal void Bind(Text text, Func<string> source)
        {
            _text = text;
            _source = source;
            Rewrite();
        }

        public void Rewrite()
        {
            if (_text != null && _source != null)
                _text.text = _source() ?? "";
        }

        private void OnEnable()
        {
            if (!_listening)
            {
                Lang.Changed += Rewrite;
                _listening = true;
            }
            Rewrite();
        }

        private void OnDisable() => StopListening();

        private void OnDestroy() => StopListening();

        private void StopListening()
        {
            if (!_listening) return;
            Lang.Changed -= Rewrite;
            _listening = false;
        }
    }
}
