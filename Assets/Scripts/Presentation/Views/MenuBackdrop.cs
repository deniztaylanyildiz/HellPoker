using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// The bottom of Hell behind the title screen and its sub-screens (rules, settings, records): the animated menu art,
    /// full screen, opaque so no click reaches the table underneath. Falls back to the stone wall, then to a flat colour.
    /// </summary>
    public static class MenuBackdrop
    {
        public static SpriteFrameAnimator Create(Transform screen, MenuBackdropLibrary library = null)
        {
            Image image = UiFactory.CreateImage("Background", screen, Palette.Night);
            image.rectTransform.Stretch();
            image.raycastTarget = true;

            var animator = image.gameObject.AddComponent<SpriteFrameAnimator>();
            animator.FallbackColor = Palette.Night;
            animator.Play((library ?? UiArt.MenuBackdrop).Get());
            return animator;
        }
    }
}
