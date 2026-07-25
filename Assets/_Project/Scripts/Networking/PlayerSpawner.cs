using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using UnityEngine;

namespace AlienZoo.Networking
{
    /// <summary>
    /// Spawns a player object for each connection once it has loaded the starting scenes.
    /// Server-only logic; the spawned object is owned by the connecting client.
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField] private NetworkObject _playerPrefab;
        [SerializeField] private Transform[] _spawnPoints;

        private NetworkManager _nm;

        private void OnEnable()
        {
            _nm = InstanceFinder.NetworkManager;
            if (_nm != null)
                _nm.SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScenes;
        }

        private void OnDisable()
        {
            if (_nm != null)
                _nm.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedStartScenes;
        }

        private void OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
        {
            if (!asServer || _playerPrefab == null) return;

            Transform sp = PickSpawn(conn.ClientId);
            NetworkObject nob = Instantiate(_playerPrefab, sp.position, sp.rotation);
            _nm.ServerManager.Spawn(nob, conn); // conn becomes the owner
        }

        private Transform PickSpawn(int clientId)
        {
            if (_spawnPoints == null || _spawnPoints.Length == 0) return transform;
            return _spawnPoints[clientId % _spawnPoints.Length];
        }
    }
}
