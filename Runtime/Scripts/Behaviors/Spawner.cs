using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("General Settings")]
    [Tooltip("The prefab to spawn.")]
    public GameObject prefab;
    [Tooltip("The time in seconds between each spawn.")]
    public float spawnPeriod = 1f;
    [Tooltip("The range in which the prefab will be spawned around the spawner object.")]
    public float spawnRange = 10f;

    [Header("Initialization")]
    public bool beginOnAwake = true;
    [Tooltip("If enabled, the spawner will wait for the ROS connection to be established before starting to spawn.")]
    public bool waitForROSConnection = true;

    [Header("Pool Mode")]
    [Tooltip("If enabled, the spawner will only spawn new prefabs until this collider contains maxInPool spawned objects. If some objects are removed from the collider area, new objects will be spawned.")]
    public BoxCollider poolArea;
    [Tooltip("The maximum number of spawned objects that can be inside the poolArea collider at any given time.")]
    public int maxInPool = 10;
    [Tooltip("The tag to assign to spawned objects (used to check if they are in the pool area).")]
    public string poolTag = "Spawned";
    public int numInPool = 0;


    bool spawning = false;
    float spawnTimer = 0f;

    public async void StartSpawning()
    {
        if (waitForROSConnection) await ROSConnector.AwaitConnection();
        spawning = true;
    }

    public void StopSpawning()
    {
        spawning = false;
    }

    async void Awake()
    {
        if (waitForROSConnection) await ROSConnector.AwaitConnection();

        if (beginOnAwake)
        {
            StartSpawning();
        }

        if (poolArea) poolArea.isTrigger = true;
    }

    void Update()
    {
        if (spawning)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer > spawnPeriod)
            {
                spawnTimer = 0f;
                SpawnEnemy();
            }
        }
    }

    public void SpawnEnemy()
    {
        if (poolArea)
        {
            var boxSize = poolArea.size;
            boxSize.Scale(poolArea.transform.lossyScale);

            var overlaps = Physics.OverlapBox(poolArea.transform.position, boxSize, poolArea.transform.rotation);
            overlaps = System.Array.FindAll(overlaps, x => x.CompareTag(poolTag));
            numInPool = overlaps.Length;
            if (numInPool >= maxInPool)
            {
                return;
            }
        }

        Vector3 spawnPosition = transform.position + Random.insideUnitSphere * spawnRange;
        spawnPosition.y = transform.position.y;
        var spawn = Instantiate(prefab, spawnPosition, Quaternion.identity);
        if (poolArea && poolTag != "")
        {
            spawn.tag = poolTag;
        }
    }

}
