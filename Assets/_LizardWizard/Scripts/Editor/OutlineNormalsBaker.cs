using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

//-- bakes smoothed normals into uv3 on import so the toon outline doesn't split on hard edges
//-- same fix as the RVFX outline video, but done automatically instead of baking vertex colors in Houdini
public class OutlineNormalsBaker : AssetPostprocessor
{
    const string ArtFolder = "Assets/_LizardWizard/Art/";
    const int Channel = 3;

    //-- bump this to force every model to reimport
    public override uint GetVersion() => 3;

    void OnPostprocessModel(GameObject root)
    {
        if (!assetPath.StartsWith(ArtFolder)) return;

        var meshes = new HashSet<Mesh>();
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh != null) meshes.Add(filter.sharedMesh);
        }
        foreach (var skinned in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (skinned.sharedMesh != null) meshes.Add(skinned.sharedMesh);
        }

        foreach (var mesh in meshes) Bake(mesh);
    }

    static void Bake(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        if (normals.Length != vertices.Length) return;

        Vector4[] tangents = mesh.tangents;
        if (tangents.Length != vertices.Length)
        {
            mesh.RecalculateTangents();
            tangents = mesh.tangents;
        }
        if (tangents.Length != vertices.Length) tangents = new Vector4[vertices.Length];

        //-- tangents are junk on our models since they have no uvs, so square them up against the normal
        for (int i = 0; i < tangents.Length; i++)
        {
            Vector3 n = normals[i];
            Vector3 t = (Vector3)tangents[i];
            t -= n * Vector3.Dot(n, t);
            if (t.sqrMagnitude < 1e-6f) t = Vector3.Cross(n, Mathf.Abs(n.y) < 0.99f ? Vector3.up : Vector3.right);
            t.Normalize();
            tangents[i] = new Vector4(t.x, t.y, t.z, tangents[i].w < 0 ? -1f : 1f);
        }
        mesh.tangents = tangents;

        //-- average the face normals of every triangle touching the same spot, weighted by corner angle
        var smooth = new Dictionary<Vector3Int, Vector3>();
        int[] triangles = mesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
            Vector3 pa = vertices[a], pb = vertices[b], pc = vertices[c];

            //-- normalized by hand, Unity's .normalized returns zero on tiny triangles
            Vector3 face = Vector3.Cross(pb - pa, pc - pa);
            float length = face.magnitude;
            if (length < 1e-12f) continue;
            face /= length;
            if (Vector3.Dot(face, normals[a] + normals[b] + normals[c]) < 0) face = -face;

            Add(smooth, pa, face * Vector3.Angle(pb - pa, pc - pa));
            Add(smooth, pb, face * Vector3.Angle(pa - pb, pc - pb));
            Add(smooth, pc, face * Vector3.Angle(pa - pc, pb - pc));
        }

        //-- stored in tangent space so it still lines up after skinning
        var baked = new List<Vector3>(vertices.Length);
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 n = normals[i];
            Vector3 t = (Vector3)tangents[i];

            if (!smooth.TryGetValue(Key(vertices[i]), out Vector3 s) || s == Vector3.zero)
            {
                //-- shader falls back to the normal normal
                baked.Add(Vector3.zero);
                continue;
            }

            s.Normalize();
            Vector3 bitangent = Vector3.Cross(n, t) * (tangents[i].w < 0 ? -1f : 1f);
            baked.Add(new Vector3(Vector3.Dot(s, t), Vector3.Dot(s, bitangent), Vector3.Dot(s, n)));
        }

        mesh.SetUVs(Channel, baked);
    }

    static void Add(Dictionary<Vector3Int, Vector3> smooth, Vector3 position, Vector3 normal)
    {
        Vector3Int key = Key(position);
        smooth.TryGetValue(key, out Vector3 sum);
        smooth[key] = sum + normal;
    }

    //-- rounding so split verts at the same spot match
    static Vector3Int Key(Vector3 p)
    {
        return new Vector3Int(Mathf.RoundToInt(p.x * 100000f), Mathf.RoundToInt(p.y * 100000f), Mathf.RoundToInt(p.z * 100000f));
    }
}
