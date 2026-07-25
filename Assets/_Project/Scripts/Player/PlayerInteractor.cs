using FishNet.Object;
using UnityEngine;
using AlienZoo.Animals;

namespace AlienZoo.Player
{
    /// <summary>
    /// Bare-bones capture interaction for the first playtest: look at a creature and HOLD E to build
    /// its subdue meter. Once subdued and sitting inside a TeleporterPad, the pad captures it →
    /// instant payout + quota tick. Real luring / carrying / tools replace this later.
    /// </summary>
    public class PlayerInteractor : NetworkBehaviour
    {
        [SerializeField] private Transform _aimSource;        // usually the player camera
        [SerializeField] private float _range = 4f;
        [SerializeField] private float _subduePerSecond = 60f;
        [SerializeField] private LayerMask _mask = ~0;

        private void Update()
        {
            if (!base.IsOwner || _aimSource == null) return;

            if (Input.GetKey(KeyCode.E) &&
                Physics.Raycast(_aimSource.position, _aimSource.forward, out RaycastHit hit, _range, _mask))
            {
                var ai = hit.collider.GetComponentInParent<AnimalAI>();
                if (ai != null)
                    CmdSubdue(ai, _subduePerSecond * Time.deltaTime);
            }
        }

        // The client aims; the server applies the subdue (authoritative on the actual value later).
        [ServerRpc]
        private void CmdSubdue(AnimalAI target, float amount)
        {
            if (target != null)
                target.ApplySubdue(amount);
        }
    }
}
