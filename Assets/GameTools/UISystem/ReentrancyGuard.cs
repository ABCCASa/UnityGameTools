using System;
using UnityEngine;
namespace GameTools.UISystem
{
    public class ReentrancyGuard
    {
        public bool entered { get; private set; }
        public void CheckNotEntered()
        {
            if (entered) throw new InvalidOperationException("Reentrant call detected.");
        }

        public IDisposable Enter()
        {
            CheckNotEntered();
            entered = true;
            return new Scope(this);
        }

        private void Exit()
        {
            entered = false;
        }

        private sealed class Scope : IDisposable
        {
            private readonly ReentrancyGuard _owner;

            internal Scope(ReentrancyGuard owner)
            {
                _owner = owner;
            }

            public void Dispose()
            {
                _owner.Exit();
            }
        }
    }
}