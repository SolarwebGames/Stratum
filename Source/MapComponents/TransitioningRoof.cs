using UnityEngine;

namespace SolarWeb.Stratum.MapComponents;

public class TransitioningRoof
{
  public Vector3 startPos;
  public Vector3 endPos;
  public int startTick;
  public int durationTicks;

  public Material material = default!;
  public Color color;

  public Mesh mesh = default!;

  public void Init(Vector3 start, Vector3 end, int currentTick, int duration, Material mat, Color col, Vector2[] uvs, Mesh assignedMesh)
  {
    startPos = start;
    endPos = end;
    startTick = currentTick;
    durationTicks = duration;
    material = mat;
    color = col;
    mesh = assignedMesh;

    mesh.vertices = [
      new Vector3(-0.5f, 0, -0.5f),
      new Vector3(-0.5f, 0,  0.5f),
      new Vector3( 0.5f, 0,  0.5f),
      new Vector3( 0.5f, 0, -0.5f)
    ];

    mesh.uv = [
      uvs[0],
      uvs[1],
      uvs[2],
      uvs[3]
    ];

    mesh.triangles = [0, 1, 2, 0, 2, 3];
    mesh.normals = [Vector3.up, Vector3.up, Vector3.up, Vector3.up];
    mesh.colors = [col, col, col, col];
  }

  public float GetProgress(int currentTick)
  {
    if (durationTicks <= 0) return 1f;
    float p = (float)(currentTick - startTick) / durationTicks;
    return Mathf.Clamp01(p);
  }

  public Vector3 GetCurrentPosition(int currentTick)
  {
    float p = GetProgress(currentTick);
    float ease = p * p * (3f - 2f * p);
    return Vector3.Lerp(startPos, endPos, ease);
  }
}
