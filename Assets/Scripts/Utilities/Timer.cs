using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Utilities
{
    public class Timer : MonoBehaviour
    {
        [ShowInInspector, ReadOnly] public bool IsRunning { get; private set; }

        public UnityEvent OnTimerEnd;

        public void StartTimer(float seconds)
        {
            StartCoroutine(TimerRoutine(seconds));
        }

        public void StopTimer()
        {
            StopAllCoroutines();
            IsRunning = false;
            OnTimerEnd?.Invoke();
        }

        private IEnumerator TimerRoutine(float seconds)
        {
            IsRunning = true;
            yield return new WaitForSeconds(seconds);
            IsRunning = false;
            OnTimerEnd.Invoke();
        }
    }
}