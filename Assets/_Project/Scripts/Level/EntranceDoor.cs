using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;
using AlienZoo.Player;

namespace AlienZoo.Level
{
    /// <summary>
    /// Interactive farmhouse door — the transition point into the procedural interior/dungeon.
    /// A nearby player presses the interact key; the request is validated on the server, which
    /// loads the interior scene for that connection (or the whole team). Interior generation is a
    /// later pass, so until an interior scene is assigned this just logs the entry.
    ///
    /// Requires a NetworkObject and a trigger Collider (the interaction proximity volume).
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class EntranceDoor : NetworkBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private KeyCode _interactKey = KeyCode.F;
        [Tooltip("Shown by UI when the local player is in range (UI hookup later).")]
        [SerializeField] private string _prompt = "Press F to enter";

        [Header("Interior")]
        [Tooltip("Interior scene to load. Leave empty until interiors exist.")]
        [SerializeField] private string _interiorSceneName;
        [Tooltip("If true, the whole team enters together; otherwise only the interactor.")]
        [SerializeField] private bool _groupEntry = false;

        [Header("Visual")]
        [Tooltip("Optional transform swung open when the door is used.")]
        [SerializeField] private Transform _doorVisual;

        public string Prompt => _prompt;
        public bool LocalPlayerInRange { get; private set; }

        private bool _used;

        private void OnTriggerEnter(Collider other)
        {
            var pc = other.GetComponentInParent<PlayerController>();
            if (pc != null && pc.IsOwner) LocalPlayerInRange = true;
        }

        private void OnTriggerExit(Collider other)
        {
            var pc = other.GetComponentInParent<PlayerController>();
            if (pc != null && pc.IsOwner) LocalPlayerInRange = false;
        }

        private void Update()
        {
            if (!LocalPlayerInRange || _used) return;
            if (Input.GetKeyDown(_interactKey))
                CmdEnter();
        }

        // Any nearby player may open the door (no ownership needed on the door itself).
        [ServerRpc(RequireOwnership = false)]
        private void CmdEnter(NetworkConnection conn = null)
        {
            if (_used) return;
            _used = true;
            RpcOpen();

            if (string.IsNullOrEmpty(_interiorSceneName))
            {
                Debug.Log($"[EntranceDoor] '{name}' used — no interior scene assigned yet (interior generation TODO).");
                _used = false; // allow re-use while interiors are still stubbed
                return;
            }

            var sld = new SceneLoadData(_interiorSceneName) { ReplaceScenes = ReplaceOption.None };
            if (_groupEntry)
                InstanceFinder.SceneManager.LoadGlobalScenes(sld);
            else
                InstanceFinder.SceneManager.LoadConnectionScenes(conn, sld);
        }

        [ObserversRpc]
        private void RpcOpen()
        {
            if (_doorVisual != null)
                _doorVisual.localRotation = Quaternion.Euler(0f, 95f, 0f); // placeholder swing; replace with animation
        }
    }
}
