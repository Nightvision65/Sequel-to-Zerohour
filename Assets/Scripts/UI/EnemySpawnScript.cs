using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnScript : MonoBehaviour
{
    public GameObject[] enemy;
    public float startTime, endTime;
    public float acceleration;
    public float minDis, maxDis;
    [SerializeField] private float time, timer;
    void Start()
    {
        time = startTime;
    }

    // Update is called once per frame
    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            Spawn();
            timer = time;
        }
    }
    public void Spawn()
    {

        // Calculate the new position
        if (PlayerController.instance.isActiveAndEnabled) { 
            // Get a random direction as a 2D vector
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            // Get a random distance between minDistance and maxDistance
            float randomDistance = Random.Range(minDis, maxDis);
            Vector2 spawnPosition = (Vector2)PlayerController.instance.GetComponentInChildren<CharacterScript>().transform.position + randomDirection * randomDistance;
            // Instantiate the object at the calculated position
            Instantiate(enemy[0], spawnPosition, Quaternion.identity);
            // Get a random direction as a 2D vector
            randomDirection = Random.insideUnitCircle.normalized;
            // Get a random distance between minDistance and maxDistance
            randomDistance = Random.Range(minDis, maxDis);
            spawnPosition = (Vector2)PlayerController.instance.GetComponentInChildren<CharacterScript>().transform.position + randomDirection * randomDistance;
            Instantiate(enemy[1], spawnPosition, Quaternion.identity);
            time = Mathf.Clamp(time - acceleration, endTime, startTime);
        }
    }
}
