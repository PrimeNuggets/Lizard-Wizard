using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class HazardSpawner : MonoBehaviour
{
    // Only one instance of this script is needed in any given scene. Put this on the camera
    //Attach ground objects to this list to allow hazards to spawn on them. Hazards will also spawn on any objects using the "Ground" tag, even if they are not in this list.
    [Tooltip("List of ground objects where hazards can spawn. Objects with 'Ground' tag are auto-included if useGroundTag is true.")]
    public List<GameObject> spawnableGround = new List<GameObject>();

    [Tooltip("Automatically include all objects tagged 'Ground' as spawnable ground.")]
    public bool useGroundTag = true;

    [Serializable]
    public class SpawnPosition
    {
        public float[] xBounds = new float[2];
        public float yPos;
    }

    [NonSerialized] public List<SpawnPosition> spawnPositions = new List<SpawnPosition>();
    [Serializable]
    public class Bound2D
    {
        public float left;
        public float right;
        public float bottom;
        public float top;

        public Bound2D(float left, float right, float bottom, float top)
        {
            this.left = left;
            this.right = right;
            this.bottom = bottom;
            this.top = top;
        }
    }
    [Tooltip("Screen boundaries for spawning (left, right, bottom, top). Hazards won't spawn outside these bounds.")]
    public Bound2D screenBounds = new Bound2D(-20f, 20f, -20f, 20f); //xMin, xMax, yMin, yMax
    [Serializable]
    public class HazardSpawn
    {
        [Tooltip("Prefab of the hazard to spawn.")]
        public GameObject hazardPrefab;
        [Range(0f, 100f), Tooltip("Percentage chance (0-100) for this hazard to spawn on a valid position.")]
        public float spawnChance;
    }
    [Tooltip("List of hazard prefabs and their spawn chances.")]
    public List<HazardSpawn> hazards = new List<HazardSpawn>();
    [Tooltip("The number of times to attempt to spawn hazards.")] public int spawnRolls = 1;
    [Tooltip("The x-axis distance from the player within which hazards will not spawn. Set to 0 to disable.")] public float playerSpawnExclusionX = 5f;
    [Tooltip("The x-axis distance from other hazards within which hazards will not spawn. Set to 0 to disable.")] public float hazardSpawnExclusionX = 2.5f;
    Camera mainCamera;
    Bound2D bounds = new Bound2D(0f, 0f, 0f, 0f);
    HashSet<int> attemptedSpawnPositions = new HashSet<int>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCamera = GetComponent<Camera>();
        ProcessSpawnPositions();
    }

    void ProcessSpawnPositions()
    {
        //This function processes the spawnableGround list to populate the spawnPositions list with the x bounds and y position of each ground object. This allows hazards to be spawned on top of these objects by randomly selecting an x value within the bounds and using the y position as is.
        GameObject[] groundObjects = new GameObject[0];
        if (useGroundTag)
        {
            groundObjects = GameObject.FindGameObjectsWithTag("Ground");
        }
        foreach (GameObject ground in groundObjects)
        {
            if (!spawnableGround.Contains(ground))
            {
                spawnableGround.Add(ground);
            }
        }

        foreach (GameObject ground in spawnableGround)
        {
            Collider2D groundCollider = ground.GetComponent<Collider2D>();
            if (groundCollider != null)
            {
                SpawnPosition newSpawnPos = new SpawnPosition();
                newSpawnPos.xBounds[0] = groundCollider.bounds.min.x;
                newSpawnPos.xBounds[1] = groundCollider.bounds.max.x;
                newSpawnPos.yPos = groundCollider.bounds.max.y;
                spawnPositions.Add(newSpawnPos);
            }
            else
            {
                Debug.LogWarning("Ground object " + ground.name + " does not have a Collider2D component and will be ignored for hazard spawning.");
            }
        }
    }

    void SpawnHazard(HazardSpawn hazardSpawn, SpawnPosition spawnPosition)
    {
        if (!WithinBounds(new Vector3(spawnPosition.xBounds[0], spawnPosition.yPos, 0f)) && !WithinBounds(new Vector3(spawnPosition.xBounds[1], spawnPosition.yPos, 0f)))
        {
            Debug.LogWarning("Spawn position for hazard " + hazardSpawn.hazardPrefab.name + " is out of screen bounds and will be skipped.");
            return;
        }

        float spawnX = UnityEngine.Random.Range(spawnPosition.xBounds[0], spawnPosition.xBounds[1]);
        Vector3 spawnPoint = new Vector3(spawnX, spawnPosition.yPos, 0f);
        if (TooCloseToPlayerOnX(spawnPoint.x))
        {
            return;
        }
        if (TooCloseToHazardOnX(spawnPoint.x))
        {
            return;
        }

        if (UnityEngine.Random.Range(0f, 100f) <= hazardSpawn.spawnChance)
        {
            Instantiate(hazardSpawn.hazardPrefab, spawnPoint, Quaternion.identity);
        }
    }

    void SpawnHazards()
    {
        for (int spawnIndex = 0; spawnIndex < spawnPositions.Count; spawnIndex++)
        {
            if (attemptedSpawnPositions.Contains(spawnIndex))
            {
                continue;
            }

            SpawnPosition spawnPosition = spawnPositions[spawnIndex];
            if (!WithinBounds(new Vector3(spawnPosition.xBounds[0], spawnPosition.yPos, 0f)) && !WithinBounds(new Vector3(spawnPosition.xBounds[1], spawnPosition.yPos, 0f)))
            {
                continue;
            }

            attemptedSpawnPositions.Add(spawnIndex);

            for (int roll = 0; roll < spawnRolls; roll++)
            {
                foreach (HazardSpawn hazard in hazards)
                {
                    if (hazard != null && hazard.hazardPrefab != null)
                    {
                        SpawnHazard(hazard, spawnPosition);
                    }
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateBounds();
        SpawnHazards();
    }
    void UpdateBounds()
    {
        bounds.left = mainCamera.transform.position.x + screenBounds.left;
        bounds.right = mainCamera.transform.position.x + screenBounds.right;
        bounds.bottom = mainCamera.transform.position.y + screenBounds.bottom;
        bounds.top = mainCamera.transform.position.y + screenBounds.top;
    }
    bool WithinBounds(Vector3 position)
    {
        return position.x >= bounds.left && position.x <= bounds.right && position.y >= bounds.bottom && position.y <= bounds.top;
    }

    bool TooCloseToPlayerOnX(float spawnX)
    {
        if (playerSpawnExclusionX <= 0f)
        {
            return false;
        }

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if (Mathf.Abs(spawnX - player.transform.position.x) <= playerSpawnExclusionX)
            {
                return true;
            }
        }

        return false;
    }

    bool TooCloseToHazardOnX(float spawnX)
    {
        if (hazardSpawnExclusionX <= 0f)
        {
            return false;
        }

        HazardManager[] existingHazards = FindObjectsByType<HazardManager>(FindObjectsSortMode.None);
        foreach (HazardManager hazard in existingHazards)
        {
            if (Mathf.Abs(spawnX - hazard.transform.position.x) <= hazardSpawnExclusionX)
            {
                return true;
            }
        }

        return false;
    }
}
