using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

//-- Bakes the NavMesh on start since the terrain + props are generated
[RequireComponent(typeof(NavMeshSurface))]
public class RuntimeNavMesh : MonoBehaviour
{
    private NavMeshSurface surface;

    IEnumerator Start()
    {
        surface = GetComponent<NavMeshSurface>();

        //-- Wait a frame so TerrainGenerator is done placing everything
        yield return null;

        surface.RemoveData();
        surface.navMeshData = NavMeshBuilder.BuildNavMeshData(
            surface.GetBuildSettings(), new List<NavMeshBuildSource>(), new Bounds(),
            transform.position, transform.rotation);
        surface.AddData();

        //-- Builds in the background so it doesn't freeze
        float startTime = Time.realtimeSinceStartup;
        yield return surface.UpdateNavMesh(surface.navMeshData);

        Debug.Log("NavMesh baked in " + (Time.realtimeSinceStartup - startTime).ToString("F2") + "s");
    }
}
