using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class PlatformBeamFit : MonoBehaviour
{
    private MeshFilter meshFilter;
    private Mesh originalMesh;
    private Mesh fittedMesh;

    public void Fit(float width, float length, Transform floor)
    {
        meshFilter = GetComponent<MeshFilter>();
        originalMesh = meshFilter.sharedMesh;

        if (originalMesh == null || !originalMesh.isReadable)
        {
            Debug.LogError("PlatformBeamFit: Mesh missing or Read/Write disabled!", this);
            return;
        }

        fittedMesh = Instantiate(originalMesh);
        fittedMesh.name = "PlatformBeam_Fitted";

        Vector3[] vertices = fittedMesh.vertices;
        Bounds bounds = originalMesh.bounds;

        float topY = bounds.max.y;
        float tolerance = Mathf.Max(bounds.size.y * 0.001f, 0.00001f);
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;
        int topCount = 0;

        foreach (Vector3 vertex in vertices)
        {
            if (Mathf.Abs(vertex.y - topY) > tolerance) continue;

            minX = Mathf.Min(minX, vertex.x);
            maxX = Mathf.Max(maxX, vertex.x);
            minZ = Mathf.Min(minZ, vertex.z);
            maxZ = Mathf.Max(maxZ, vertex.z);
            topCount++;
        }

        if (topCount == 0 || maxX <= minX || maxZ <= minZ)
        {
            Debug.LogError("PlatformBeamFit: Cannot find beam top!", this);
            Destroy(fittedMesh);
            fittedMesh = null;
            return;
        }

        Vector3[] source =
        {
            new Vector3(minX, topY, minZ),
            new Vector3(maxX, topY, minZ),
            new Vector3(maxX, topY, maxZ),
            new Vector3(minX, topY, maxZ)
        };

        Vector3[] target =
        {
            new Vector3(-width / 2f, 0, -length / 2f),
            new Vector3( width / 2f, 0, -length / 2f),
            new Vector3( width / 2f, 0,  length / 2f),
            new Vector3(-width / 2f, 0,  length / 2f)
        };

        for (int i = 0; i < 4; i++)
        {
            target[i] = transform.InverseTransformPoint(floor.TransformPoint(target[i]));
        }

        int bestOffset = 0;
        bool bestReversed = false;
        float bestScore = float.MaxValue;

        for (int reverse = 0; reverse < 2; reverse++)
        {
            for (int offset = 0; offset < 4; offset++)
            {
                float score = 0f;
                for (int i = 0; i < 4; i++)
                {
                    int j = reverse == 0 ? (i + offset) % 4 : (offset - i + 4) % 4;
                    score += (source[i] - target[j]).sqrMagnitude;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    bestOffset = offset;
                    bestReversed = reverse == 1;
                }
            }
        }

        Vector3[] corners = new Vector3[4];

        for (int i = 0; i < 4; i++)
        {
            int j = bestReversed ? (bestOffset - i + 4) % 4 : (i + bestOffset) % 4;
            corners[i] = target[j];
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];

            if (Mathf.Abs(vertex.y - topY) > tolerance) continue;
            float u = Mathf.InverseLerp(minX, maxX, vertex.x);
            float v = Mathf.InverseLerp(minZ, maxZ, vertex.z);

            Vector3 front = Vector3.Lerp(corners[0], corners[1], u);
            Vector3 back = Vector3.Lerp(corners[3], corners[2], u);
            vertices[i] = Vector3.Lerp(front, back, v);
        }

        fittedMesh.vertices = vertices;
        fittedMesh.RecalculateBounds();
        fittedMesh.RecalculateNormals();
        meshFilter.sharedMesh = fittedMesh;

        Debug.Log($"PlatformBeamFit: Beam fitted to {width:F2} x {length:F2}", this);
    }

    private void OnDestroy()
    {
        if (meshFilter != null && meshFilter.sharedMesh == fittedMesh) meshFilter.sharedMesh = originalMesh;
        if (fittedMesh != null)  Destroy(fittedMesh);
    }
}
