// GEÇİCİ TEST KISAYOLU'nun testi. Kısayolu yansımayla bulur: DevJumpShortcut.cs silinince derleme bozulmaz, bu testler kendiliğinden
// atlanır (Ignore); istersen bu dosyayı da sil.

using System;
using HellPoker.Core.Dealers;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>Demo 3.4.1's temporary F9: only a developer's build has it; it goes to the four demons in order.</summary>
    public class DevJumpShortcutTests
    {
        private static Type Shortcut => Type.GetType("HellPoker.Presentation.DevJumpShortcut, HellPoker.Presentation");

        private static bool Allowed(bool debugBuild, bool editor, string devValue) =>
            (bool)Shortcut.GetMethod("Allowed").Invoke(null, new object[] { debugBuild, editor, devValue });

        [SetUp]
        public void RequireIt()
        {
            if (Shortcut == null) Assert.Ignore("The temporary F9 is gone (DevJumpShortcut.cs deleted).");
        }

        [Test]
        public void APlayersBuild_HasNoF9_ADevelopersHasIt()
        {
            Assert.IsFalse(Allowed(false, false, null), "a player's build: nothing");
            Assert.IsFalse(Allowed(false, false, "0"));
            Assert.IsTrue(Allowed(true, false, null), "a development build");
            Assert.IsTrue(Allowed(false, true, null), "the editor");
            Assert.IsTrue(Allowed(false, false, "1"), "HELLPOKER_DEV=1");
        }

        [Test]
        public void TheFourTargets_AreTheChaptersDemonsAndLucifer()
        {
            CollectionAssert.AreEqual(new[] { DealerRoster.MammonId, DealerRoster.BelialId, DealerRoster.LilithId, DealerRoster.LuciferId },
                (string[])Shortcut.GetField("Targets").GetValue(null));
        }
    }
}
