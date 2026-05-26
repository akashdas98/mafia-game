using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Target))]
public class TargetEditor : Editor
{
  public override void OnInspectorGUI()
  {
    DrawDefaultInspector();
    EditorGUILayout.HelpBox("Oblique Loft debug drawing is controlled here. Target and obstacle objects only need ObliqueLoftCollider components to participate in the new hit test.", MessageType.Info);
  }

  private void OnSceneGUI()
  {
    Target targetComponent = (Target)target;
    if (!targetComponent.DrawObliqueLoftDebug || !targetComponent.HasObliqueDebugRay)
    {
      return;
    }

    ObliqueRay ray = targetComponent.LastObliqueDebugRay;
    bool blocked = targetComponent.LastObliqueDebugBlocked;
    Vector3 labelPoint = blocked
      ? targetComponent.LogicPointToScenePoint(targetComponent.LastObliqueDebugHit.Point)
      : targetComponent.LogicPointToScenePoint(ray.To);

    string label = blocked
      ? "Oblique hit " + targetComponent.LastObliqueDebugHit.SurfaceType + " face #" + targetComponent.LastObliqueDebugHit.FaceIndex + "\n" + targetComponent.LastObliqueDebugHit.HitObject.name
      : "Oblique clear";

    if (!string.IsNullOrEmpty(targetComponent.LastObliqueDebugStatus))
    {
      label += "\n" + targetComponent.LastObliqueDebugStatus;
    }

    if (blocked)
    {
      DrawHitFace(targetComponent);
    }

    Handles.Label(labelPoint + Vector3.up * 0.15f, label);
  }

  private void DrawHitFace(Target targetComponent)
  {
    ObliqueRayHit hit = targetComponent.LastObliqueDebugHit;
    if (hit.Collider == null ||
      hit.FaceIndex < 0 ||
      !TryGetGeneratedFace(hit.Collider, hit.FaceIndex, out ObliqueLoftFace face))
    {
      return;
    }

    Vector3[] scenePoints = new Vector3[face.Vertices.Count];
    for (int i = 0; i < face.Vertices.Count; i++)
    {
      scenePoints[i] = targetComponent.LogicPointToScenePoint(hit.Collider.LocalToLogicWorld(face.Vertices[i]));
    }

    Color fill = new Color(0.55f, 1f, 0.55f, 0.22f);
    Color outline = new Color(0.2f, 1f, 0.2f, 0.95f);
    Handles.color = fill;
    Handles.DrawAAConvexPolygon(scenePoints);
    Handles.color = outline;
    for (int i = 0; i < scenePoints.Length; i++)
    {
      Handles.DrawAAPolyLine(4f, scenePoints[i], scenePoints[(i + 1) % scenePoints.Length]);
    }
  }

  private bool TryGetGeneratedFace(ObliqueLoftCollider collider, int faceIndex, out ObliqueLoftFace face)
  {
    foreach (ObliqueLoftFace candidate in collider.GeneratedFaces)
    {
      if (candidate.FaceIndex == faceIndex)
      {
        face = candidate;
        return true;
      }
    }

    face = null;
    return false;
  }
}
