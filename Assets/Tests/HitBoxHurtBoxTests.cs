using NUnit.Framework;
using Runtime.Combat;
using UnityEngine;
using System.Reflection;

namespace Tests
{
    public class HitBoxHurtBoxTests
    {
        private GameObject _attackerGO;
        private GameObject _victimGO;
        private GameObject _hurtChildGO;
        private HitBox _hitBox;
        private HurtBox _hurtBox;
        private UnitHealth _health;

        private static void InvokePrivate(object target, string methodName)
        {
            var m = target.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            m?.Invoke(target, null);
        }

        [SetUp]
        public void SetUp()
        {
            // ─── SETUP: ATTACKER ────────────────────────────────
            _attackerGO = new GameObject("Attacker");
            var colA = _attackerGO.AddComponent<BoxCollider2D>();
            colA.isTrigger = true;
            var rbA = _attackerGO.AddComponent<Rigidbody2D>();
            rbA.bodyType = RigidbodyType2D.Kinematic;
            _hitBox = _attackerGO.AddComponent<HitBox>();

            // ─── SETUP: VICTIM ROOT ─────────────────────────────
            _victimGO = new GameObject("Victim");
            _health = _victimGO.AddComponent<UnitHealth>();

            // ─── SETUP: CHILD HURTBOX ───────────────────────────
            _hurtChildGO = new GameObject("HurtBox_Child");
            _hurtChildGO.transform.SetParent(_victimGO.transform);
            var childCol = _hurtChildGO.AddComponent<BoxCollider2D>();
            childCol.isTrigger = true;
            _hurtBox = _hurtChildGO.AddComponent<HurtBox>();

            // Ensure Awake() runs manually for tests (no SendMessage)
            InvokePrivate(_hurtBox, "Awake");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_attackerGO);
            Object.DestroyImmediate(_victimGO);
        }

        // ──────────────────────────────────────────────────────
        // TESTS
        // ──────────────────────────────────────────────────────

        [Test]
        public void HurtBox_TakesDamage_OnHitBoxCollision()
        {
            int before = _health.CurrentHealth;

            // Plain struct initialization — no reflection required
            var dmg = new Runtime.Combat.DamageProfile
            {
                Raw = 15f,
                Blunt = 10f,
                Fire = 5f,
                CritMultiplier = 1f,
                KnockbackForce = 0f
            };

            _hitBox.SetDamage(dmg);

            bool hitApplied = _hurtBox.ApplyHit(_hitBox, false);

            Assert.IsTrue(hitApplied, "HitBox should have been applied successfully.");
            Assert.That(_health.CurrentHealth, Is.LessThan(before),
                $"Health should decrease after being hit. Before: {before}, After: {_health.CurrentHealth}");
        }
        

        [Test]
        public void HurtBox_ApplyHit_AddsKnockbackForce_AndRaisesEvent()
        {
            // Arrange
            var rb = _hurtBox.gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;     // so forces actually apply
            rb.gravityScale = 0f;                      // isolate knockback movement

            bool knockbackCalled = false;
            _hurtBox.OnKnockbackApplied.AddListener(() => knockbackCalled = true);

            // Place the objects so we know the force direction
            _hitBox.transform.position = Vector3.left * 1f;
            _hurtBox.transform.position = Vector3.right * 1f;

            // Give the hitbox a profile with non-zero knockback
            var dmg = new Runtime.Combat.DamageProfile
            {
                Raw = 5f,
                KnockbackForce = 10f,
                CritMultiplier = 1f
            };
            _hitBox.SetDamage(dmg);

            // Record pre-impact velocity
            Vector2 beforeVel = rb.linearVelocity;

            // Act
            bool hitApplied = _hurtBox.ApplyHit(_hitBox, false);

            // Assert
            Assert.IsTrue(hitApplied, "HitBox should apply hit successfully.");
            Assert.IsTrue(knockbackCalled, "Knockback event should be invoked.");

            // Expected direction: from hurtbox ← hitbox  (so roughly +X)
            Vector2 expectedDir = (Vector2)(_hurtBox.transform.position - _hitBox.transform.position).normalized;
            Vector2 afterVel = rb.linearVelocity;

            // Physics may not simulate instantly; check impulse direction & magnitude
            Assert.That(Vector2.Dot(afterVel.normalized, expectedDir), Is.GreaterThan(0.9f),
                $"Knockback direction incorrect. Expected ≈ {expectedDir}, got {afterVel.normalized}");
            Assert.That(afterVel.magnitude, Is.GreaterThan(0f), "Rigidbody2D should gain velocity from knockback.");
        }




        [Test]
        public void HurtBox_IgnoresDamage_WhenInvulnerable()
        {
            // Arrange
            _health.ModifyHealth(-10);
            int before = _health.CurrentHealth;

            // Force invulnerable
            var field = typeof(HurtBox).GetField("_isInvulnerable",
                BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(_hurtBox, true);

            // Act
            _hurtBox.ApplyHit(_hitBox, false);

            // Assert
            Assert.That(_health.CurrentHealth, Is.EqualTo(before),
                "Health should remain unchanged while invulnerable.");
        }

        [Test]
        public void HurtBox_Invulnerability_Expires_AfterTimer()
        {
            var invulnField = typeof(HurtBox).GetField("_isInvulnerable",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var timerField = typeof(HurtBox).GetField("_invulnTimer",
                BindingFlags.NonPublic | BindingFlags.Instance);

            invulnField?.SetValue(_hurtBox, true);
            timerField?.SetValue(_hurtBox, 0.1f);

            InvokePrivate(_hurtBox, "Update"); // simulate frame
            timerField?.SetValue(_hurtBox, -1f);
            InvokePrivate(_hurtBox, "Update");

            bool state = (bool)invulnField?.GetValue(_hurtBox)!;
            Assert.That(state, Is.False, "Invulnerability should expire after timer.");
        }

        [Test]
        public void HurtBox_ConnectsToHealth_Automatically_WhenNull()
        {
            // Arrange: nullify private reference
            var field = typeof(HurtBox).GetField("_health",
                BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(_hurtBox, null);

            // Act: re-run Awake manually
            InvokePrivate(_hurtBox, "Awake");

            // Assert: should reconnect to parent UnitHealth
            var reattached = field?.GetValue(_hurtBox) as UnitHealth;
            Assert.IsNotNull(reattached, "HurtBox should auto-assign UnitHealth from parent if missing.");
        }

        [Test]
        public void HurtBox_Collider_IsAlwaysTrigger()
        {
            var col = _hurtBox.GetComponent<Collider2D>();
            Assert.IsNotNull(col, "HurtBox should have a Collider2D attached.");
            Assert.IsTrue(col.isTrigger, "HurtBox collider should always be trigger.");
        }
    }
}

