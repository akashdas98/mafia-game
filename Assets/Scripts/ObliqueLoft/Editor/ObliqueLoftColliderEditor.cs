using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ObliqueLoftCollider))]
public class ObliqueLoftColliderEditor : Editor
{
  private enum EditMode
  {
    None,
    Footprint,
    Slice
  }

  private enum SelectionKind
  {
    None,
    FootprintPoint,
    SlicePoint
  }

  private const float DefaultNudgeStep = 1f / 32f;
  private const float MinimumSliceSpacing = 0.001f;
  private const float MinimumFlatEdgeLength = 1f / 32f;
  private const float SnapProximity = 1f / 128f;
  private const float ConnectorYTolerance = 0.0001f;
  private const float PolygonPositionSliderMin = -20f;
  private const float PolygonPositionSliderMax = 20f;
  private static readonly Color FootprintColor = new Color(1f, 0.62f, 0.08f);
  private static readonly Color FrontSliceColor = Color.cyan;
  private static readonly Color BackSliceColor = new Color(0.18f, 0.45f, 1f);
  private static readonly Color MiddleSliceColor = Color.green;
  private static readonly Color SelectedColor = Color.white;
  private static readonly Color HoverColor = new Color(1f, 1f, 0.45f);

  private EditMode editMode = EditMode.Footprint;
  private SelectionKind selectionKind = SelectionKind.None;
  private int selectedSliceIndex;
  private int selectedPointIndex = -1;
  private int selectedFootprintFrontPartnerIndex = -1;
  private int selectedFootprintBackPartnerIndex = -1;
  private bool drawFaceLabels = true;
  private float nudgeStep = DefaultNudgeStep;
  private bool isNormalizingSlices;
  private SerializedProperty useInRaycastsProperty;
  private SerializedProperty showGizmosProperty;

  private void OnEnable()
  {
    useInRaycastsProperty = serializedObject.FindProperty("useInRaycasts");
    showGizmosProperty = serializedObject.FindProperty("showGizmos");
  }

  public override void OnInspectorGUI()
  {
    ObliqueLoftCollider collider = (ObliqueLoftCollider)target;
    NormalizeSlicesToFootprint(collider, false);

    serializedObject.Update();
    EditorGUILayout.PropertyField(useInRaycastsProperty);
    EditorGUILayout.PropertyField(showGizmosProperty, new GUIContent("Show Generated Face Gizmos"));
    serializedObject.ApplyModifiedProperties();

    EditorGUILayout.Space();
    EditorGUILayout.LabelField("Collider Data", EditorStyles.boldLabel);
    EditorGUILayout.LabelField("Footprint Points", collider.EditableFootprint.Count.ToString());
    EditorGUILayout.LabelField("Slices", collider.EditableSlices.Count.ToString());
    EditorGUILayout.LabelField("Generated Faces", collider.GeneratedFaces.Count.ToString());

    EditorGUILayout.Space();
    EditorGUILayout.LabelField("Oblique Loft Tools", EditorStyles.boldLabel);

    EditorGUILayout.BeginHorizontal();
    if (GUILayout.Toggle(editMode == EditMode.Footprint, "Edit Footprint", "Button"))
    {
      editMode = EditMode.Footprint;
    }
    else if (editMode == EditMode.Footprint)
    {
      editMode = EditMode.None;
    }

    if (GUILayout.Toggle(editMode == EditMode.Slice, "Edit Slice", "Button"))
    {
      editMode = EditMode.Slice;
    }
    else if (editMode == EditMode.Slice)
    {
      editMode = EditMode.None;
    }
    EditorGUILayout.EndHorizontal();

    DrawSliceControls(collider);
    DrawPolygonPositionControls(collider);

    EditorGUILayout.BeginHorizontal();
    if (GUILayout.Button("Reset Box"))
    {
      Undo.RecordObject(collider, "Reset Oblique Loft Box");
      collider.ResetToBox();
      NormalizeSlicesToFootprint(collider, false);
      ClearSelection();
      EditorUtility.SetDirty(collider);
    }

    if (GUILayout.Button("Rebuild"))
    {
      Undo.RecordObject(collider, "Rebuild Oblique Loft Collider");
      collider.Rebuild();
      EditorUtility.SetDirty(collider);
    }
    EditorGUILayout.EndHorizontal();

    EditorGUILayout.BeginHorizontal();
    if (GUILayout.Button("Add Middle Slice"))
    {
      AddMiddleSlice(collider);
    }
    EditorGUILayout.EndHorizontal();

    drawFaceLabels = EditorGUILayout.Toggle("Draw Generated Face Labels", drawFaceLabels);
    nudgeStep = Mathf.Max(0.0001f, EditorGUILayout.FloatField("Arrow Nudge Step", nudgeStep));

    DrawSelectionStatus(collider);

    if (collider.ValidationErrors.Count > 0)
    {
      EditorGUILayout.Space();
      EditorGUILayout.HelpBox(string.Join("\n", collider.ValidationErrors), MessageType.Error);
    }

    EditorGUILayout.HelpBox("Click points to select and drag them. Arrow keys nudge the selected point; Shift+Arrow nudges 10x. Click an edge to insert a point. Right-click a point for options. Delete removes the selected point. Footprint point count is independent; slice add/delete is synchronized across all slices.", MessageType.Info);
  }

  private void OnSceneGUI()
  {
    ObliqueLoftCollider collider = (ObliqueLoftCollider)target;
    NormalizeSlicesToFootprint(collider, false);
    Event current = Event.current;

    HandleKeyboard(collider, current);

    if (collider.ShowGizmos)
    {
      DrawGeneratedFaceShading(collider);
    }

    if (drawFaceLabels)
    {
      DrawFaceLabels(collider);
    }

    DrawFootprint(collider, editMode == EditMode.Footprint);
    DrawSlices(collider, editMode == EditMode.Slice);

    if (editMode != EditMode.None)
    {
      HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
    }
  }

  private void DrawSliceControls(ObliqueLoftCollider collider)
  {
    if (collider.EditableSlices.Count == 0)
    {
      return;
    }

    selectedSliceIndex = Mathf.Clamp(selectedSliceIndex, 0, collider.EditableSlices.Count - 1);
    string[] labels = collider.EditableSlices
      .Select((slice, index) => index + ": " + slice.Name + " depth " + slice.Depth.ToString("0.###"))
      .ToArray();
    selectedSliceIndex = EditorGUILayout.Popup("Selected Slice", selectedSliceIndex, labels);

    ObliqueLoftSlice selectedSlice = collider.EditableSlices[selectedSliceIndex];
    using (new EditorGUI.DisabledScope(IsBoundarySlice(collider, selectedSliceIndex)))
    {
      EditorGUI.BeginChangeCheck();
      float newDepth = EditorGUILayout.FloatField("Slice Depth", selectedSlice.Depth);
      if (EditorGUI.EndChangeCheck())
      {
        Undo.RecordObject(collider, "Change Oblique Loft Slice Depth");
        selectedSlice.SetDepth(ClampMiddleSliceDepth(collider, selectedSliceIndex, SnapDepthToFootprint(newDepth, collider)));
        SortSlices(collider);
        selectedSliceIndex = Mathf.Clamp(collider.EditableSlices.IndexOf(selectedSlice), 0, collider.EditableSlices.Count - 1);
        NormalizeSlicesToFootprint(collider, false);
        collider.Rebuild();
        EditorUtility.SetDirty(collider);
      }
    }

    using (new EditorGUI.DisabledScope(!CanRemoveSelectedMiddleSlice(collider)))
    {
      if (GUILayout.Button("Remove Selected Middle Slice"))
      {
        RemoveSelectedSlice(collider);
      }
    }

    EditorGUILayout.HelpBox("Slice Depth is the Y position on the footprint that this cross-section belongs to. Drag the dotted connector handle for middle slices. Front/back connectors are locked to the footprint ends.", MessageType.None);
  }

  private void DrawPolygonPositionControls(ObliqueLoftCollider collider)
  {
    EditorGUILayout.Space();
    EditorGUILayout.LabelField("Polygon Position", EditorStyles.boldLabel);

    DrawFootprintPositionControls(collider);
    DrawSelectedSlicePositionControls(collider);
  }

  private void DrawFootprintPositionControls(ObliqueLoftCollider collider)
  {
    using (new EditorGUI.DisabledScope(collider.EditableFootprint.Count == 0))
    {
      Vector2 center = GetPointCenter(collider.EditableFootprint);
      EditorGUI.BeginChangeCheck();
      float newX = EditorGUILayout.Slider("Footprint Position X", center.x, PolygonPositionSliderMin, PolygonPositionSliderMax);
      float newY = EditorGUILayout.Slider("Footprint Position Y", center.y, PolygonPositionSliderMin, PolygonPositionSliderMax);
      if (EditorGUI.EndChangeCheck())
      {
        Undo.RecordObject(collider, "Move Oblique Loft Footprint");
        TranslatePoints(collider.EditableFootprint, new Vector2(newX - center.x, newY - center.y));
        NormalizeSlicesToFootprint(collider, false);
        collider.Rebuild();
        EditorUtility.SetDirty(collider);
      }
    }
  }

  private void DrawSelectedSlicePositionControls(ObliqueLoftCollider collider)
  {
    if (collider.EditableSlices.Count == 0)
    {
      return;
    }

    selectedSliceIndex = Mathf.Clamp(selectedSliceIndex, 0, collider.EditableSlices.Count - 1);
    ObliqueLoftSlice selectedSlice = collider.EditableSlices[selectedSliceIndex];
    using (new EditorGUI.DisabledScope(selectedSlice.EditablePoints.Count == 0))
    {
      Vector2 center = GetPointCenter(selectedSlice.EditablePoints);
      EditorGUI.BeginChangeCheck();
      float newX = EditorGUILayout.Slider("Selected Slice Position X", center.x, PolygonPositionSliderMin, PolygonPositionSliderMax);
      float newY = EditorGUILayout.Slider("Selected Slice Position Y", center.y, PolygonPositionSliderMin, PolygonPositionSliderMax);
      if (EditorGUI.EndChangeCheck())
      {
        Undo.RecordObject(collider, "Move Oblique Loft Slice");
        Vector2 requestedDelta = new Vector2(newX - center.x, newY - center.y);
        TranslateSlicePoints(selectedSlice, requestedDelta);
        NormalizeSlicesToFootprint(collider, false);
        collider.Rebuild();
        EditorUtility.SetDirty(collider);
      }
    }
  }

  private void DrawSelectionStatus(ObliqueLoftCollider collider)
  {
    string selection = "None";
    if (selectionKind == SelectionKind.FootprintPoint && IsValidFootprintPoint(collider, selectedPointIndex))
    {
      selection = "Footprint point " + selectedPointIndex;
    }
    else if (selectionKind == SelectionKind.SlicePoint && IsValidSlicePoint(collider, selectedSliceIndex, selectedPointIndex))
    {
      selection = "Slice " + selectedSliceIndex + " point " + selectedPointIndex;
    }

    EditorGUILayout.LabelField("Selection", selection);
  }

  private Vector2 GetPointCenter(System.Collections.Generic.IReadOnlyList<Vector2> points)
  {
    if (points == null || points.Count == 0)
    {
      return Vector2.zero;
    }

    Vector2 total = Vector2.zero;
    for (int i = 0; i < points.Count; i++)
    {
      total += points[i];
    }

    return total / points.Count;
  }

  private void TranslatePoints(System.Collections.Generic.IList<Vector2> points, Vector2 delta)
  {
    for (int i = 0; i < points.Count; i++)
    {
      points[i] += delta;
    }
  }

  private void TranslateSlicePoints(ObliqueLoftSlice slice, Vector2 delta)
  {
    if (slice.EditablePoints.Count == 0)
    {
      return;
    }

    float minY = slice.EditablePoints.Min(point => point.y);
    if (minY + delta.y < slice.Depth)
    {
      delta.y = slice.Depth - minY;
    }

    TranslatePoints(slice.EditablePoints, delta);
  }

  private void AutoRepairSlicePolygon(ObliqueLoftCollider collider, int sliceIndex)
  {
    if (sliceIndex < 0 || sliceIndex >= collider.EditableSlices.Count)
    {
      return;
    }

    ObliqueLoftSlice slice = collider.EditableSlices[sliceIndex];
    slice.EnsurePointOrder();
    if (slice.EditablePoints.Count < 4)
    {
      return;
    }

    int repairLimit = slice.EditablePoints.Count * slice.EditablePoints.Count;
    for (int repairStep = 0; repairStep < repairLimit; repairStep++)
    {
      if (!TryGetFirstCrossingEdges(slice, out int firstEdgeIndex, out int secondEdgeIndex))
      {
        return;
      }

      ReverseConnectionRange(slice, firstEdgeIndex + 1, secondEdgeIndex);
    }
  }

  private bool TryGetFirstCrossingEdges(ObliqueLoftSlice slice, out int firstEdgeIndex, out int secondEdgeIndex)
  {
    firstEdgeIndex = -1;
    secondEdgeIndex = -1;
    slice.EnsurePointOrder();
    IReadOnlyList<int> order = slice.PointOrder;
    for (int i = 0; i < order.Count; i++)
    {
      int nextI = (i + 1) % order.Count;
      for (int j = i + 1; j < order.Count; j++)
      {
        int nextJ = (j + 1) % order.Count;
        if (i == j || nextI == j || nextJ == i)
        {
          continue;
        }

        if (SegmentsIntersect(
          slice.Points[order[i]],
          slice.Points[order[nextI]],
          slice.Points[order[j]],
          slice.Points[order[nextJ]]))
        {
          firstEdgeIndex = i;
          secondEdgeIndex = j;
          return true;
        }
      }
    }

    return false;
  }

  private void ReverseConnectionRange(ObliqueLoftSlice slice, int startIndex, int endIndex)
  {
    slice.ReverseConnectionRange(startIndex, endIndex);
  }

  private bool SegmentsIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
  {
    float o1 = Cross(b - a, c - a);
    float o2 = Cross(b - a, d - a);
    float o3 = Cross(d - c, a - c);
    float o4 = Cross(d - c, b - c);

    if (Mathf.Abs(o1) <= Mathf.Epsilon && IsPointOnSegment(a, b, c))
    {
      return true;
    }

    if (Mathf.Abs(o2) <= Mathf.Epsilon && IsPointOnSegment(a, b, d))
    {
      return true;
    }

    if (Mathf.Abs(o3) <= Mathf.Epsilon && IsPointOnSegment(c, d, a))
    {
      return true;
    }

    if (Mathf.Abs(o4) <= Mathf.Epsilon && IsPointOnSegment(c, d, b))
    {
      return true;
    }

    return o1 * o2 < 0f && o3 * o4 < 0f;
  }

  private bool IsPointOnSegment(Vector2 a, Vector2 b, Vector2 point)
  {
    return point.x >= Mathf.Min(a.x, b.x) - Mathf.Epsilon &&
      point.x <= Mathf.Max(a.x, b.x) + Mathf.Epsilon &&
      point.y >= Mathf.Min(a.y, b.y) - Mathf.Epsilon &&
      point.y <= Mathf.Max(a.y, b.y) + Mathf.Epsilon;
  }

  private float Cross(Vector2 a, Vector2 b)
  {
    return a.x * b.y - a.y * b.x;
  }

  private void DrawFootprint(ObliqueLoftCollider collider, bool editable)
  {
    for (int i = 0; i < collider.EditableFootprint.Count; i++)
    {
      int nextIndex = (i + 1) % collider.EditableFootprint.Count;
      Vector3 scenePoint = FootprintPointToScene(collider, collider.EditableFootprint[i]);
      Vector3 nextPoint = FootprintPointToScene(collider, collider.EditableFootprint[nextIndex]);

      Handles.color = FootprintColor;
      Handles.DrawAAPolyLine(3f, scenePoint, nextPoint);

      if (editable)
      {
        DrawEdgeButton(collider, scenePoint, nextPoint, i, false);
        DrawFootprintPoint(collider, i, scenePoint);
      }
      else
      {
        DrawPointDisc(scenePoint, FootprintColor, 0.045f);
      }

      Handles.color = FootprintColor;
      Handles.Label(scenePoint + Vector3.up * 0.06f, "F" + i);
    }
  }

  private void DrawSlices(ObliqueLoftCollider collider, bool editable)
  {
    if (collider.EditableSlices.Count == 0)
    {
      return;
    }

    for (int sliceIndex = 0; sliceIndex < collider.EditableSlices.Count; sliceIndex++)
    {
      ObliqueLoftSlice slice = collider.EditableSlices[sliceIndex];
      slice.EnsurePointOrder();
      Color sliceColor = GetSliceColor(collider, sliceIndex);
      bool activeSlice = editable && sliceIndex == selectedSliceIndex;
      DrawSliceDepthConnectors(collider, slice, sliceColor, activeSlice);

      for (int orderIndex = 0; orderIndex < slice.PointOrder.Count; orderIndex++)
      {
        int pointIndex = slice.GetConnectionPointIndex(orderIndex);
        int nextIndex = slice.GetConnectionPointIndex((orderIndex + 1) % slice.PointOrder.Count);
        Vector3 scenePoint = SlicePointToScene(collider, slice, pointIndex);
        Vector3 nextPoint = SlicePointToScene(collider, slice, nextIndex);

        Handles.color = sliceColor;
        Handles.DrawAAPolyLine(activeSlice ? 3f : 2f, scenePoint, nextPoint);

        if (activeSlice)
        {
          DrawEdgeButton(collider, scenePoint, nextPoint, orderIndex, true);
        }
      }

      for (int pointIndex = 0; pointIndex < slice.EditablePoints.Count; pointIndex++)
      {
        Vector3 scenePoint = SlicePointToScene(collider, slice, pointIndex);
        if (activeSlice)
        {
          DrawSlicePoint(collider, sliceIndex, pointIndex, scenePoint);
        }
        else
        {
          DrawPointDisc(scenePoint, sliceColor, 0.04f);
        }

        Handles.color = sliceColor;
        Handles.Label(scenePoint + Vector3.up * 0.06f, "S" + sliceIndex + ":" + pointIndex);
      }
    }
  }

  private void DrawSliceDepthConnectors(ObliqueLoftCollider collider, ObliqueLoftSlice slice, Color sliceColor, bool activeSlice)
  {
    if (collider.EditableFootprint.Count < 2 || slice.EditablePoints.Count < 2)
    {
      return;
    }

    GetFootprintDepthEndpoints(collider, slice.Depth, out Vector3 leftDepthPoint, out Vector3 rightDepthPoint);
    GetBottomConnectorPointIndices(slice, out int leftConnectorIndex, out int rightConnectorIndex);
    Vector3 leftSlicePoint = SlicePointToScene(collider, slice, leftConnectorIndex);
    Vector3 rightSlicePoint = SlicePointToScene(collider, slice, rightConnectorIndex);

    Color connectorColor = sliceColor;
    connectorColor.a = activeSlice ? 0.65f : 0.35f;
    Handles.color = connectorColor;
    Handles.DrawDottedLine(leftSlicePoint, leftDepthPoint, activeSlice ? 4f : 7f);
    Handles.DrawDottedLine(rightSlicePoint, rightDepthPoint, activeSlice ? 4f : 7f);

    int sliceIndex = collider.EditableSlices.IndexOf(slice);
    if (activeSlice && !IsBoundarySlice(collider, sliceIndex))
    {
      DrawSliceDepthHandle(collider, sliceIndex, (leftDepthPoint + rightDepthPoint) * 0.5f, sliceColor);
    }
  }

  private void DrawSliceDepthHandle(ObliqueLoftCollider collider, int sliceIndex, Vector3 position, Color sliceColor)
  {
    Handles.color = sliceColor;
    float size = HandleUtility.GetHandleSize(position) * 0.08f;

    EditorGUI.BeginChangeCheck();
    Vector3 moved = Handles.FreeMoveHandle(position, Quaternion.identity, size, Vector3.zero, Handles.RectangleHandleCap);
    if (!EditorGUI.EndChangeCheck())
    {
      return;
    }

    ObliqueLoftSlice slice = collider.EditableSlices[sliceIndex];
    Undo.RecordObject(collider, "Move Slice Depth Connector");
    float localDepth = SceneToFootprintPoint(collider, moved).y;
    slice.SetDepth(ClampMiddleSliceDepth(collider, sliceIndex, SnapDepthToFootprint(localDepth, collider)));
    SortSlices(collider);
    selectedSliceIndex = Mathf.Clamp(collider.EditableSlices.IndexOf(slice), 0, collider.EditableSlices.Count - 1);
    NormalizeSlicesToFootprint(collider, false);
    collider.Rebuild();
    EditorUtility.SetDirty(collider);
  }

  private void GetBottomConnectorPointIndices(ObliqueLoftSlice slice, out int leftIndex, out int rightIndex)
  {
    leftIndex = 0;
    rightIndex = 0;
    if (slice.EditablePoints.Count == 0)
    {
      return;
    }

    if (slice.EditablePoints.Count == 1)
    {
      return;
    }

    float bottomY = slice.EditablePoints.Min(point => point.y);
    int bottomTieCount = 0;
    for (int i = 0; i < slice.EditablePoints.Count; i++)
    {
      Vector2 point = slice.EditablePoints[i];
      if (Mathf.Abs(point.y - bottomY) > ConnectorYTolerance)
      {
        continue;
      }

      if (bottomTieCount == 0)
      {
        leftIndex = i;
        rightIndex = i;
      }
      else
      {
        if (point.x < slice.EditablePoints[leftIndex].x)
        {
          leftIndex = i;
        }

        if (point.x > slice.EditablePoints[rightIndex].x)
        {
          rightIndex = i;
        }
      }

      bottomTieCount++;
    }

    if (bottomTieCount >= 2)
    {
      return;
    }

    int lowestIndex = leftIndex;
    int secondIndex = FindSecondLowestPointIndex(slice, lowestIndex);
    if (slice.EditablePoints[secondIndex].x < slice.EditablePoints[lowestIndex].x)
    {
      leftIndex = secondIndex;
      rightIndex = lowestIndex;
    }
    else
    {
      leftIndex = lowestIndex;
      rightIndex = secondIndex;
    }
  }

  private int FindSecondLowestPointIndex(ObliqueLoftSlice slice, int lowestIndex)
  {
    int secondIndex = lowestIndex == 0 ? 1 : 0;
    for (int i = 0; i < slice.EditablePoints.Count; i++)
    {
      if (i == lowestIndex)
      {
        continue;
      }

      Vector2 point = slice.EditablePoints[i];
      Vector2 second = slice.EditablePoints[secondIndex];
      if (point.y < second.y - ConnectorYTolerance ||
        Mathf.Abs(point.y - second.y) <= ConnectorYTolerance &&
        Mathf.Abs(point.x - slice.EditablePoints[lowestIndex].x) > Mathf.Abs(second.x - slice.EditablePoints[lowestIndex].x))
      {
        secondIndex = i;
      }
    }

    return secondIndex;
  }

  private void DrawFootprintPoint(ObliqueLoftCollider collider, int pointIndex, Vector3 scenePoint)
  {
    bool selected = selectionKind == SelectionKind.FootprintPoint && selectedPointIndex == pointIndex;
    Handles.color = selected ? SelectedColor : FootprintColor;

    float size = HandleUtility.GetHandleSize(scenePoint) * (selected ? 0.075f : 0.055f);
    EditorGUI.BeginChangeCheck();
    Vector3 moved = Handles.FreeMoveHandle(scenePoint, Quaternion.identity, size, Vector3.zero, Handles.DotHandleCap);
    if (EditorGUI.EndChangeCheck())
    {
      SelectFootprintPoint(collider, pointIndex);
      Undo.RecordObject(collider, "Move Footprint Point");
      collider.EditableFootprint[pointIndex] = SnapPointToNearbyPoints(SceneToFootprintPoint(collider, moved), collider.EditableFootprint, pointIndex);
      EnforceMandatoryFootprintEdges(collider, pointIndex);
      NormalizeSlicesToFootprint(collider, false);
      collider.Rebuild();
      EditorUtility.SetDirty(collider);
    }

    HandlePointContextMenu(collider, SelectionKind.FootprintPoint, pointIndex, scenePoint);
  }

  private void DrawSlicePoint(ObliqueLoftCollider collider, int sliceIndex, int pointIndex, Vector3 scenePoint)
  {
    bool selected = selectionKind == SelectionKind.SlicePoint && selectedSliceIndex == sliceIndex && selectedPointIndex == pointIndex;
    Color color = selected ? SelectedColor : GetSliceColor(collider, sliceIndex);
    Handles.color = color;

    float size = HandleUtility.GetHandleSize(scenePoint) * (selected ? 0.075f : 0.055f);
    EditorGUI.BeginChangeCheck();
    Vector3 moved = Handles.FreeMoveHandle(scenePoint, Quaternion.identity, size, Vector3.zero, Handles.DotHandleCap);
    if (EditorGUI.EndChangeCheck())
    {
      SelectSlicePoint(sliceIndex, pointIndex);
      Undo.RecordObject(collider, "Move Slice Point");
      ObliqueLoftSlice slice = collider.EditableSlices[sliceIndex];
      Vector2 snapped = SnapPointToNearbyPoints(SceneToSlicePoint(collider, moved), slice.EditablePoints, pointIndex);
      slice.EditablePoints[pointIndex] = ClampSlicePoint(collider, slice, snapped);
      AutoRepairSlicePolygon(collider, sliceIndex);
      collider.Rebuild();
      EditorUtility.SetDirty(collider);
    }

    HandlePointContextMenu(collider, SelectionKind.SlicePoint, pointIndex, scenePoint);
  }

  private void DrawEdgeButton(ObliqueLoftCollider collider, Vector3 a, Vector3 b, int edgeIndex, bool sliceEdge)
  {
    Vector3 midpoint = (a + b) * 0.5f;
    Handles.color = HoverColor;
    float size = HandleUtility.GetHandleSize(midpoint) * 0.04f;
    if (Handles.Button(midpoint, Quaternion.identity, size, size, Handles.RectangleHandleCap))
    {
      if (sliceEdge)
      {
        InsertSlicePointAfterEdge(collider, edgeIndex, 0.5f);
      }
      else
      {
        InsertFootprintPointAfterEdge(collider, edgeIndex, 0.5f);
      }

      Repaint();
    }
  }

  private void HandlePointContextMenu(ObliqueLoftCollider collider, SelectionKind kind, int pointIndex, Vector3 scenePoint)
  {
    Event current = Event.current;
    if (current.type != EventType.ContextClick)
    {
      return;
    }

    float distance = HandleUtility.DistanceToCircle(scenePoint, HandleUtility.GetHandleSize(scenePoint) * 0.08f);
    if (distance > 8f)
    {
      return;
    }

    if (kind == SelectionKind.FootprintPoint)
    {
      SelectFootprintPoint(collider, pointIndex);
    }
    else
    {
      SelectSlicePoint(selectedSliceIndex, pointIndex);
    }

    GenericMenu menu = new GenericMenu();
    menu.AddItem(new GUIContent("Delete Point"), false, () => DeleteSelectedPoint(collider));
    menu.ShowAsContext();
    current.Use();
  }

  private void HandleKeyboard(ObliqueLoftCollider collider, Event current)
  {
    if (current.type != EventType.KeyDown)
    {
      return;
    }

    if (current.keyCode == KeyCode.Delete || current.keyCode == KeyCode.Backspace)
    {
      DeleteSelectedPoint(collider);
      current.Use();
      return;
    }

    Vector2 delta = GetArrowDelta(current);
    if (delta == Vector2.zero)
    {
      return;
    }

    float multiplier = current.shift ? 10f : 1f;
    NudgeSelectedPoint(collider, delta * nudgeStep * multiplier);
    current.Use();
  }

  private Vector2 GetArrowDelta(Event current)
  {
    switch (current.keyCode)
    {
      case KeyCode.LeftArrow:
        return Vector2.left;
      case KeyCode.RightArrow:
        return Vector2.right;
      case KeyCode.UpArrow:
        return Vector2.up;
      case KeyCode.DownArrow:
        return Vector2.down;
      default:
        return Vector2.zero;
    }
  }

  private void NudgeSelectedPoint(ObliqueLoftCollider collider, Vector2 delta)
  {
    if (selectionKind == SelectionKind.FootprintPoint && IsValidFootprintPoint(collider, selectedPointIndex))
    {
      Undo.RecordObject(collider, "Nudge Footprint Point");
      collider.EditableFootprint[selectedPointIndex] += delta;
      EnforceMandatoryFootprintEdges(collider, selectedPointIndex);
      NormalizeSlicesToFootprint(collider, false);
      collider.Rebuild();
      EditorUtility.SetDirty(collider);
    }
    else if (selectionKind == SelectionKind.SlicePoint && IsValidSlicePoint(collider, selectedSliceIndex, selectedPointIndex))
    {
      Undo.RecordObject(collider, "Nudge Slice Point");
      ObliqueLoftSlice slice = collider.EditableSlices[selectedSliceIndex];
      slice.EditablePoints[selectedPointIndex] = ClampSlicePoint(collider, slice, slice.EditablePoints[selectedPointIndex] + delta);
      AutoRepairSlicePolygon(collider, selectedSliceIndex);
      collider.Rebuild();
      EditorUtility.SetDirty(collider);
    }
  }

  private void InsertFootprintPointAfterEdge(ObliqueLoftCollider collider, int edgeIndex, float t)
  {
    if (collider.EditableFootprint.Count < 2)
    {
      return;
    }

    int nextIndex = (edgeIndex + 1) % collider.EditableFootprint.Count;
    Vector2 inserted = Vector2.Lerp(collider.EditableFootprint[edgeIndex], collider.EditableFootprint[nextIndex], t);

    Undo.RecordObject(collider, "Insert Footprint Point");
    collider.EditableFootprint.Insert(nextIndex, inserted);
    EnforceMandatoryFootprintEdges(collider, nextIndex);
    NormalizeSlicesToFootprint(collider, false);
    collider.Rebuild();
    SelectFootprintPoint(collider, nextIndex);
    EditorUtility.SetDirty(collider);
  }

  private void InsertSlicePointAfterEdge(ObliqueLoftCollider collider, int edgeIndex, float t)
  {
    ObliqueLoftSlice reference = collider.EditableSlices.FirstOrDefault(slice => slice.EditablePoints.Count >= 2);
    if (reference == null || reference.EditablePoints.Count < 2)
    {
      return;
    }

    reference.EnsurePointOrder();
    int pointIndex = reference.GetConnectionPointIndex(edgeIndex);
    int nextIndex = reference.GetConnectionPointIndex((edgeIndex + 1) % reference.PointOrder.Count);

    Undo.RecordObject(collider, "Insert Slice Point In All Slices");
    foreach (ObliqueLoftSlice slice in collider.EditableSlices)
    {
      slice.EnsurePointOrder();
      Vector2 a = slice.EditablePoints[pointIndex];
      Vector2 b = slice.EditablePoints[nextIndex];
      int insertedIndex = slice.EditablePoints.Count;
      slice.EditablePoints.Add(Vector2.Lerp(a, b, t));
      slice.InsertConnectionPointAfter(edgeIndex, insertedIndex);
    }

    collider.Rebuild();
    SelectSlicePoint(selectedSliceIndex, collider.EditableSlices[selectedSliceIndex].GetConnectionPointIndex(edgeIndex + 1));
    EditorUtility.SetDirty(collider);
  }

  private void DeleteSelectedPoint(ObliqueLoftCollider collider)
  {
    if (selectionKind == SelectionKind.FootprintPoint && IsValidFootprintPoint(collider, selectedPointIndex))
    {
      if (collider.EditableFootprint.Count <= 3)
      {
        return;
      }

      Undo.RecordObject(collider, "Delete Footprint Point");
      collider.EditableFootprint.RemoveAt(selectedPointIndex);
      selectedPointIndex = Mathf.Clamp(selectedPointIndex, 0, collider.EditableFootprint.Count - 1);
      EnforceMandatoryFootprintEdges(collider, selectedPointIndex);
      NormalizeSlicesToFootprint(collider, false);
      collider.Rebuild();
      EditorUtility.SetDirty(collider);
    }
    else if (selectionKind == SelectionKind.SlicePoint && IsValidSlicePoint(collider, selectedSliceIndex, selectedPointIndex))
    {
      if (collider.EditableSlices[selectedSliceIndex].EditablePoints.Count <= 3)
      {
        return;
      }

      Undo.RecordObject(collider, "Delete Slice Point From All Slices");
      foreach (ObliqueLoftSlice slice in collider.EditableSlices)
      {
        if (selectedPointIndex < slice.EditablePoints.Count)
        {
          slice.RemoveConnectionPoint(selectedPointIndex);
          slice.EditablePoints.RemoveAt(selectedPointIndex);
          slice.EnsurePointOrder();
        }
      }

      int remainingCount = collider.EditableSlices[selectedSliceIndex].EditablePoints.Count;
      selectedPointIndex = Mathf.Clamp(selectedPointIndex, 0, remainingCount - 1);
      collider.Rebuild();
      EditorUtility.SetDirty(collider);
    }
  }

  private void AddMiddleSlice(ObliqueLoftCollider collider)
  {
    Undo.RecordObject(collider, "Add Oblique Loft Middle Slice");
    if (collider.EditableSlices.Count == 0)
    {
      collider.ResetToBox();
      EditorUtility.SetDirty(collider);
      return;
    }

    ObliqueLoftSlice source = collider.EditableSlices[Mathf.Clamp(selectedSliceIndex, 0, collider.EditableSlices.Count - 1)];
    float depth = CalculateNewSliceDepth(collider);
    ObliqueLoftSlice slice = new ObliqueLoftSlice("Middle", depth);
    slice.CopyPointsFrom(source);
    collider.EditableSlices.Add(slice);
    SortSlices(collider);
    selectedSliceIndex = collider.EditableSlices.IndexOf(slice);
    ClearSelection();
    NormalizeSlicesToFootprint(collider, false);
    collider.Rebuild();
    EditorUtility.SetDirty(collider);
  }

  private void RemoveSelectedSlice(ObliqueLoftCollider collider)
  {
    if (!CanRemoveSelectedMiddleSlice(collider))
    {
      return;
    }

    Undo.RecordObject(collider, "Remove Oblique Loft Slice");
    selectedSliceIndex = Mathf.Clamp(selectedSliceIndex, 0, collider.EditableSlices.Count - 1);
    collider.EditableSlices.RemoveAt(selectedSliceIndex);
    selectedSliceIndex = Mathf.Clamp(selectedSliceIndex, 0, collider.EditableSlices.Count - 1);
    ClearSelection();
    NormalizeSlicesToFootprint(collider, false);
    collider.Rebuild();
    EditorUtility.SetDirty(collider);
  }

  private bool CanRemoveSelectedMiddleSlice(ObliqueLoftCollider collider)
  {
    return collider.EditableSlices.Count > 2 &&
      selectedSliceIndex > 0 &&
      selectedSliceIndex < collider.EditableSlices.Count - 1;
  }

  private float CalculateNewSliceDepth(ObliqueLoftCollider collider)
  {
    if (collider.EditableSlices.Count >= 2)
    {
      for (int i = 0; i < collider.EditableSlices.Count - 1; i++)
      {
        float a = collider.EditableSlices[i].Depth;
        float b = collider.EditableSlices[i + 1].Depth;
        if (b - a > MinimumSliceSpacing * 2f)
        {
          return (a + b) * 0.5f;
        }
      }

      return collider.EditableSlices[collider.EditableSlices.Count - 1].Depth + 0.25f;
    }

    return collider.EditableSlices[0].Depth + 1f;
  }

  private void SortSlices(ObliqueLoftCollider collider)
  {
    collider.EditableSlices.Sort((a, b) => a.Depth.CompareTo(b.Depth));
    for (int i = 0; i < collider.EditableSlices.Count; i++)
    {
      if (i == 0)
      {
        collider.EditableSlices[i].SetName("Front");
      }
      else if (i == collider.EditableSlices.Count - 1)
      {
        collider.EditableSlices[i].SetName("Back");
      }
      else
      {
        collider.EditableSlices[i].SetName("Middle " + i);
      }
    }
  }

  private void NormalizeSlicesToFootprint(ObliqueLoftCollider collider, bool recordUndo)
  {
    if (isNormalizingSlices || collider.EditableFootprint.Count < 3 || collider.EditableSlices.Count < 2)
    {
      return;
    }

    isNormalizingSlices = true;
    bool changed = false;
    if (recordUndo)
    {
      Undo.RecordObject(collider, "Normalize Oblique Loft Slices");
    }

    SortSlices(collider);
    changed |= EnsureMandatoryFootprintEdges(collider, selectionKind == SelectionKind.FootprintPoint ? selectedPointIndex : -1);
    GetFootprintDepthRange(collider, out float minDepth, out float maxDepth);
    changed |= SetSliceDepth(collider.EditableSlices[0], minDepth);
    changed |= SetSliceDepth(collider.EditableSlices[collider.EditableSlices.Count - 1], maxDepth);

    SortSlices(collider);
    changed |= FixCollapsedBoundarySlicePositions(collider);
    for (int i = 0; i < collider.EditableSlices.Count; i++)
    {
      ObliqueLoftSlice slice = collider.EditableSlices[i];
      changed |= EnsureNormalSliceShape(collider, slice);
      changed |= ClampAllSlicePoints(collider, slice);
    }

    if (changed)
    {
      collider.Rebuild();
      EditorUtility.SetDirty(collider);
    }

    isNormalizingSlices = false;
  }

  private bool SetSliceDepth(ObliqueLoftSlice slice, float depth)
  {
    if (Mathf.Approximately(slice.Depth, depth))
    {
      return false;
    }

    slice.SetDepth(depth);
    return true;
  }

  private void EnforceMandatoryFootprintEdges(ObliqueLoftCollider collider, int movedPointIndex)
  {
    EnsureMandatoryFootprintEdges(collider, movedPointIndex);
  }

  private bool EnsureMandatoryFootprintEdges(ObliqueLoftCollider collider, int preferredPointIndex)
  {
    if (collider.EditableFootprint.Count < 3)
    {
      return false;
    }

    bool changed = false;
    changed |= EnsureMandatoryFootprintEdge(collider, true, preferredPointIndex);
    changed |= EnsureMandatoryFootprintEdge(collider, false, preferredPointIndex);
    return changed;
  }

  private bool EnsureMandatoryFootprintEdge(ObliqueLoftCollider collider, bool frontEdge, int preferredPointIndex)
  {
    int count = collider.EditableFootprint.Count;
    bool useSelectedMandatoryPoint = IsValidFootprintPoint(collider, preferredPointIndex) &&
      ((frontEdge && IsValidFootprintPoint(collider, selectedFootprintFrontPartnerIndex)) ||
      (!frontEdge && IsValidFootprintPoint(collider, selectedFootprintBackPartnerIndex)));
    int extremeIndex = useSelectedMandatoryPoint ? preferredPointIndex : FindExtremeFootprintPointIndex(collider, frontEdge);
    int previousIndex = (extremeIndex - 1 + count) % count;
    int nextIndex = (extremeIndex + 1) % count;
    int partnerIndex = useSelectedMandatoryPoint
      ? (frontEdge ? selectedFootprintFrontPartnerIndex : selectedFootprintBackPartnerIndex)
      : ChooseMandatoryEdgePartner(collider, extremeIndex, previousIndex, nextIndex);

    Vector2 extreme = collider.EditableFootprint[extremeIndex];
    Vector2 partner = collider.EditableFootprint[partnerIndex];
    bool changed = false;
    if (!Mathf.Approximately(partner.y, extreme.y))
    {
      partner.y = extreme.y;
      changed = true;
    }

    if (Mathf.Abs(partner.x - extreme.x) < MinimumFlatEdgeLength)
    {
      float direction = partnerIndex == previousIndex ? -1f : 1f;
      partner.x = extreme.x + direction * MinimumFlatEdgeLength;
      changed = true;
    }

    if (changed)
    {
      collider.EditableFootprint[partnerIndex] = partner;
    }

    return changed;
  }

  private int FindExtremeFootprintPointIndex(ObliqueLoftCollider collider, bool frontEdge)
  {
    int best = 0;
    for (int i = 1; i < collider.EditableFootprint.Count; i++)
    {
      if (frontEdge)
      {
        if (collider.EditableFootprint[i].y < collider.EditableFootprint[best].y)
        {
          best = i;
        }
      }
      else if (collider.EditableFootprint[i].y > collider.EditableFootprint[best].y)
      {
        best = i;
      }
    }

    return best;
  }

  private int ChooseMandatoryEdgePartner(ObliqueLoftCollider collider, int extremeIndex, int previousIndex, int nextIndex)
  {
    Vector2 extreme = collider.EditableFootprint[extremeIndex];
    Vector2 previous = collider.EditableFootprint[previousIndex];
    Vector2 next = collider.EditableFootprint[nextIndex];
    bool previousAligned = Mathf.Approximately(previous.y, extreme.y);
    bool nextAligned = Mathf.Approximately(next.y, extreme.y);
    if (previousAligned && !nextAligned)
    {
      return previousIndex;
    }

    if (nextAligned && !previousAligned)
    {
      return nextIndex;
    }

    return Mathf.Abs(previous.x - extreme.x) >= Mathf.Abs(next.x - extreme.x) ? previousIndex : nextIndex;
  }

  private bool IsBoundarySlice(ObliqueLoftCollider collider, int sliceIndex)
  {
    return sliceIndex <= 0 || sliceIndex >= collider.EditableSlices.Count - 1;
  }

  private bool EnsureNormalSliceShape(ObliqueLoftCollider collider, ObliqueLoftSlice slice)
  {
    if (slice.EditablePoints.Count >= 3)
    {
      return false;
    }

    float halfWidth = GetFootprintWidthAtDepth(collider, slice.Depth) * 0.5f;
    float height = GetFootprintDepthHeight(collider) * 0.5f;
    slice.EditablePoints.Clear();
    slice.EditablePoints.Add(new Vector2(-halfWidth, slice.Depth));
    slice.EditablePoints.Add(new Vector2(-halfWidth, slice.Depth + height));
    slice.EditablePoints.Add(new Vector2(halfWidth, slice.Depth + height));
    slice.EditablePoints.Add(new Vector2(halfWidth, slice.Depth));
    return true;
  }

  private bool FixCollapsedBoundarySlicePositions(ObliqueLoftCollider collider)
  {
    if (collider.EditableSlices.Count < 2)
    {
      return false;
    }

    ObliqueLoftSlice front = collider.EditableSlices[0];
    ObliqueLoftSlice back = collider.EditableSlices[collider.EditableSlices.Count - 1];
    if (front.EditablePoints.Count != back.EditablePoints.Count ||
      front.EditablePoints.Count < 3 ||
      !HaveSameEditablePoints(front, back))
    {
      return false;
    }

    MoveSliceShapeBottomToDepth(front);
    MoveSliceShapeBottomToDepth(back);
    return true;
  }

  private bool HaveSameEditablePoints(ObliqueLoftSlice a, ObliqueLoftSlice b)
  {
    for (int i = 0; i < a.EditablePoints.Count; i++)
    {
      if ((a.EditablePoints[i] - b.EditablePoints[i]).sqrMagnitude > 0.000001f)
      {
        return false;
      }
    }

    return true;
  }

  private void MoveSliceShapeBottomToDepth(ObliqueLoftSlice slice)
  {
    float bottom = slice.EditablePoints.Min(point => point.y);
    for (int i = 0; i < slice.EditablePoints.Count; i++)
    {
      Vector2 point = slice.EditablePoints[i];
      point.y = slice.Depth + point.y - bottom;
      slice.EditablePoints[i] = point;
    }
  }

  private bool ClampAllSlicePoints(ObliqueLoftCollider collider, ObliqueLoftSlice slice)
  {
    bool changed = false;
    for (int i = 0; i < slice.EditablePoints.Count; i++)
    {
      Vector2 clamped = ClampSlicePoint(collider, slice, slice.EditablePoints[i]);
      if (clamped != slice.EditablePoints[i])
      {
        slice.EditablePoints[i] = clamped;
        changed = true;
      }
    }

    return changed;
  }

  private Vector2 ClampSlicePoint(ObliqueLoftCollider collider, ObliqueLoftSlice slice, Vector2 point)
  {
    point.y = Mathf.Max(slice.Depth, point.y);
    return point;
  }

  private Vector2 SnapPointToNearbyPoints(Vector2 point, System.Collections.Generic.IReadOnlyList<Vector2> points, int ignoreIndex)
  {
    bool snappedX = false;
    bool snappedY = false;
    float bestXDistance = SnapProximity;
    float bestYDistance = SnapProximity;
    for (int i = 0; i < points.Count; i++)
    {
      if (i == ignoreIndex)
      {
        continue;
      }

      Vector2 other = points[i];
      float xDistance = Mathf.Abs(point.x - other.x);
      if (xDistance <= bestXDistance)
      {
        point.x = other.x;
        bestXDistance = xDistance;
        snappedX = true;
      }

      float yDistance = Mathf.Abs(point.y - other.y);
      if (yDistance <= bestYDistance)
      {
        point.y = other.y;
        bestYDistance = yDistance;
        snappedY = true;
      }

      if (!snappedX || !snappedY)
      {
        float pointDistance = (point - other).magnitude;
        if (pointDistance <= SnapProximity)
        {
          point = other;
          snappedX = true;
          snappedY = true;
        }
      }
    }

    return point;
  }

  private void CopyBestNormalShape(ObliqueLoftCollider collider, ObliqueLoftSlice destination)
  {
    ObliqueLoftSlice source = collider.EditableSlices.FirstOrDefault(slice => slice.EditablePoints.Count >= 3);
    if (source != null)
    {
      destination.CopyPointsFrom(source);
      return;
    }

    EnsureNormalSliceShape(collider, destination);
  }

  private void GetFootprintDepthRange(ObliqueLoftCollider collider, out float minDepth, out float maxDepth)
  {
    minDepth = collider.EditableFootprint.Min(point => point.y);
    maxDepth = collider.EditableFootprint.Max(point => point.y);
  }

  private float GetFootprintWidthAtDepth(ObliqueLoftCollider collider, float depth)
  {
    GetFootprintDepthEndpoints(collider, depth, out Vector3 left, out Vector3 right);
    return Mathf.Max(0.1f, Mathf.Abs(right.x - left.x));
  }

  private float GetFootprintDepthHeight(ObliqueLoftCollider collider)
  {
    GetFootprintDepthRange(collider, out float minDepth, out float maxDepth);
    return Mathf.Max(0.1f, maxDepth - minDepth);
  }

  private float SnapDepthToFootprint(float requestedDepth, ObliqueLoftCollider collider)
  {
    float bestDepth = requestedDepth;
    float bestDistance = float.PositiveInfinity;

    for (int i = 0; i < collider.EditableFootprint.Count; i++)
    {
      Vector2 a = collider.EditableFootprint[i];
      Vector2 b = collider.EditableFootprint[(i + 1) % collider.EditableFootprint.Count];
      if (Mathf.Approximately(a.y, b.y))
      {
        continue;
      }

      float minY = Mathf.Min(a.y, b.y);
      float maxY = Mathf.Max(a.y, b.y);
      float candidate = Mathf.Clamp(requestedDepth, minY, maxY);
      float distance = Mathf.Abs(candidate - requestedDepth);
      if (distance < bestDistance)
      {
        bestDistance = distance;
        bestDepth = candidate;
      }
    }

    GetFootprintDepthRange(collider, out float minDepth, out float maxDepth);
    return Mathf.Clamp(bestDepth, minDepth, maxDepth);
  }

  private float ClampMiddleSliceDepth(ObliqueLoftCollider collider, int sliceIndex, float depth)
  {
    if (IsBoundarySlice(collider, sliceIndex))
    {
      return collider.EditableSlices[sliceIndex].Depth;
    }

    float min = collider.EditableSlices[sliceIndex - 1].Depth + MinimumSliceSpacing;
    float max = collider.EditableSlices[sliceIndex + 1].Depth - MinimumSliceSpacing;
    return Mathf.Clamp(depth, min, max);
  }

  private void DrawFaceLabels(ObliqueLoftCollider collider)
  {
    foreach (ObliqueLoftFace face in collider.GeneratedFaces)
    {
      Vector3 center = Vector3.zero;
      foreach (Vector3 vertex in face.Vertices)
      {
        center += collider.LocalToLogicWorld(vertex);
      }

      center /= face.Vertices.Count;
      Handles.Label(collider.LogicWorldToScene(center), face.SurfaceType + " #" + face.FaceIndex);
    }
  }

  private void DrawGeneratedFaceShading(ObliqueLoftCollider collider)
  {
    foreach (ObliqueLoftFace face in collider.GeneratedFaces)
    {
      if (face.Vertices.Count < 3)
      {
        continue;
      }

      Vector3[] scenePoints = new Vector3[face.Vertices.Count];
      for (int i = 0; i < face.Vertices.Count; i++)
      {
        scenePoints[i] = collider.LogicWorldToScene(collider.LocalToLogicWorld(face.Vertices[i]));
      }

      Handles.color = GetNormalMapFaceColor(face.Normal);
      for (int i = 1; i < scenePoints.Length - 1; i++)
      {
        Handles.DrawAAConvexPolygon(scenePoints[0], scenePoints[i], scenePoints[i + 1]);
      }
    }
  }

  private Color GetNormalMapFaceColor(Vector3 normal)
  {
    Vector3 normalized = normal == Vector3.zero ? Vector3.zero : normal.normalized;
    return new Color(
      normalized.x * 0.5f + 0.5f,
      normalized.y * 0.5f + 0.5f,
      normalized.z * 0.5f + 0.5f,
      0.34f);
  }

  private Vector3 FootprintPointToScene(ObliqueLoftCollider collider, Vector2 point)
  {
    return collider.LocalGroundToScene(point);
  }

  private Vector2 SceneToFootprintPoint(ObliqueLoftCollider collider, Vector3 scenePoint)
  {
    return collider.SceneToLocalGround(scenePoint);
  }

  private void GetFootprintDepthEndpoints(ObliqueLoftCollider collider, float depth, out Vector3 left, out Vector3 right)
  {
    float fallbackMinX = collider.EditableFootprint.Min(point => point.x);
    float fallbackMaxX = collider.EditableFootprint.Max(point => point.x);
    float leftX = fallbackMinX;
    float rightX = fallbackMaxX;
    bool hasIntersection = false;

    for (int i = 0; i < collider.EditableFootprint.Count; i++)
    {
      Vector2 a = collider.EditableFootprint[i];
      Vector2 b = collider.EditableFootprint[(i + 1) % collider.EditableFootprint.Count];

      if (TryGetHorizontalFootprintSegment(a, b, depth, out float segmentMinX, out float segmentMaxX))
      {
        if (!hasIntersection)
        {
          leftX = segmentMinX;
          rightX = segmentMaxX;
          hasIntersection = true;
        }
        else
        {
          leftX = Mathf.Min(leftX, segmentMinX);
          rightX = Mathf.Max(rightX, segmentMaxX);
        }

        continue;
      }

      if (!TryGetHorizontalFootprintIntersection(a, b, depth, out float x))
      {
        continue;
      }

      if (!hasIntersection)
      {
        leftX = x;
        rightX = x;
        hasIntersection = true;
      }
      else
      {
        leftX = Mathf.Min(leftX, x);
        rightX = Mathf.Max(rightX, x);
      }
    }

    left = FootprintPointToScene(collider, new Vector2(leftX, depth));
    right = FootprintPointToScene(collider, new Vector2(rightX, depth));
  }

  private bool TryGetHorizontalFootprintSegment(Vector2 a, Vector2 b, float depth, out float minX, out float maxX)
  {
    minX = 0f;
    maxX = 0f;
    if (!Mathf.Approximately(a.y, b.y) || !Mathf.Approximately(depth, a.y))
    {
      return false;
    }

    minX = Mathf.Min(a.x, b.x);
    maxX = Mathf.Max(a.x, b.x);
    return true;
  }

  private bool TryGetHorizontalFootprintIntersection(Vector2 a, Vector2 b, float depth, out float x)
  {
    x = 0f;

    if (Mathf.Approximately(a.y, b.y))
    {
      return false;
    }

    float minY = Mathf.Min(a.y, b.y);
    float maxY = Mathf.Max(a.y, b.y);
    if (depth < minY || depth > maxY)
    {
      return false;
    }

    float t = (depth - a.y) / (b.y - a.y);
    x = Mathf.Lerp(a.x, b.x, t);
    return true;
  }

  private Vector3 SlicePointToScene(ObliqueLoftCollider collider, ObliqueLoftSlice slice, int pointIndex)
  {
    return collider.LogicWorldToScene(collider.LocalToLogicWorld(slice.GetLocalVertex(pointIndex)));
  }

  private Vector2 SceneToSlicePoint(ObliqueLoftCollider collider, Vector3 scenePoint)
  {
    if (collider.EditableSlices.Count == 0)
    {
      return Vector2.zero;
    }

    ObliqueLoftSlice slice = collider.EditableSlices[Mathf.Clamp(selectedSliceIndex, 0, collider.EditableSlices.Count - 1)];
    float localX = SceneToSliceLocalX(collider, slice, scenePoint);
    Vector3 groundAtSliceDepth = FootprintPointToScene(collider, new Vector2(localX, slice.Depth));
    float localHeight = (scenePoint.y - groundAtSliceDepth.y) / Mathf.Max(collider.GetLogicHeightScale(), 0.0001f);
    float localY = slice.Depth + localHeight;
    return ClampSlicePoint(collider, slice, new Vector2(localX, localY));
  }

  private float SceneToSliceLocalX(ObliqueLoftCollider collider, ObliqueLoftSlice slice, Vector3 scenePoint)
  {
    Vector3 groundAtZeroX = FootprintPointToScene(collider, new Vector2(0f, slice.Depth));
    Vector3 localRight = collider.transform.TransformVector(Vector3.right);
    if (Mathf.Abs(localRight.x) > 0.0001f)
    {
      return (scenePoint.x - groundAtZeroX.x) / localRight.x;
    }

    return collider.SceneToLocalGround(scenePoint).x;
  }

  private float DivideByScale(float value, float scale)
  {
    return Mathf.Abs(scale) > Mathf.Epsilon ? value / scale : value;
  }

  private Vector2 ClampSlicePointHeight(Vector2 point)
  {
    point.y = Mathf.Max(0f, point.y);
    return point;
  }

  private Color GetSliceColor(ObliqueLoftCollider collider, int sliceIndex)
  {
    if (sliceIndex == 0)
    {
      return FrontSliceColor;
    }

    return sliceIndex == collider.EditableSlices.Count - 1 ? BackSliceColor : MiddleSliceColor;
  }

  private void DrawPointDisc(Vector3 position, Color color, float sizeScale)
  {
    Handles.color = color;
    Handles.DotHandleCap(0, position, Quaternion.identity, HandleUtility.GetHandleSize(position) * sizeScale, EventType.Repaint);
  }

  private void SelectFootprintPoint(ObliqueLoftCollider collider, int pointIndex)
  {
    selectionKind = SelectionKind.FootprintPoint;
    selectedPointIndex = pointIndex;
    CaptureMandatoryFootprintPartners(collider, pointIndex);
  }

  private void SelectSlicePoint(int sliceIndex, int pointIndex)
  {
    selectionKind = SelectionKind.SlicePoint;
    selectedSliceIndex = sliceIndex;
    selectedPointIndex = pointIndex;
    selectedFootprintFrontPartnerIndex = -1;
    selectedFootprintBackPartnerIndex = -1;
  }

  private void ClearSelection()
  {
    selectionKind = SelectionKind.None;
    selectedPointIndex = -1;
    selectedFootprintFrontPartnerIndex = -1;
    selectedFootprintBackPartnerIndex = -1;
  }

  private void CaptureMandatoryFootprintPartners(ObliqueLoftCollider collider, int pointIndex)
  {
    selectedFootprintFrontPartnerIndex = -1;
    selectedFootprintBackPartnerIndex = -1;
    if (!IsValidFootprintPoint(collider, pointIndex) || collider.EditableFootprint.Count < 3)
    {
      return;
    }

    GetFootprintDepthRange(collider, out float minDepth, out float maxDepth);
    int previousIndex = (pointIndex - 1 + collider.EditableFootprint.Count) % collider.EditableFootprint.Count;
    int nextIndex = (pointIndex + 1) % collider.EditableFootprint.Count;
    if (TryGetMandatoryEdgePartner(collider, pointIndex, previousIndex, nextIndex, minDepth, out int frontPartner))
    {
      selectedFootprintFrontPartnerIndex = frontPartner;
    }

    if (TryGetMandatoryEdgePartner(collider, pointIndex, previousIndex, nextIndex, maxDepth, out int backPartner))
    {
      selectedFootprintBackPartnerIndex = backPartner;
    }
  }

  private bool TryGetMandatoryEdgePartner(ObliqueLoftCollider collider, int pointIndex, int previousIndex, int nextIndex, float depth, out int partnerIndex)
  {
    partnerIndex = -1;
    Vector2 point = collider.EditableFootprint[pointIndex];
    if (!Mathf.Approximately(point.y, depth))
    {
      return false;
    }

    bool previousMatches = Mathf.Approximately(collider.EditableFootprint[previousIndex].y, depth);
    bool nextMatches = Mathf.Approximately(collider.EditableFootprint[nextIndex].y, depth);
    if (!previousMatches && !nextMatches)
    {
      return false;
    }

    if (previousMatches && nextMatches)
    {
      partnerIndex = Mathf.Abs(collider.EditableFootprint[previousIndex].x - point.x) >= Mathf.Abs(collider.EditableFootprint[nextIndex].x - point.x)
        ? previousIndex
        : nextIndex;
      return true;
    }

    partnerIndex = previousMatches ? previousIndex : nextIndex;
    return true;
  }

  private bool IsValidFootprintPoint(ObliqueLoftCollider collider, int pointIndex)
  {
    return pointIndex >= 0 && pointIndex < collider.EditableFootprint.Count;
  }

  private bool IsValidSlicePoint(ObliqueLoftCollider collider, int sliceIndex, int pointIndex)
  {
    return sliceIndex >= 0 &&
      sliceIndex < collider.EditableSlices.Count &&
      pointIndex >= 0 &&
      pointIndex < collider.EditableSlices[sliceIndex].EditablePoints.Count;
  }
}
