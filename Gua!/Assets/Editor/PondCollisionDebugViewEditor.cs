using System.Linq;
using FrogCamp.Game;
using FrogCamp.Networking;
using UnityEditor;
using UnityEngine;

namespace FrogCamp.Editor
{
    [CustomEditor(typeof(PondCollisionDebugView))]
    public sealed class PondCollisionDebugViewEditor : UnityEditor.Editor
    {
        private enum EditTarget
        {
            None,
            Boundary,
            Obstacle
        }

        private EditTarget editTarget;
        private bool appendMode;
        private int selectedRegion;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            PondCollisionDebugView view =
                (PondCollisionDebugView)target;
            PondCollisionConfig config = view.CollisionConfig;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("红色石头边界",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "选择“编辑红线节点”后可直接拖动节点；" +
                "清空重画时按住 Shift 并依次左键添加节点。",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(config == null))
            {
                if (GUILayout.Button("编辑红线节点"))
                {
                    editTarget = EditTarget.Boundary;
                    appendMode = false;
                    SceneView.RepaintAll();
                }
                if (GUILayout.Button("清空并重新绘制红线"))
                {
                    Undo.RecordObject(config, "Redraw Pond Boundary");
                    config.ReplaceBoundary(null);
                    Save(config);
                    editTarget = EditTarget.Boundary;
                    appendMode = true;
                    SceneView.RepaintAll();
                }
                if (editTarget == EditTarget.Boundary &&
                    GUILayout.Button("删除红线最后一个节点"))
                {
                    Undo.RecordObject(config,
                        "Remove Pond Boundary Point");
                    config.RemoveLastBoundaryPoint();
                    Save(config);
                    SceneView.RepaintAll();
                }
                if (GUILayout.Button("恢复默认石头边界"))
                {
                    Undo.RecordObject(config, "Restore Pond Boundary");
                    config.ReplaceBoundary(
                        PondObstacleMap.DefaultBoundary);
                    Save(config);
                    editTarget = EditTarget.None;
                    appendMode = false;
                    SceneView.RepaintAll();
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("黄色物件碰撞区域",
                    EditorStyles.boldLabel);
                DrawObstacleControls(config);

                EditorGUILayout.Space();
                if (editTarget != EditTarget.None &&
                    GUILayout.Button("结束编辑并保存"))
                {
                    editTarget = EditTarget.None;
                    appendMode = false;
                    Save(config);
                    SceneView.RepaintAll();
                }
            }
        }

        private void DrawObstacleControls(PondCollisionConfig config)
        {
            if (config == null || config.ObstacleRegions.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "尚未生成黄色碰撞区域，请重新运行碰撞安装器。",
                    MessageType.Warning);
                return;
            }

            selectedRegion = Mathf.Clamp(selectedRegion, 0,
                config.ObstacleRegions.Count - 1);
            string[] names = config.ObstacleRegions
                .Select((region, index) =>
                    (index + 1) + ". " + region.DisplayName)
                .ToArray();
            selectedRegion = EditorGUILayout.Popup(
                "选择物件", selectedRegion, names);
            PondCollisionRegion region =
                config.ObstacleRegions[selectedRegion];
            EditorGUILayout.LabelField(
                "节点数量", region.Points.Count.ToString());

            if (GUILayout.Button("编辑所选黄色区域节点"))
            {
                editTarget = EditTarget.Obstacle;
                appendMode = false;
                SceneView.RepaintAll();
            }
            if (GUILayout.Button("清空并重新绘制所选黄色区域"))
            {
                Undo.RecordObject(config,
                    "Redraw Pond Obstacle Region");
                region.ReplacePoints(null);
                Save(config);
                editTarget = EditTarget.Obstacle;
                appendMode = true;
                SceneView.RepaintAll();
            }
            if (editTarget == EditTarget.Obstacle &&
                GUILayout.Button("删除所选区域最后一个节点"))
            {
                Undo.RecordObject(config,
                    "Remove Pond Obstacle Point");
                region.RemoveLastPoint();
                Save(config);
                SceneView.RepaintAll();
            }
            if (GUILayout.Button("恢复全部默认黄色区域"))
            {
                Undo.RecordObject(config,
                    "Restore Pond Obstacle Regions");
                config.ReplaceObstacleRegions(
                    PondObstacleMap.Definitions);
                Save(config);
                editTarget = EditTarget.None;
                appendMode = false;
                SceneView.RepaintAll();
            }
        }

        private void OnSceneGUI()
        {
            PondCollisionDebugView view =
                (PondCollisionDebugView)target;
            PondCollisionConfig config = view.CollisionConfig;
            RectTransform rect = view.transform as RectTransform;
            if (config == null || rect == null) return;

            DrawBoundary(rect, config);
            DrawObstacleRegions(rect, config);

            if (editTarget == EditTarget.Boundary)
                DrawBoundaryHandles(rect, config);
            else if (editTarget == EditTarget.Obstacle &&
                     config.ObstacleRegions.Count > 0)
                DrawRegionHandles(rect, config,
                    config.ObstacleRegions[Mathf.Clamp(
                        selectedRegion, 0,
                        config.ObstacleRegions.Count - 1)]);

            if (appendMode)
            {
                CaptureSceneClick(rect, config);
                DrawInstructions();
            }
        }

        private static void DrawBoundary(RectTransform rect,
            PondCollisionConfig config)
        {
            DrawClosedPolyline(rect, config.RockBoundary,
                new Color(1f, 0.16f, 0.12f, 1f), 5f);
        }

        private void DrawObstacleRegions(RectTransform rect,
            PondCollisionConfig config)
        {
            for (int index = 0;
                 index < config.ObstacleRegions.Count; index++)
            {
                bool selected = editTarget == EditTarget.Obstacle &&
                                index == selectedRegion;
                DrawClosedPolyline(rect,
                    config.ObstacleRegions[index].Points,
                    selected
                        ? new Color(1f, 0.63f, 0.05f, 1f)
                        : new Color(1f, 0.82f, 0.15f, 0.72f),
                    selected ? 5f : 3f);
            }
        }

        private static void DrawClosedPolyline(RectTransform rect,
            System.Collections.Generic.IReadOnlyList<Vector2> points,
            Color color, float width)
        {
            int count = points == null ? 0 : points.Count;
            if (count < 2) return;
            Vector3[] worldPoints =
                new Vector3[count + (count >= 3 ? 1 : 0)];
            for (int index = 0; index < count; index++)
                worldPoints[index] = rect.TransformPoint(
                    PondCollisionDebugView.LogicalToLocal(
                        points[index]));
            if (count >= 3) worldPoints[count] = worldPoints[0];
            Handles.color = color;
            Handles.DrawAAPolyLine(width, worldPoints);
        }

        private static void DrawBoundaryHandles(RectTransform rect,
            PondCollisionConfig config)
        {
            for (int index = 0;
                 index < config.RockBoundary.Count; index++)
            {
                Vector3 moved = DrawHandle(rect,
                    config.RockBoundary[index],
                    new Color(1f, 0.3f, 0.18f, 1f),
                    out bool changed);
                if (!changed) continue;
                Undo.RecordObject(config,
                    "Move Pond Boundary Point");
                config.SetBoundaryPoint(index,
                    PondCollisionDebugView.LocalToLogical(
                        rect.InverseTransformPoint(moved)));
                Save(config);
            }
        }

        private static void DrawRegionHandles(RectTransform rect,
            PondCollisionConfig config, PondCollisionRegion region)
        {
            for (int index = 0; index < region.Points.Count; index++)
            {
                Vector3 moved = DrawHandle(rect, region.Points[index],
                    new Color(1f, 0.78f, 0.08f, 1f),
                    out bool changed);
                if (!changed) continue;
                Undo.RecordObject(config,
                    "Move Pond Obstacle Point");
                region.SetPoint(index,
                    PondCollisionDebugView.LocalToLogical(
                        rect.InverseTransformPoint(moved)));
                Save(config);
            }
        }

        private static Vector3 DrawHandle(RectTransform rect,
            Vector2 logicalPoint, Color color, out bool changed)
        {
            Vector3 world = rect.TransformPoint(
                PondCollisionDebugView.LogicalToLocal(logicalPoint));
            float size = HandleUtility.GetHandleSize(world) * 0.055f;
            EditorGUI.BeginChangeCheck();
            Handles.color = color;
            Vector3 moved = Handles.FreeMoveHandle(
                world, Quaternion.identity, size,
                Vector3.zero, Handles.DotHandleCap);
            changed = EditorGUI.EndChangeCheck();
            return moved;
        }

        private void CaptureSceneClick(RectTransform rect,
            PondCollisionConfig config)
        {
            Event current = Event.current;
            if (current.type == EventType.Layout)
                HandleUtility.AddDefaultControl(
                    GUIUtility.GetControlID(FocusType.Passive));
            if (current.type != EventType.MouseDown ||
                current.button != 0 || !current.shift)
                return;

            Ray ray = HandleUtility.GUIPointToWorldRay(
                current.mousePosition);
            Plane plane = new Plane(rect.forward, rect.position);
            if (!plane.Raycast(ray, out float distance)) return;
            Vector2 logical = PondCollisionDebugView.LocalToLogical(
                rect.InverseTransformPoint(ray.GetPoint(distance)));

            Undo.RecordObject(config, "Add Pond Collision Point");
            if (editTarget == EditTarget.Boundary)
                config.AddBoundaryPoint(logical);
            else if (editTarget == EditTarget.Obstacle &&
                     config.ObstacleRegions.Count > 0)
                config.ObstacleRegions[Mathf.Clamp(
                    selectedRegion, 0,
                    config.ObstacleRegions.Count - 1)]
                    .AddPoint(logical);
            Save(config);
            current.Use();
            SceneView.RepaintAll();
        }

        private void DrawInstructions()
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(18f, 18f, 360f, 58f),
                EditorStyles.helpBox);
            GUILayout.Label(
                editTarget == EditTarget.Boundary
                    ? "正在重绘红色石头边界：Shift + 左键添加节点"
                    : "正在重绘所选黄色区域：Shift + 左键添加节点");
            GUILayout.Label("系统自动闭合首尾；结束后在 Inspector 保存。");
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private static void Save(PondCollisionConfig config)
        {
            if (config == null) return;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }
    }
}
