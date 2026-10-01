using System;
using System.Collections;
using System.Collections.Generic;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace HellPoker.Core.Tests
{
    public class AnimationSequencerTests
    {
        private GameObject _host;
        private AnimationSequencer _sequencer;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("Sequencer");
            _sequencer = _host.AddComponent<AnimationSequencer>();
        }

        [TearDown]
        public void TearDown()
        {
            AnimationClock.Speed = 1f;
            Object.DestroyImmediate(_host);
        }

        /// <summary>A long animation: records its frames, and whether it was told to skip.</summary>
        private static IEnumerator Long(List<string> log, string name, int frames = 1000)
        {
            log.Add(name + " start");
            for (int i = 0; i < frames && !AnimationClock.IsSkipping; i++)
                yield return null;
            log.Add(name + " end");
        }

        [Test]
        public void Complete_RunsEverythingQueued_InOrder()
        {
            var log = new List<string>();
            _sequencer.Play(Long(log, "deal"));
            _sequencer.Do(() => log.Add("message"));
            _sequencer.Wait(60f);
            _sequencer.Play(Long(log, "flip"));

            _sequencer.Complete();

            CollectionAssert.AreEqual(new[] { "deal start", "deal end", "message", "flip start", "flip end" }, log);
            Assert.IsFalse(_sequencer.IsBusy);
            Assert.IsFalse(AnimationClock.IsSkipping, "Skipping ends with the call.");
        }

        [Test]
        public void Do_RunsAtOnce_WhenIdle()
        {
            bool done = false;

            _sequencer.Do(() => done = true);

            Assert.IsTrue(done);
            Assert.IsFalse(_sequencer.IsBusy);
        }

        [Test]
        public void AStepThatThrows_IsDropped_AndTheQueueGoesOn()
        {
            var log = new List<string>();
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: broken step");
            _sequencer.Play(Long(log, "first"));
            _sequencer.Do(() => throw new InvalidOperationException("broken step"));
            _sequencer.Do(() => log.Add("after"));

            _sequencer.Complete();

            CollectionAssert.AreEqual(new[] { "first start", "first end", "after" }, log);
            Assert.IsFalse(_sequencer.IsBusy);
        }

        [Test]
        public void Speed_IsNeverZero()
        {
            AnimationClock.Speed = 0f;

            Assert.Greater(AnimationClock.Speed, 0f);
            Assert.AreEqual(1f, AnimationClock.Scaled(AnimationClock.Speed), 1e-5f);
        }
    }
}
