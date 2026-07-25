using FishNet;
using UnityEngine;
using AlienZoo.Data;
using AlienZoo.Economy;
using AlienZoo.GameState;

namespace AlienZoo.Networking
{
    /// <summary>
    /// DEV-ONLY quick harness. Draws on-screen buttons to start Host/Server/Client and, once
    /// connected, to kick off a test "day" and watch money / quota update live.
    /// Remove (or disable) before shipping.
    /// </summary>
    public class DevNetworkStarter : MonoBehaviour
    {
        [SerializeField] private string _clientAddress = "127.0.0.1";
        [SerializeField] private bool _autoHost = false;

        [Header("Test Day (optional — for the capture-loop test)")]
        [SerializeField] private PlanetDefinition _testPlanet;
        [SerializeField] private int _seed = 12345;

        private void Start()
        {
            if (_autoHost) StartHost();
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 280, 320), GUI.skin.box);

            if (InstanceFinder.NetworkManager == null)
            {
                GUILayout.Label("No NetworkManager in scene.");
                GUILayout.EndArea();
                return;
            }

            bool server = InstanceFinder.IsServerStarted;
            bool client = InstanceFinder.IsClientStarted;

            if (!server && !client)
            {
                if (GUILayout.Button("Host (Server + Client)")) StartHost();
                if (GUILayout.Button("Server only")) InstanceFinder.ServerManager.StartConnection();
                if (GUILayout.Button($"Client → {_clientAddress}"))
                    InstanceFinder.ClientManager.StartConnection(_clientAddress);
            }
            else
            {
                GUILayout.Label($"Server: {server}   Client: {client}");

                if (GameManager.Instance != null)
                    GUILayout.Label($"Phase: {GameManager.Instance.Phase.Value}");

                if (server && _testPlanet != null && GameManager.Instance != null)
                {
                    if (GUILayout.Button("Begin Day (test)"))
                        GameManager.Instance.BeginDay(_testPlanet, _seed);
                }

                if (EconomyManager.Instance != null)
                    GUILayout.Label($"Money: ${EconomyManager.Instance.Balance}");
                if (GameManager.Instance != null)
                    GUILayout.Label($"Quota complete: {GameManager.Instance.QuotaComplete.Value}");

                GUILayout.Space(8);
                if (GUILayout.Button("Stop"))
                {
                    if (client) InstanceFinder.ClientManager.StopConnection();
                    if (server) InstanceFinder.ServerManager.StopConnection(true);
                }
            }

            GUILayout.EndArea();
        }

        private void StartHost()
        {
            InstanceFinder.ServerManager.StartConnection();
            InstanceFinder.ClientManager.StartConnection();
        }
    }
}
