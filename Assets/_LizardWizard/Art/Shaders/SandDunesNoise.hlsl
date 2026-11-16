//-- noise + polygon code from the sand dune tutorial https://youtu.be/KqWfo6EPjCw (see SandDunes_Credits.txt)

#ifndef SAND_DUNES_NOISE
#define SAND_DUNES_NOISE

float SandRand2To1(float2 value, float2 dotDir)
{
    float2 smallValue = sin(value);
    float random = dot(smallValue, dotDir);
    return frac(sin(random) * 143758.5453);
}

float2 SandRand2To2(float2 value)
{
    return float2(
        SandRand2To1(value, float2(12.989, 78.233)),
        SandRand2To1(value, float2(39.346, 11.135)));
}

float3 SandRand1To3(float value)
{
    return frac(sin(value + float3(3.9812, 7.1536, 5.7241)) * 143758.5453);
}

float SandEaseInOut(float t)
{
    float easeIn = t * t;
    float easeOut = 1 - (1 - t) * (1 - t);
    return lerp(easeIn, easeOut, t);
}

//-- perlin
float SandPerlin(float2 value)
{
    float2 cellMin = floor(value);
    float2 cellMax = cellMin + 1;

    float2 dirLL = SandRand2To2(float2(cellMin.x, cellMin.y)) * 2 - 1;
    float2 dirLR = SandRand2To2(float2(cellMax.x, cellMin.y)) * 2 - 1;
    float2 dirUL = SandRand2To2(float2(cellMin.x, cellMax.y)) * 2 - 1;
    float2 dirUR = SandRand2To2(float2(cellMax.x, cellMax.y)) * 2 - 1;

    float2 f = frac(value);
    float ll = dot(dirLL, f - float2(0, 0));
    float lr = dot(dirLR, f - float2(1, 0));
    float ul = dot(dirUL, f - float2(0, 1));
    float ur = dot(dirUR, f - float2(1, 1));

    float tx = SandEaseInOut(f.x);
    float ty = SandEaseInOut(f.y);
    return lerp(lerp(ll, lr, tx), lerp(ul, ur, tx), ty);
}

//-- layered perlin 0-1
float SandLayeredNoise(float2 value, float persistence, float roughness, int octaves)
{
    float noise = 0;
    float frequency = 1;
    float factor = 1;
    for (int i = 0; i < octaves; i++)
    {
        noise += SandPerlin(value * frequency + i * 0.72354) * factor;
        factor *= persistence;
        frequency *= roughness;
    }
    return noise + 0.5;
}

//-- voronoi, x = cell dist, y = random
float2 SandVoronoi(float2 value)
{
    float2 baseCell = floor(value);
    float minDist = 10;
    float2 closestCell = baseCell;

    [unroll]
    for (int x = -1; x <= 1; x++)
    {
        [unroll]
        for (int y = -1; y <= 1; y++)
        {
            float2 cell = baseCell + float2(x, y);
            float2 cellPos = cell + SandRand2To2(cell);
            float dist = length(cellPos - value);
            if (dist < minDist)
            {
                minDist = dist;
                closestCell = cell;
            }
        }
    }

    float random = SandRand2To1(closestCell, float2(12.9898, 78.233));
    return float2(minDist, random);
}

//-- polygon with smooth edges
float SandPolygonGradient(float2 uv, float sides, float width, float height)
{
    float aWidth = width * cos(PI / sides);
    float aHeight = height * cos(PI / sides);
    float2 p = (uv * 2 - 1) / float2(aWidth, aHeight);
    p.y *= -1;
    float pCoord = atan2(p.x, p.y);
    float r = 2 * PI / sides;
    float dist = cos(floor(0.5 + pCoord / r) * r - pCoord) * length(p);
    return 1 - dist;
}

#endif
