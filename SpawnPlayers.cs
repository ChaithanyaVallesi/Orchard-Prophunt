using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SpawnPlayers : MonoBehaviour
{
    public GameObject playerPrefab;
    List<Transform> spawnPoints = new List<Transform>();

    void Start()
    {
        foreach(Transform spawnPoint in transform)
        {
            spawnPoints.Add(spawnPoint.transform);
        }
        Vector3 randomPosition = spawnPoints[Random.Range(0, spawnPoints.Count - 1)].transform.position;
        PhotonNetwork.Instantiate(playerPrefab.name, randomPosition, Quaternion.identity);
    }
}
