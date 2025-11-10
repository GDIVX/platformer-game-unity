using NUnit.Framework;
using Runtime.Combat;
using UnityEngine;

namespace Tests
{
    public class UnitHealthTests
    {
        private GameObject _go;
        private UnitHealth _health;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("UnitHealth_Test");
            _health = _go.AddComponent<UnitHealth>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void Damage_ToZero_TriggersDeathAndEvents()
        {
            int changeCount = 0;
            int lastCurrent = -1;
            int lastMax = -1;
            bool died = false;

            _health.OnHealthChanged.AddListener((cur, max) =>
            {
                changeCount++;
                lastCurrent = cur;
                lastMax = max;
            });
            _health.OnDied.AddListener(() => died = true);

            _health.ModifyHealth(-999);

            Assert.That(_health.CurrentHealth, Is.EqualTo(0));
            Assert.That(_health.IsAlive, Is.False);
            Assert.That(changeCount, Is.EqualTo(1));
            Assert.That(lastCurrent, Is.EqualTo(0));
            Assert.That(lastMax, Is.EqualTo(_health.MaxHealth));
            Assert.IsTrue(died);
        }

        [Test]
        public void Heal_WhileDead_IsIgnored()
        {
            _health.Kill();
            int changeCount = 0;
            _health.OnHealthChanged.AddListener((_, _) => changeCount++);

            _health.ModifyHealth(+25);

            Assert.That(_health.CurrentHealth, Is.EqualTo(0));
            Assert.That(changeCount, Is.EqualTo(0));
            Assert.That(_health.IsAlive, Is.False);
        }

        [Test]
        public void Revive_FullRestoration_AndEventRaised()
        {
            _health.Kill();
            bool revived = false;
            int changeCount = 0;
            int lastCurrent = -1;

            _health.OnRevived.AddListener(() => revived = true);
            _health.OnHealthChanged.AddListener((cur, _) =>
            {
                changeCount++;
                lastCurrent = cur;
            });

            _health.Revive();

            Assert.IsTrue(revived);
            Assert.That(_health.IsAlive, Is.True);
            Assert.That(_health.CurrentHealth, Is.EqualTo(_health.MaxHealth));
            Assert.That(changeCount, Is.EqualTo(1));
            Assert.That(lastCurrent, Is.EqualTo(_health.MaxHealth));
        }

        [Test]
        public void SetMaxHealth_UpdatesValues_AndMaintainsState()
        {
            int changeCount = 0;
            int diedCount = 0;

            _health.OnHealthChanged.AddListener((_, _) => changeCount++);
            _health.OnDied.AddListener(() => diedCount++);

            _health.SetMaxHealth(50);
            Assert.That(_health.MaxHealth, Is.EqualTo(50));
            Assert.That(_health.CurrentHealth, Is.EqualTo(50));
            Assert.That(changeCount, Is.EqualTo(1));
            Assert.That(diedCount, Is.EqualTo(0));

            _health.Kill();
            changeCount = 0;
            _health.SetMaxHealth(5);

            Assert.That(_health.MaxHealth, Is.EqualTo(5));
            Assert.That(_health.CurrentHealth, Is.EqualTo(0));
            Assert.That(_health.IsAlive, Is.False);
            Assert.That(changeCount, Is.EqualTo(1));
            Assert.That(diedCount, Is.EqualTo(1));
        }
    }
}
