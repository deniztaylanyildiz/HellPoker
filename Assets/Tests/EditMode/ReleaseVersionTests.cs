using HellPoker.Core.Game;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>The version players see: a demo build reads "Demo 1.0" (menu, run log, the zip's notes), its zip "HellPoker-Demo-1.0".</summary>
    public class ReleaseVersionTests
    {
        [Test]
        public void ADemoBuild_ReadsDemo_AndItsFilesAreNamedSo_AnOtherBuildKeepsItsNumber()
        {
            Assert.AreEqual("Demo 1.0", ReleaseVersion.Display("1.0.0-demo"));
            Assert.AreEqual("Demo-1.0", ReleaseVersion.FileName("1.0.0-demo"));
            Assert.IsTrue(ReleaseVersion.IsDemo("1.0.0-demo"));
            Assert.AreEqual("v0.1.6", ReleaseVersion.Display("0.1.6"));
            Assert.AreEqual("0.1.6", ReleaseVersion.FileName("0.1.6"));
            Assert.IsFalse(ReleaseVersion.IsDemo("0.1.6"));
            Assert.AreEqual("", ReleaseVersion.Display(""));
        }
    }
}