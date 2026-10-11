using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    private int width = 1024;
    private int height = 1024;
    //width and height of the terrain. Must be equal! 
    // it scales the dunes with it- increasing it will NOT make more dunes it will make BIGGER dunes
    private int depth = 25;
    //depth is how high the hills go.
    private float scale = 80f;
    //scale of the perlin noise map. higher = more dunes across the map.
    private float multscale = 10f;
    public float propscale = 100f;
    //scale of the perlin noise multiplier map. lower means larger areas of flat and hilly terrain

    //Seeds. They each have an X and Y of where in the perlin noise map they are. 
    // Could squash some of them together for simplification and easier map seed gen but idk for now
    private float seedX = 0f;
    private float seedY = 0f;
    private float multseedX = 0f;
    private float multseedY = 0f;
    private float propseedX = 0f;
    private float propseedY = 0f;
    public float propRollLowerBound;
    public float propRollUpperBound;
    public GameObject prop1;
    private Collider[] propcheckbuffer;
    private Vector3 centerOfMap;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       centerOfMap = new Vector3(width/2f, depth/2f, height/2f);
        //uncomment this for randomization
        //seedX = Random.Range(0f,100000f);
        //seedY = Random.Range(0f,100000f);
        //propseedX = Random.Range(0f,100000f);
        //propseedY = Random.Range(0f,100000f);
        Terrain terrain = GetComponent<Terrain>();
        terrain.terrainData = GenerateTerrain(terrain.terrainData);
        PlaceProps(prop1);

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

    void PlaceProps(GameObject prop)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float xCoord = (float)x / width * propscale;
                float yCoord = (float)y / height * propscale;
                if (Random.Range(0f, 100f)*Mathf.PerlinNoise(xCoord + propseedX, yCoord + propseedY)>propRollUpperBound)
                {
                    float xRand = Random.value;
                    float yRand = Random.value;
                    //
                    var colliders = Physics.OverlapSphereNonAlloc(new Vector3(x+xRand, 0.1f+GetComponent<Terrain>().terrainData.GetHeight(x, y), y+yRand), 0.0f,propcheckbuffer);
                    if (colliders > 0) {
                    //Debug.Log("overlap detected - "+colliders[0].name);
                    }else{
                    //spawn prop
                    Instantiate(prop, new Vector3(x + xRand, GetComponent<Terrain>().terrainData.GetHeight(x, y), y + yRand), Quaternion.identity);

                }
                }else{
                     //Debug.Log("spawn aborted - highroll >" + Mathf.PerlinNoise(xCoord + propseedX, yCoord + propseedY));
                }
            }
        }

    }
    //doing a test of a different prop placement system. Preferable for objectives and PoIs
    void PlaceProps2(GameObject prop, float radius){
        
    }
}
