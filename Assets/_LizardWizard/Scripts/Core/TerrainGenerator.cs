using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public int width = 1024;
    public int height = 1024;
    public int depth = 25;
    public float scale = 80f;
    public float multscale = 10f;
    public float seedX = 0f;
    public float seedY = 0f;
    public float propseedX = 0f;
    public float propseedY = 0f;
    public GameObject prop1;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //uncomment this for randomization
        //seedX = Random.Range(0f,100000f);
        //seedY = Random.Range(0f,100000f);
        //propseedX = Random.Range(0f,100000f);
        //propseedY = Random.Range(0f,100000f);
        Terrain terrain = GetComponent<Terrain>();
        terrain.terrainData = GenerateTerrain(terrain.terrainData);
        // PlaceProps(prop1);

    }
    /* DO NOT UNCOMMENT 
    this is for live update terrain. any large size (>500x500) will start slowing computers.
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
        float ScalexCoord = (float)x / width * multscale;
        float ScaleyCoord = (float)y / height * multscale;
        float xCoord = (float)x / width * scale;
        float yCoord = (float)y / height * scale;
        return Mathf.PerlinNoise(xCoord + seedX, yCoord + seedY) * Mathf.PerlinNoise(ScalexCoord, ScaleyCoord);
    }

    void PlaceProps(GameObject prop)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float xCoord = (float)x / width * scale;
                float yCoord = (float)y / height * scale;
                if (Random.value < Mathf.PerlinNoise(xCoord + propseedX, yCoord + propseedY))
                {
                    Instantiate(prop, new Vector3(x + Random.value, GetComponent<Terrain>().terrainData.GetHeight(x, y), y + Random.value), Quaternion.identity);
                }
            }
        }

    }
}
