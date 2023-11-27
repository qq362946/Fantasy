using System;
using UnityEngine;

namespace Fantasy
{
    public class FantasyCollider2D : MonoBehaviour
    {
        public event Action<Collider2D> OnTriggerEnter2DEvent;

        private void OnTriggerEnter2D(Collider2D other)
        {
            OnTriggerEnter2DEvent?.Invoke(other);
        }
    }
}