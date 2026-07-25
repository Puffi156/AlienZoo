using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

namespace AlienZoo.Player
{
    /// <summary>
    /// Minimal owner-driven first-person controller for early playtests.
    /// Movement runs ONLY on the owning client; a FishNet NetworkTransform (Client Authoritative)
    /// replicates the transform to everyone else. No prediction yet — that's a later polish pass.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private float _jumpHeight = 1.2f;

        [Header("Look")]
        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private Transform _cameraPivot;   // child holding the Camera
        [SerializeField] private float _minPitch = -80f;
        [SerializeField] private float _maxPitch = 80f;

        private CharacterController _cc;
        private Camera _cam;
        private AudioListener _listener;
        private float _pitch;
        private float _verticalVel;

        // External movement modifiers (acid slow, cornfield drag...). Net speed = product of all.
        private readonly Dictionary<Object, float> _speedModifiers = new();
        private float _speedMultiplier = 1f;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (_cameraPivot != null)
            {
                _cam = _cameraPivot.GetComponentInChildren<Camera>();
                _listener = _cameraPivot.GetComponentInChildren<AudioListener>();
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            bool owner = base.IsOwner;
            // Only the local player renders through their own camera / hears through their listener.
            if (_cam != null) _cam.enabled = owner;
            if (_listener != null) _listener.enabled = owner;

            if (owner) LockCursor(true);

            // Non-owners are driven by NetworkTransform; don't run local movement on them.
            enabled = owner;
        }

        private void Update()
        {
            if (!base.IsOwner) return;

            HandleCursorToggle();
            Look();
            Move();
        }

        private void Look()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            float mx = Input.GetAxis("Mouse X") * _mouseSensitivity;
            float my = Input.GetAxis("Mouse Y") * _mouseSensitivity;

            transform.Rotate(0f, mx, 0f);                       // yaw the body
            _pitch = Mathf.Clamp(_pitch - my, _minPitch, _maxPitch);
            if (_cameraPivot != null)
                _cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void Move()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 dir = (transform.right * h + transform.forward * v);
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            if (_cc.isGrounded)
            {
                _verticalVel = -1f;                              // keep grounded
                if (Input.GetButtonDown("Jump"))
                    _verticalVel = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            }
            _verticalVel += _gravity * Time.deltaTime;

            Vector3 velocity = dir * (_moveSpeed * _speedMultiplier) + Vector3.up * _verticalVel;
            _cc.Move(velocity * Time.deltaTime);
        }

        // ---- External speed modifiers (called by hazards / foliage on enter & exit) ----

        /// <summary>Apply a movement multiplier from an environmental volume (key by the source).</summary>
        public void SetSpeedModifier(Object source, float multiplier)
        {
            if (source == null) return;
            _speedModifiers[source] = Mathf.Clamp01(multiplier);
            RecomputeSpeed();
        }

        /// <summary>Remove a modifier when the player leaves that volume.</summary>
        public void ClearSpeedModifier(Object source)
        {
            if (source != null && _speedModifiers.Remove(source))
                RecomputeSpeed();
        }

        private void RecomputeSpeed()
        {
            float m = 1f;
            foreach (var kv in _speedModifiers)
                m *= kv.Value;
            _speedMultiplier = m;
        }

        // Esc frees the cursor (handy in-editor); click to re-lock.
        private void HandleCursorToggle()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) LockCursor(false);
            else if (Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0)) LockCursor(true);
        }

        private void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
