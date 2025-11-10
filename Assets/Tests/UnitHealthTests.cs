using System.Reflection;
using NUnit.Framework;
using Runtime.Combat;
using Runtime.Combat.UI;
using UnityEngine;

namespace Tests.EditMode
{
    public class UnitHealthTests
    {
        private GameObject _gameObject;
        private UnitHealth _health;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("UnitHealth_Test_Object");
            _health = _gameObject.AddComponent<UnitHealth>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void ModifyHealth_DamageToZero_KillsAndRaisesEvents()
        {
            int changeCount = 0;
            int lastCurrent = -1;
            int lastMax = -1;
            bool died = false;

            _health.OnHealthChanged.AddListener((current, max) =>
            {
                changeCount++;
                lastCurrent = current;
                lastMax = max;
            });
            _health.OnDied.AddListener(() => died = true);

            _health.ModifyHealth(-150);

            Assert.That(_health.CurrentHealth, Is.EqualTo(0));
            Assert.That(_health.IsAlive, Is.False);
            Assert.That(changeCount, Is.EqualTo(1));
            Assert.That(lastCurrent, Is.EqualTo(0));
            Assert.That(lastMax, Is.EqualTo(_health.MaxHealth));
            Assert.IsTrue(died);
        }

        [Test]
        public void ModifyHealth_HealingWhileDead_IsIgnored()
        {
            _health.Kill();

            int changeCount = 0;
            _health.OnHealthChanged.AddListener((_, _) => changeCount++);

            _health.ModifyHealth(25);

            Assert.That(_health.CurrentHealth, Is.EqualTo(0));
            Assert.That(changeCount, Is.EqualTo(0));
            Assert.That(_health.IsAlive, Is.False);
        }

        [Test]
        public void Revive_RestoresFullHealth_AndRaisesEvents()
        {
            _health.Kill();

            bool revived = false;
            int changeCount = 0;
            int lastCurrent = -1;

            _health.OnRevived.AddListener(() => revived = true);
            _health.OnHealthChanged.AddListener((current, _) =>
            {
                changeCount++;
                lastCurrent = current;
            });

            _health.Revive();

            Assert.IsTrue(revived);
            Assert.That(_health.IsAlive, Is.True);
            Assert.That(_health.CurrentHealth, Is.EqualTo(_health.MaxHealth));
            Assert.That(changeCount, Is.EqualTo(1));
            Assert.That(lastCurrent, Is.EqualTo(_health.MaxHealth));
        }

        [Test]
        public void SetMaxHealth_AdjustsCurrentAndMaintainsState()
        {
            int changeCount = 0;
            _health.OnHealthChanged.AddListener((_, _) => changeCount++);

            _health.SetMaxHealth(50);

            Assert.That(_health.MaxHealth, Is.EqualTo(50));
            Assert.That(_health.CurrentHealth, Is.EqualTo(50));
            Assert.That(changeCount, Is.EqualTo(1));

            _health.OnHealthChanged.RemoveAllListeners();
            changeCount = 0;
            int diedCount = 0;
            _health.OnHealthChanged.AddListener((_, _) => changeCount++);
            _health.OnDied.AddListener(() => diedCount++);

            _health.Kill();
            Assert.IsFalse(_health.IsAlive);
            Assert.That(diedCount, Is.EqualTo(1));

            _health.SetMaxHealth(5);

            Assert.That(_health.MaxHealth, Is.EqualTo(5));
            Assert.That(_health.CurrentHealth, Is.EqualTo(0));
            Assert.IsFalse(_health.IsAlive);
            Assert.That(changeCount, Is.EqualTo(1));
            Assert.That(diedCount, Is.EqualTo(1));
        }
    }

    public class HealthViewTests
    {
        [Test]
        public void HealthView_UpdatesDisplays_OnDeathAndRevive()
        {
            var gameObject = new GameObject("HealthView_Test_Object");
            try
            {
                var health = gameObject.AddComponent<UnitHealth>();
                var view = gameObject.AddComponent<HealthView>();
                var display = gameObject.AddComponent<FakeHealthDisplay>();

                view.enabled = false;

                SetPrivateField(view, "_unitHealth", health);
                SetPrivateField(view, "_displayComponents", new MonoBehaviour[] { display });
                SetPrivateField(view, "_displays", new IHealthDisplay[] { display });

                view.enabled = true;

                Assert.That(display.LastHealth, Is.EqualTo(health.CurrentHealth));
                Assert.That(display.LastMax, Is.EqualTo(health.MaxHealth));
                Assert.That(display.LastAlive, Is.True);

                health.Kill();

                Assert.That(display.LastHealth, Is.EqualTo(0));
                Assert.That(display.LastAlive, Is.False);

                health.Revive();

                Assert.That(display.LastHealth, Is.EqualTo(health.MaxHealth));
                Assert.That(display.LastMax, Is.EqualTo(health.MaxHealth));
                Assert.That(display.LastAlive, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{fieldName}' was not found on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private class FakeHealthDisplay : MonoBehaviour, IHealthDisplay
        {
            public int LastHealth { get; private set; }
            public int LastMax { get; private set; }
            public bool LastAlive { get; private set; }

            public void SetHealth(int currentHealth, int maxHealth)
            {
                LastHealth = currentHealth;
                LastMax = maxHealth;
            }

            public void SetAliveState(bool isAlive)
            {
                LastAlive = isAlive;
            }
        }
    }
}
