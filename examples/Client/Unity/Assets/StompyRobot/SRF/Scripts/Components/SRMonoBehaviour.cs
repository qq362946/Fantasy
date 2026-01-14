namespace SRF
{
    using System.Diagnostics;
    using UnityEngine;

    /// <summary>
    /// Base MonoBehaviour which provides useful common functionality
    /// </summary>
    public abstract class SRMonoBehaviour : MonoBehaviour
    {
        /// <summary>
        /// Get the Transform component, using a cached reference if possible.
        /// </summary>
        public Transform CachedTransform
        {
            [DebuggerStepThrough]
            [DebuggerNonUserCode]
            get
            {
                if (_transform == null)
                {
                    _transform = base.transform;
                }

                return _transform;
            }
        }

        /// <summary>
        /// Get the Collider component, using a cached reference if possible.
        /// </summary>
        public Collider CachedCollider
        {
            [DebuggerStepThrough]
            [DebuggerNonUserCode]
            get
            {
                if (_collider == null)
                {
                    _collider = GetComponent<Collider>();
                }

                return _collider;
            }
        }

        /// <summary>
        /// Get the Collider component, using a cached reference if possible.
        /// </summary>
        public Collider2D CachedCollider2D
        {
            [DebuggerStepThrough]
            [DebuggerNonUserCode]
            get
            {
                if (_collider2D == null)
                {
                    _collider2D = GetComponent<Collider2D>();
                }

                return _collider2D;
            }
        }

        /// <summary>
        /// Get the Rigidbody component, using a cached reference if possible.
        /// </summary>
        public Rigidbody CachedRigidBody
        {
            [DebuggerStepThrough]
            [DebuggerNonUserCode]
            get
            {
                if (_rigidBody == null)
                {
                    _rigidBody = GetComponent<Rigidbody>();
                }

                return _rigidBody;
            }
        }

        /// <summary>
        /// Get the Rigidbody2D component, using a cached reference if possible.
        /// </summary>
        public Rigidbody2D CachedRigidBody2D
        {
            [DebuggerStepThrough]
            [DebuggerNonUserCode]
            get
            {
                if (_rigidbody2D == null)
                {
                    _rigidbody2D = GetComponent<Rigidbody2D>();
                }

                return _rigidbody2D;
            }
        }

        /// <summary>
        /// Get the GameObject this behaviour is attached to, using a cached reference if possible.
        /// </summary>
        public GameObject CachedGameObject
        {
            [DebuggerStepThrough]
            [DebuggerNonUserCode]
            get
            {
                if (_gameObject == null)
                {
                    _gameObject = base.gameObject;
                }

                return _gameObject;
            }
        }

        // Override existing getters for legacy usage

        // ReSharper disable InconsistentNaming
        public new Transform transform
        {
            get { return CachedTransform; }
        }

#if !UNITY_5

		public new Collider collider
		{
			get { return CachedCollider; }
		}
		public new Collider2D collider2D
		{
			get { return CachedCollider2D; }
		}
		public new Rigidbody rigidbody
		{
			get { return CachedRigidBody; }
		}
		public new Rigidbody2D rigidbody2D
		{
			get { return CachedRigidBody2D; }
		}
		public new GameObject gameObject
		{
			get { return CachedGameObject; }
		}

#endif

        // ReSharper restore InconsistentNaming

        private Collider _collider;
        private Transform _transform;
        private Rigidbody _rigidBody;
        private GameObject _gameObject;
        private Rigidbody2D _rigidbody2D;
        private Collider2D _collider2D;

        /// <summary>
        /// Assert that the value is not null, disable the object and print a debug error message if it is.
        /// </summary>
        /// <param name="value">Object to check</param>
        /// <param name="fieldName">Debug name to pass in</param>
        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        protected void AssertNotNull(object value, string fieldName = null)
        {
            SRDebugUtil.AssertNotNull(value, fieldName, this);
        }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        protected void Assert(bool condition, string message = null)
        {
            SRDebugUtil.Assert(condition, message, this);
        }

        /// <summary>
        /// Assert that the value is not null, disable the object and print a debug error message if it is.
        /// </summary>
        /// <param name="value">Object to check</param>
        /// <param name="fieldName">Debug name to pass in</param>
        /// <returns>True if object is not null</returns>
        [Conditional("UNITY_EDITOR")]
        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        protected void EditorAssertNotNull(object value, string fieldName = null)
        {
            AssertNotNull(value, fieldName);
        }

        [Conditional("UNITY_EDITOR")]
        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        protected void EditorAssert(bool condition, string message = null)
        {
            Assert(condition, message);
        }
    }
}
