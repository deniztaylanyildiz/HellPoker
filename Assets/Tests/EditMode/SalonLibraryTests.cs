using System;
using System.Collections.Generic;
using HellPoker.Presentation.Animation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HellPoker.Core.Tests
{
    public class SalonLibraryTests
    {
        private readonly List<Object> _created = new List<Object>();
        private Dictionary<string, Texture2D> _files;

        [SetUp]
        public void SetUp() => _files = new Dictionary<string, Texture2D>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _created)
                Object.DestroyImmediate(o);
            _created.Clear();
        }

        private Texture2D Strip(int frames, string name)
        {
            var texture = new Texture2D(frames * SalonLibrary.FrameWidth, 270) { name = name };
            _created.Add(texture);
            return texture;
        }

        private SalonLibrary Library() => new SalonLibrary(path => _files.TryGetValue(path, out Texture2D t) ? t : null);

        [Test]
        public void Hall_IsCutInto480PixelFrames_AndLoops()
        {
            _files["Backgrounds/mammon/normal"] = Strip(3, "normal");

            SpriteClip clip = Library().Get("mammon", SalonMode.Normal);

            Assert.AreEqual(3, clip.Frames.Length);
            Assert.AreEqual(new Rect(960, 0, 480, 270), clip.Frames[2].rect);
            Assert.IsTrue(clip.Loop);
        }

        [Test]
        public void EachMode_UsesItsOwnVariant()
        {
            _files["Backgrounds/belial/normal"] = Strip(3, "normal");
            _files["Backgrounds/belial/soul"] = Strip(2, "soul");
            _files["Backgrounds/belial/hell"] = Strip(1, "hell");

            SalonLibrary library = Library();

            Assert.AreEqual("soul", library.Get("belial", SalonMode.Soul).Frames[0].texture.name);
            Assert.AreEqual("hell", library.Get("belial", SalonMode.Hell).Frames[0].texture.name);
        }

        [Test]
        public void MissingVariant_FallsBackToTheHallsNormalLook()
        {
            _files["Backgrounds/lilith/normal"] = Strip(3, "normal");

            Assert.AreEqual("normal", Library().Get("lilith", SalonMode.Soul).Frames[0].texture.name);
        }

        [Test]
        public void MissingHall_FallsBackToTheStoneWall_BurningInTheFinalStretch()
        {
            _files[SalonLibrary.StoneWall] = Strip(1, "stone");
            _files[SalonLibrary.BurningStoneWall] = Strip(1, "burning");
            SalonLibrary library = Library();

            Assert.AreEqual("stone", library.Get("nobody", SalonMode.Normal).Frames[0].texture.name);
            Assert.AreEqual("stone", library.Get("nobody", SalonMode.Soul).Frames[0].texture.name);
            Assert.AreEqual("burning", library.Get("nobody", SalonMode.Hell).Frames[0].texture.name);
            Assert.AreEqual("stone", library.Get(null, SalonMode.Normal).Frames[0].texture.name);
        }

        [Test]
        public void NoArtAtAll_ReturnsNull_WithoutThrowing()
        {
            foreach (SalonMode mode in Enum.GetValues(typeof(SalonMode)))
                Assert.IsNull(Library().Get("mammon", mode));
        }

        [Test]
        public void ALoaderThatThrows_CountsAsMissing()
        {
            var library = new SalonLibrary(path => throw new InvalidOperationException("no disk"));

            Assert.IsNull(library.Get("mammon", SalonMode.Normal));
        }
    }
}
