using UnityEngine;
using System.Collections;

public class ShipSpawner : MonoBehaviour
{
    public GameObject shipPrefab;
    public Vector3 spawnAreaCenter;
    public Vector3 spawnAreaSize;
    public Vector3 shipRotation = new Vector3(-90f, 0f, 0f);
    public int numberOfShips = 2; // Количество кораблей для спавна

    void Start()
    {
        SpawnShips();
    }

    void SpawnShips()
    {
        for (int i = 0; i < numberOfShips; i++)
        {
            SpawnSingleShip(i + 1);
        }
    }

    void SpawnSingleShip(int shipNumber)
    {
        Vector3 randomPosition = new Vector3(
            Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
            Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2),
            Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
        ) + spawnAreaCenter;

        Quaternion spawnRotation = Quaternion.Euler(shipRotation);
        GameObject spawnedShip = Instantiate(shipPrefab, randomPosition, spawnRotation);

        // Даем кораблю имя с номером
        spawnedShip.name = $"Корабль_{shipNumber}";

        Debug.Log($"🚢 Корабль #{shipNumber} создан в позиции: {randomPosition}");
        Debug.Log($"📍 Координаты: X={randomPosition.x:F2}, Y={randomPosition.y:F2}, Z={randomPosition.z:F2}");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawCube(spawnAreaCenter, spawnAreaSize);
    }
}