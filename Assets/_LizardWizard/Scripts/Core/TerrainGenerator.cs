using System;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    [Serializable]
    public class Props
    {
        public enum GenType
        {
            Randomized,
            Perlin,
            Scattered
        }
        public GameObject item;
        public float scale = 100f;
        [LabelOverride("Generation Type")] public GenType genType;
        //Perlin
        public float spawnChance = 10f;
        [NonSerialized] public float seedX = 0f;
        [NonSerialized] public float seedY = 0f;
        //Scattered
        public float rollLowerBound;
        public float rollUpperBound;
        //Randomized
        [Tooltip("Minimum distance from other prop")] public float minDist = 50f;
        public float radius = 500f;
        public int maxCount = 1000;
    }
    private int width = 1024;
    private int height = 1024;
    //width and height of the terrain. Must be equal! 
    // it scales the dunes with it- increasing it will NOT make more dunes it will make BIGGER dunes
    private int depth = 35;
    //depth is how high the hills go.
    private float scale = 40f;
    //scale of the perlin noise map. higher = more dunes across the map.
    private float multscale = 10f;
    //scale of the perlin noise multiplier map. lower means larger areas of flat and hilly terrain

    //Seeds. They each have an X and Y of where in the perlin noise map they are. 
    // Could squash some of them together for simplification and easier map seed gen but idk for now
    private float seedX = 0f;
    private float seedY = 0f;
    private float multseedX = 0f;
    private float multseedY = 0f;
    public Props[] props;
    private Collider[] propcheckbuffer = new Collider[10];
    private Vector3 centerOfMap;
    private Vector3 poiLocation;
    private GameObject PropParent;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        centerOfMap = new Vector3(width/2f, depth/2f, height/2f);
        PropParent = new GameObject("PropParent");
        //uncomment this for randomization
        //seedX = Random.Range(0f,100000f);
        //seedY = Random.Range(0f,100000f);
        //propseedX = Random.Range(0f,100000f);
        //propseedY = Random.Range(0f,100000f);
        Terrain terrain = GetComponent<Terrain>();
        terrain.terrainData = GenerateTerrain(terrain.terrainData);
        //placeprops overrides:
            // for perlin patterns: Placeprops (prop, float minimum %chance of prop)
            // for scattering: Placeprops (prop, float lower bound of prop chance, float upper bound of propchance)
            // for radius based random placements: PlaceProps(GameObject prop, float RadiusFromOther, float spawnRadius, Vector2 spawncenter, int NumberToPlace)
        foreach (var prop in props)
        {
            switch (prop.genType)
            {
                case Props.GenType.Perlin:
                    PlaceProps(prop, prop.spawnChance);
                    break;
                case Props.GenType.Scattered:
                    PlaceProps(prop, prop.rollLowerBound, prop.rollUpperBound);
                    break;
                case Props.GenType.Randomized:
                    PlaceProps(prop, prop.minDist, prop.radius, new Vector2(centerOfMap.x, centerOfMap.z), prop.maxCount);
                    break;
            }
        }
        //PlaceProps(prop1, 75f);
        
    }
    /* DO NOT UNCOMMENT 
    this is for terrain that updates live. any large size (>500x500) will start slowing computers. for small testing only.
    void Update()
    {
        Terrain terrain = GetComponent<Terrain>();
        terrain.terrainData = GenerateTerrain(terrain.terrainData);
    }
    */

    TerrainData GenerateTerrain(TerrainData terrainData)
    {
        terrainData.heightmapResolution = width + 1;
        terrainData.size = new Vector3(width, depth, height);
        terrainData.SetHeights(0, 0, GenerateHeights());
        return terrainData;
    }
    float[,] GenerateHeights()
    {
        float[,] heights = new float[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                heights[x, y] = CalculateHeight(x, y);
            }
        }
        return heights;
    }
    float CalculateHeight(int x, int y)
    {
        //Okay so this is for the terrain height multiplier
        float ScalexCoord = (float)x / width * multscale;
        float ScaleyCoord = (float)y / height * multscale;
        //to make 'areas' the multscale should be a lot less than scale

        float xCoord = (float)x / width * scale;
        float yCoord = (float)y / height * scale;
        //terrain noise * larger mult noise*
        return Mathf.PerlinNoise(xCoord + seedX, yCoord + seedY) * Mathf.PerlinNoise(ScalexCoord + multseedX, ScaleyCoord + multseedY);
    }

    void PlaceProps(Props prop, float chance)
    {
        chance = 100 - chance;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float xCoord = (float)x / width * prop.scale;
                float yCoord = (float)y / height * prop.scale;
                if (UnityEngine.Random.Range(0f, 100f)*Mathf.PerlinNoise(xCoord + prop.seedX, yCoord + prop.seedY)>chance)
                {
                    float xRand = UnityEngine.Random.value;
                    float yRand = UnityEngine.Random.value;
                    //
                    int colliders = Physics.OverlapSphereNonAlloc(new Vector3(x+xRand, 0.1f+GetComponent<Terrain>().terrainData.GetHeight(x, y), y+yRand), 1.0f,propcheckbuffer);
                    if (colliders > 1) {
                    //Debug.Log("overlap detected - "+colliders);
                    }else{
                    //spawn prop
                    Instantiate(prop.item, new Vector3(x + xRand, GetComponent<Terrain>().terrainData.GetHeight(x, y), y + yRand), Quaternion.identity);
                    Physics.SyncTransforms();
                }
                }else{
                     //Debug.Log("spawn aborted - highroll >" + Mathf.PerlinNoise(xCoord + propseedX, yCoord + propseedY));
                }
            }
        }

    }
    
    void PlaceProps(Props prop, float lowerroll, float upperroll)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float xCoord = (float)x / width * prop.scale;
                float yCoord = (float)y / height * prop.scale;
                if (UnityEngine.Random.Range(lowerroll, upperroll) < Mathf.PerlinNoise(xCoord + prop.seedX, yCoord + prop.seedY))
                {
                    float xRand = UnityEngine.Random.value;
                    float yRand = UnityEngine.Random.value;
                    //
                    int colliders = Physics.OverlapSphereNonAlloc(new Vector3(x+xRand, 0.1f+GetComponent<Terrain>().terrainData.GetHeight(x, y), y+yRand), 1.0f,propcheckbuffer);
                    if (colliders > 1) {
                    //Debug.Log("overlap detected - "+colliders);
                    }else{
                    //spawn prop
                    Instantiate(prop.item, new Vector3(x + xRand, GetComponent<Terrain>().terrainData.GetHeight(x, y), y + yRand), Quaternion.identity, PropParent.transform);
                     Physics.SyncTransforms();
                     }
                }
            }
        }
    }

//different prop placement system. Preferable for objectives and PoIs
        void PlaceProps(Props prop, float RadiusFromOther, float spawnRadius, Vector2 spawncenter, int NumberToPlace){
            //if shits sideways its because of THIS MOFO
            int placedprops = 0;
            int failedplacements = 0;
            
            while(placedprops < NumberToPlace){
            Vector2 randCircle = spawncenter+ (UnityEngine.Random.insideUnitCircle * spawnRadius);
            poiLocation = new Vector3(randCircle.x, GetComponent<Terrain>().terrainData.GetHeight((int)randCircle.x, (int)randCircle.y), randCircle.y);
            poiLocation.y = GetComponent<Terrain>().terrainData.GetHeight((int)poiLocation.x, (int)poiLocation.z);
            {
                //
                int colliders = Physics.OverlapSphereNonAlloc(poiLocation, RadiusFromOther, propcheckbuffer);
                if (colliders > 1) 
                {
                //Debug.Log("overlap detected - "+colliders);
                failedplacements++;
                if(failedplacements > 1000) //prop limit may need a variable
                    {
                    //Debug.Log("Too many failed placements - loop aborted");
                    return;
                    }
                }else{
                //spawn prop
                Instantiate(prop.item, poiLocation, Quaternion.identity, PropParent.transform);
                failedplacements = 0;
                Physics.SyncTransforms();
                placedprops++;

            }
            }

            }
        }

            
}