using System;
using System.Collections.Generic;
using System.Linq;
using RoutePlanning.Profiles;
using RoutePlanning.RoutePlanning;
using Runtime.Player.Movement;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class RouteAuthoringWindow : EditorWindow
{
    private const float LibraryPanelWidth = 240f;
    private const float TreeViewMinHeight = 140f;

    private readonly Dictionary<MoveProfile, SerializedObject> _profileSerializationCache =
        new Dictionary<MoveProfile, SerializedObject>();

    private readonly List<RouteGraph> _library = new List<RouteGraph>();

    private RouteGraph _activeGraph;
    private SerializedObject _serializedGraph;

    private TreeViewState _treeState;
    private RouteTreeView _treeView;

    private Vector2 _libraryScroll;
    private Vector2 _inspectorScroll;
    private string _librarySearch = string.Empty;

    private string _selectedPropertyPath;
    private string _selectedDisplayName;

    private GraphValidationReport _validationReport;
    private double _lastValidationSample;

    [MenuItem("Tools/Route Authoring")]
    private static void Open()
    {
        var window = GetWindow<RouteAuthoringWindow>();
        window.titleContent = new GUIContent("Route Authoring");
        window.Show();
    }

    private void OnEnable()
    {
        RefreshLibrary();
        SceneView.duringSceneGui += OnSceneGUI;
        Undo.undoRedoPerformed += OnUndoRedoPerformed;

        _treeState ??= new TreeViewState();
        _treeView = new RouteTreeView(_treeState);
        _treeView.SelectionChangedEvent += OnTreeSelectionChanged;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;

        if (_treeView != null)
        {
            _treeView.SelectionChangedEvent -= OnTreeSelectionChanged;
        }

        _profileSerializationCache.Clear();
    }

    private void OnSelectionChange()
    {
        if (Selection.activeObject is RouteGraph graph)
        {
            SetActiveGraph(graph);
            Repaint();
        }
    }

    private void OnUndoRedoPerformed()
    {
        if (_activeGraph == null)
        {
            return;
        }

        _serializedGraph = new SerializedObject(_activeGraph);
        UpdateValidation(force: true);
        Repaint();
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawLibraryPanel();
            DrawRouteEditor();
        }

        DrawValidationStatus();

        if (Event.current.type == EventType.Layout)
        {
            UpdateValidation();
        }
    }

    private void DrawLibraryPanel()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(LibraryPanelWidth)))
        {
            EditorGUILayout.LabelField("Library", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Search", GUILayout.Width(50f));
                _librarySearch = GUILayout.TextField(_librarySearch, EditorStyles.toolbarTextField);
                if (GUILayout.Button(string.Empty, EditorStyles.toolbarSearchCancelButton))
                {
                    _librarySearch = string.Empty;
                    GUI.FocusControl(null);
                }
            }

            using (var scroll = new EditorGUILayout.ScrollViewScope(_libraryScroll))
            {
                _libraryScroll = scroll.scrollPosition;

                foreach (var graph in _library)
                {
                    if (graph == null)
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(_librarySearch) &&
                        graph.name.IndexOf(_librarySearch, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        if (GUILayout.Button(graph.name, GUILayout.ExpandWidth(true)))
                        {
                            SetActiveGraph(graph);
                        }

                        if (GUILayout.Button("Ping", GUILayout.Width(46f)))
                        {
                            EditorGUIUtility.PingObject(graph);
                        }
                    }
                }
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refresh", GUILayout.Height(24f)))
            {
                RefreshLibrary();
            }

            if (GUILayout.Button("Create Route Graph", GUILayout.Height(26f)))
            {
                CreateRouteGraphAsset();
            }
        }
    }

    private void DrawRouteEditor()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            if (_activeGraph == null)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.HelpBox("Select or create a Route Graph to begin editing.", MessageType.Info);
                GUILayout.FlexibleSpace();
                return;
            }

            _serializedGraph ??= new SerializedObject(_activeGraph);
            _serializedGraph.UpdateIfRequiredOrScript();

            EditorGUILayout.LabelField(_activeGraph.name, EditorStyles.largeLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Ping Asset", GUILayout.Width(100f)))
                {
                    EditorGUIUtility.PingObject(_activeGraph);
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Validate", GUILayout.Width(100f)))
                {
                    UpdateValidation(force: true);
                }
            }

            EditorGUILayout.Space();

            Rect treeRect = GUILayoutUtility.GetRect(100f, 9999f, TreeViewMinHeight, 400f);
            _treeView?.OnGUI(treeRect);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Inspector", EditorStyles.boldLabel);
            using (var scroll = new EditorGUILayout.ScrollViewScope(_inspectorScroll))
            {
                _inspectorScroll = scroll.scrollPosition;

                EditorGUI.BeginChangeCheck();
                DrawSelectionInspector();
                bool changed = EditorGUI.EndChangeCheck();
                _serializedGraph.ApplyModifiedProperties();
                if (changed)
                {
                    EditorUtility.SetDirty(_activeGraph);
                    UpdateValidation(force: true);
                }
            }
        }
    }

    private void DrawSelectionInspector()
    {
        if (string.IsNullOrEmpty(_selectedPropertyPath))
        {
            EditorGUILayout.HelpBox("Select a node or branch in the tree view to edit its parameters.",
                MessageType.Info);
            return;
        }

        if (_serializedGraph == null)
        {
            return;
        }

        var property = _serializedGraph.FindProperty(_selectedPropertyPath);
        if (property == null)
        {
            EditorGUILayout.HelpBox("The selected property could not be found. It may have been removed.",
                MessageType.Warning);
            return;
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.PropertyField(property, new GUIContent(_selectedDisplayName), true);
        }
    }

    private void DrawValidationStatus()
    {
        if (_activeGraph == null)
        {
            return;
        }

        if (_validationReport == null)
        {
            EditorGUILayout.HelpBox("Validation pending…", MessageType.Info);
            return;
        }

        var messageType = _validationReport.HasErrors ? MessageType.Error : MessageType.Info;
        EditorGUILayout.HelpBox(_validationReport.Summary, messageType);

        if (_validationReport.HasErrors)
        {
            foreach (var error in _validationReport.Errors)
            {
                EditorGUILayout.LabelField("• " + error, EditorStyles.wordWrappedLabel);
            }
        }
    }

    private void RefreshLibrary()
    {
        _library.Clear();
        string[] guids = AssetDatabase.FindAssets("t:RouteGraph");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<RouteGraph>(path);
            if (asset != null && !_library.Contains(asset))
            {
                _library.Add(asset);
            }
        }

        _library.Sort((a, b) => string.CompareOrdinal(a?.name, b?.name));
    }

    private void CreateRouteGraphAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject("Create Route Graph", "RouteGraph", "asset",
            "Choose a location for the new Route Graph asset.");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var asset = CreateInstance<RouteGraph>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        RefreshLibrary();
        SetActiveGraph(asset);
    }

    private void SetActiveGraph(RouteGraph graph)
    {
        if (_activeGraph == graph)
        {
            return;
        }

        _activeGraph = graph;
        _serializedGraph = graph != null ? new SerializedObject(graph) : null;
        _selectedPropertyPath = null;
        _selectedDisplayName = null;
        _profileSerializationCache.Clear();

        UpdateValidation(force: true);
    }

    private void OnTreeSelectionChanged(RouteTreeView.SelectionData selection)
    {
        if (selection == null)
        {
            _selectedPropertyPath = null;
            _selectedDisplayName = null;
        }
        else
        {
            _selectedPropertyPath = selection.PropertyPath;
            _selectedDisplayName = selection.DisplayName;
        }

        Repaint();
    }

    private void UpdateValidation(bool force = false)
    {
        if (_activeGraph == null)
        {
            _validationReport = null;
            _treeView?.SetGraph(null, null);
            return;
        }

        if (!force && EditorApplication.timeSinceStartup - _lastValidationSample < 0.25)
        {
            return;
        }

        _lastValidationSample = EditorApplication.timeSinceStartup;
        _validationReport = GraphValidator.Build(_activeGraph);
        _treeView?.SetGraph(_serializedGraph, _validationReport);
        _treeView?.SetSelectionByPath(_selectedPropertyPath);
        SceneView.RepaintAll();
        Repaint();
    }

    private void OnSceneGUI(SceneView view)
    {
        if (_activeGraph == null || _serializedGraph == null || _validationReport == null)
        {
            return;
        }

        _serializedGraph.UpdateIfRequiredOrScript();
        var nodesProperty = _serializedGraph.FindProperty("_nodes");
        if (nodesProperty == null || !nodesProperty.isArray)
        {
            return;
        }

        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

        foreach (var nodeResult in _validationReport.Nodes)
        {
            if (nodeResult.Index < 0 || nodeResult.Index >= nodesProperty.arraySize)
            {
                continue;
            }

            var nodeProperty = nodesProperty.GetArrayElementAtIndex(nodeResult.Index);
            var positionProperty = nodeProperty.FindPropertyRelative("_worldPosition");
            Vector3 position = positionProperty != null ? positionProperty.vector3Value : Vector3.zero;

            Color color = ResolveNodeColor(nodeProperty, _activeGraph.DefaultColor, nodeResult.ProfileColor);
            bool isSelected = TryParseNodeIndex(_selectedPropertyPath, out int selectedIndex) &&
                              selectedIndex == nodeResult.Index;

            DrawNodeHandles(nodeResult, nodeProperty, position, color, isSelected);
        }

        foreach (var branchResult in _validationReport.Branches)
        {
            DrawBranchVisualization(branchResult);
        }
    }

    private void DrawNodeHandles(NodeValidationResult nodeResult, SerializedProperty nodeProperty, Vector3 position,
        Color color, bool isSelected)
    {
        float size = HandleUtility.GetHandleSize(position) * (isSelected ? 0.22f : 0.18f);
        Handles.color = nodeResult.HasError ? Color.red : color;
        Handles.SphereHandleCap(0, position, Quaternion.identity, size, Event.current.type);
        Handles.Label(position + Vector3.up * size * 2f, nodeResult.DisplayName,
            nodeResult.HasError ? EditorStyles.boldLabel : EditorStyles.label);

        EditorGUI.BeginChangeCheck();
        Vector3 newPosition = Handles.PositionHandle(position, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            nodeProperty.FindPropertyRelative("_worldPosition").vector3Value = newPosition;
            _serializedGraph.ApplyModifiedProperties();
            EditorUtility.SetDirty(_activeGraph);
            UpdateValidation(force: true);
        }

        if (nodeResult.Evaluation.HasValue)
        {
            DrawTrajectory(nodeResult, color);
            DrawApexHandle(nodeResult, nodeProperty, color);
            DrawDurationHandles(nodeResult);
        }
        else if (!string.IsNullOrEmpty(nodeResult.Error))
        {
            Handles.color = Color.red;
            Handles.Label(position + Vector3.up * size * 3f, nodeResult.Error, EditorStyles.wordWrappedLabel);
        }
    }

    private void DrawTrajectory(NodeValidationResult nodeResult, Color color)
    {
        var evaluation = nodeResult.Evaluation.Value;
        var trajectory = evaluation.Trajectory;
        if (trajectory == null || trajectory.Count < 2)
        {
            return;
        }

        Handles.color = nodeResult.HasError ? Color.red : color;
        Handles.DrawAAPolyLine(4f, trajectory.ToArray());

        if (evaluation.CollisionIndex.HasValue &&
            evaluation.CollisionIndex.Value >= 0 && evaluation.CollisionIndex.Value < trajectory.Count)
        {
            Vector3 collisionPoint = trajectory[evaluation.CollisionIndex.Value];
            Handles.color = Color.red;
            float size = HandleUtility.GetHandleSize(collisionPoint) * 0.15f;
            Handles.CubeHandleCap(0, collisionPoint, Quaternion.identity, size, Event.current.type);
            Handles.Label(collisionPoint + Vector3.up * size * 2f, "Collision", EditorStyles.boldLabel);
        }
    }

    private void DrawApexHandle(NodeValidationResult nodeResult, SerializedProperty nodeProperty, Color color)
    {
        var evaluation = nodeResult.Evaluation.Value;
        var trajectory = evaluation.Trajectory;
        if (trajectory == null || trajectory.Count < 2)
        {
            return;
        }

        Vector3 apex = trajectory.OrderByDescending(p => p.y).First();
        float size = HandleUtility.GetHandleSize(apex) * 0.12f;
        Handles.color = color;

        EditorGUI.BeginChangeCheck();
        Vector3 moved = Handles.FreeMoveHandle(apex, Quaternion.identity, size, Vector3.zero, Handles.CubeHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
            Vector3 delta = moved - apex;
            var positionProperty = nodeProperty.FindPropertyRelative("_worldPosition");
            positionProperty.vector3Value += delta;
            _serializedGraph.ApplyModifiedProperties();
            EditorUtility.SetDirty(_activeGraph);
            UpdateValidation(force: true);
        }

        Handles.Label(apex + Vector3.up * size * 1.5f,
            $"Apex {apex.y:F2}", EditorStyles.miniBoldLabel);
    }

    private void DrawDurationHandles(NodeValidationResult nodeResult)
    {
        if (!nodeResult.Evaluation.HasValue)
        {
            return;
        }

        if (nodeResult.Profile is GlideProfile glide)
        {
            DrawDurationSlider(glide, "_duration", nodeResult);
        }
        else if (nodeResult.Profile is FlightProfile flight)
        {
            DrawDurationSlider(flight, "_duration", nodeResult);
        }
    }

    private void DrawDurationSlider(MoveProfile profile, string propertyName, NodeValidationResult nodeResult)
    {
        var serializedProfile = GetProfileSerialized(profile);
        if (serializedProfile == null)
        {
            return;
        }

        var durationProperty = serializedProfile.FindProperty(propertyName);
        if (durationProperty == null)
        {
            return;
        }

        float duration = durationProperty.floatValue;
        Vector3 anchor = nodeResult.Evaluation.Value.Trajectory.Last();
        Vector3 handlePosition = anchor + Vector3.up * HandleUtility.GetHandleSize(anchor) * 0.4f;
        Handles.color = profile.DebugColor;

        EditorGUI.BeginChangeCheck();
        float scaled = Handles.ScaleSlider(duration, handlePosition, Vector3.right, Quaternion.identity,
            HandleUtility.GetHandleSize(handlePosition), 0f);
        if (EditorGUI.EndChangeCheck())
        {
            durationProperty.floatValue = Mathf.Max(0.1f, scaled);
            serializedProfile.ApplyModifiedProperties();
            EditorUtility.SetDirty(profile);
            UpdateValidation(force: true);
        }

        Handles.Label(handlePosition + Vector3.up * 0.1f,
            $"Duration {durationProperty.floatValue:F2}s", EditorStyles.miniBoldLabel);
    }

    private void DrawBranchVisualization(BranchValidationResult branchResult)
    {
        bool isSelected = TryParseBranchIndices(_selectedPropertyPath, out int nodeIndex, out int branchIndex) &&
                          nodeIndex == branchResult.OriginIndex && branchIndex == branchResult.BranchIndex;

        if (!branchResult.HasTarget)
        {
            Handles.color = Color.red;
            Vector3 labelPoint = branchResult.StartPosition +
                                 Vector3.up * HandleUtility.GetHandleSize(branchResult.StartPosition) * 0.2f;
            Handles.Label(labelPoint,
                $"{branchResult.DisplayName}: {branchResult.Error}",
                EditorStyles.wordWrappedLabel);
            return;
        }

        Color color = branchResult.ProfileColor;
        if (color.a <= 0f)
        {
            color = _activeGraph != null ? _activeGraph.DefaultColor : Color.cyan;
        }

        if (branchResult.Evaluation.HasValue && branchResult.Evaluation.Value.Trajectory != null &&
            branchResult.Evaluation.Value.Trajectory.Count >= 2)
        {
            var points = branchResult.Evaluation.Value.Trajectory.ToArray();
            Handles.color = branchResult.HasError ? Color.red : color;
            Handles.DrawAAPolyLine(3f, points);
        }
        else
        {
            Handles.color = Color.red;
            Handles.DrawDottedLine(branchResult.StartPosition, branchResult.TargetPosition, 4f);
        }

        Vector3 labelPos = (branchResult.StartPosition + branchResult.TargetPosition) * 0.5f;
        float labelOffset = HandleUtility.GetHandleSize(labelPos) * 0.1f;
        Handles.Label(labelPos + Vector3.up * labelOffset,
            branchResult.HasError ? branchResult.Error : branchResult.DisplayName,
            branchResult.HasError ? EditorStyles.wordWrappedLabel : EditorStyles.miniBoldLabel);

        if (isSelected)
        {
            Handles.color = Color.yellow;
            if (branchResult.Evaluation.HasValue && branchResult.Evaluation.Value.Trajectory != null &&
                branchResult.Evaluation.Value.Trajectory.Count >= 2)
            {
                Handles.DrawAAPolyLine(5f, branchResult.Evaluation.Value.Trajectory.ToArray());
            }
            else
            {
                Handles.DrawAAPolyLine(5f, branchResult.StartPosition, branchResult.TargetPosition);
            }
        }
    }

    private SerializedObject GetProfileSerialized(MoveProfile profile)
    {
        if (profile == null)
        {
            return null;
        }

        if (!_profileSerializationCache.TryGetValue(profile, out var serialized) || serialized == null)
        {
            serialized = new SerializedObject(profile);
            _profileSerializationCache[profile] = serialized;
        }

        serialized.UpdateIfRequiredOrScript();
        return serialized;
    }

    private static Color ResolveNodeColor(SerializedProperty nodeProperty, Color fallback, Color profileColor)
    {
        var colorOverride = nodeProperty.FindPropertyRelative("_colorOverride");
        if (colorOverride != null)
        {
            var overrideColor = colorOverride.colorValue;
            if (overrideColor.a > 0f)
            {
                return overrideColor;
            }
        }

        if (profileColor.a > 0f)
        {
            return profileColor;
        }

        return fallback;
    }

    private static bool TryParseNodeIndex(string propertyPath, out int index)
    {
        index = -1;
        if (string.IsNullOrEmpty(propertyPath))
        {
            return false;
        }

        const string prefix = "_nodes.Array.data[";
        int prefixIndex = propertyPath.IndexOf(prefix, StringComparison.Ordinal);
        if (prefixIndex < 0)
        {
            return false;
        }

        int start = prefixIndex + prefix.Length;
        int end = propertyPath.IndexOf(']', start);
        if (end < 0)
        {
            return false;
        }

        string slice = propertyPath.Substring(start, end - start);
        return int.TryParse(slice, out index);
    }

    private static bool TryParseBranchIndices(string propertyPath, out int nodeIndex, out int branchIndex)
    {
        nodeIndex = -1;
        branchIndex = -1;
        if (string.IsNullOrEmpty(propertyPath))
        {
            return false;
        }

        const string nodePrefix = "_nodes.Array.data[";
        int nodeStart = propertyPath.IndexOf(nodePrefix, StringComparison.Ordinal);
        if (nodeStart < 0)
        {
            return false;
        }

        int nodeEnd = propertyPath.IndexOf(']', nodeStart + nodePrefix.Length);
        if (nodeEnd < 0)
        {
            return false;
        }

        if (!int.TryParse(propertyPath.Substring(nodeStart + nodePrefix.Length,
                nodeEnd - nodeStart - nodePrefix.Length), out nodeIndex))
        {
            return false;
        }

        const string branchPrefix = "_branches.Array.data[";
        int branchStart = propertyPath.IndexOf(branchPrefix, nodeEnd, StringComparison.Ordinal);
        if (branchStart < 0)
        {
            return false;
        }

        int branchEnd = propertyPath.IndexOf(']', branchStart + branchPrefix.Length);
        if (branchEnd < 0)
        {
            return false;
        }

        return int.TryParse(propertyPath.Substring(branchStart + branchPrefix.Length,
            branchEnd - branchStart - branchPrefix.Length), out branchIndex);
    }
    private class RouteTreeView : TreeView
    {
        private readonly Dictionary<int, string> _propertyLookup = new Dictionary<int, string>();
        private readonly Dictionary<int, string> _displayLookup = new Dictionary<int, string>();
        private readonly HashSet<int> _errorItems = new HashSet<int>();

        private SerializedObject _graph;
        private SerializedProperty _nodesProperty;
        private GraphValidationReport _report;

        public RouteTreeView(TreeViewState state) : base(state)
        {
            showBorder = true;
            Reload();
        }

        public event Action<SelectionData> SelectionChangedEvent;

        public void SetGraph(SerializedObject graph, GraphValidationReport report)
        {
            _graph = graph;
            _nodesProperty = graph != null ? graph.FindProperty("_nodes") : null;
            _report = report;
            Reload();
        }

        public void SetSelectionByPath(string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath))
            {
                SetSelection(new List<int>());
                return;
            }

            int id = ResolveId(propertyPath);
            if (id != 0)
            {
                SetSelection(new List<int> { id });
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            _propertyLookup.Clear();
            _displayLookup.Clear();
            _errorItems.Clear();

            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            var items = new List<TreeViewItem>();
            int nextId = 1;

            if (_nodesProperty != null && _nodesProperty.isArray)
            {
                for (int i = 0; i < _nodesProperty.arraySize; i++)
                {
                    var nodeProperty = _nodesProperty.GetArrayElementAtIndex(i);
                    string label = $"Node {i}: {GetNodeLabel(nodeProperty)}";
                    var nodeItem = new TreeViewItem { id = nextId++, depth = 0, displayName = label };
                    items.Add(nodeItem);
                    _propertyLookup[nodeItem.id] = nodeProperty.propertyPath;
                    _displayLookup[nodeItem.id] = label;

                    if (_report?.GetNode(i)?.HasError == true)
                    {
                        _errorItems.Add(nodeItem.id);
                    }

                    var branches = nodeProperty.FindPropertyRelative("_branches");
                    if (branches != null && branches.isArray)
                    {
                        for (int b = 0; b < branches.arraySize; b++)
                        {
                            var branchProperty = branches.GetArrayElementAtIndex(b);
                            int target = branchProperty.FindPropertyRelative("_targetNodeIndex")?.intValue ?? -1;
                            string branchLabel = $"Branch {b}: → {target}";
                            var branchItem = new TreeViewItem { id = nextId++, depth = 1, displayName = branchLabel };
                            items.Add(branchItem);
                            _propertyLookup[branchItem.id] = branchProperty.propertyPath;
                            _displayLookup[branchItem.id] = branchLabel;

                            if (_report?.GetBranch(i, b)?.HasError == true)
                            {
                                _errorItems.Add(branchItem.id);
                            }
                        }
                    }
                }
            }

            SetupParentsAndChildrenFromDepths(root, items);
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (_errorItems.Contains(args.item.id))
            {
                var rect = args.rowRect;
                EditorGUI.DrawRect(rect, new Color(0.8f, 0.2f, 0.2f, 0.25f));
            }

            base.RowGUI(args);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds == null || selectedIds.Count == 0)
            {
                SelectionChangedEvent?.Invoke(null);
                return;
            }

            int id = selectedIds[0];
            if (_propertyLookup.TryGetValue(id, out var path))
            {
                _displayLookup.TryGetValue(id, out var displayName);
                SelectionChangedEvent?.Invoke(new SelectionData(path, displayName ?? path));
            }
            else
            {
                SelectionChangedEvent?.Invoke(null);
            }
        }

        private int ResolveId(string propertyPath)
        {
            foreach (var pair in _propertyLookup)
            {
                if (pair.Value == propertyPath)
                {
                    return pair.Key;
                }
            }

            return 0;
        }

        private static string GetNodeLabel(SerializedProperty nodeProperty)
        {
            var labelProp = nodeProperty.FindPropertyRelative("_label");
            string label = labelProp != null ? labelProp.stringValue : string.Empty;
            return string.IsNullOrEmpty(label) ? "Node" : label;
        }

        public readonly struct SelectionData
        {
            public SelectionData(string propertyPath, string displayName)
            {
                PropertyPath = propertyPath;
                DisplayName = displayName;
            }

            public string PropertyPath { get; }
            public string DisplayName { get; }
        }
    }

    private sealed class GraphValidationReport
    {
        private readonly List<string> _errors = new List<string>();
        private readonly Dictionary<int, NodeValidationResult> _nodeLookup = new Dictionary<int, NodeValidationResult>();
        private readonly Dictionary<(int, int), BranchValidationResult> _branchLookup =
            new Dictionary<(int, int), BranchValidationResult>();

        public readonly List<NodeValidationResult> Nodes = new List<NodeValidationResult>();
        public readonly List<BranchValidationResult> Branches = new List<BranchValidationResult>();

        public IReadOnlyList<string> Errors => _errors;
        public bool HasErrors => _errors.Count > 0;
        public string Summary { get; set; } = string.Empty;

        public void AddNode(NodeValidationResult node)
        {
            Nodes.Add(node);
            _nodeLookup[node.Index] = node;
            if (node.HasError)
            {
                _errors.Add($"{node.DisplayName}: {node.Error}");
            }
        }

        public void AddBranch(BranchValidationResult branch)
        {
            Branches.Add(branch);
            _branchLookup[(branch.OriginIndex, branch.BranchIndex)] = branch;
            if (branch.HasError && !string.IsNullOrEmpty(branch.Error))
            {
                _errors.Add($"{branch.DisplayName}: {branch.Error}");
            }
        }

        public NodeValidationResult GetNode(int index)
        {
            return _nodeLookup.TryGetValue(index, out var node) ? node : null;
        }

        public BranchValidationResult GetBranch(int nodeIndex, int branchIndex)
        {
            return _branchLookup.TryGetValue((nodeIndex, branchIndex), out var branch) ? branch : null;
        }
    }

    private sealed class NodeValidationResult
    {
        public int Index { get; set; }
        public string DisplayName { get; set; }
        public MoveProfile Profile { get; set; }
        public Color ProfileColor { get; set; }
        public MoveEvaluation? Evaluation { get; set; }
        public string Error { get; set; }
        public bool StateMismatch { get; set; }
        public Vector3 StartPosition { get; set; }
        public Vector3 TargetPosition { get; set; }
        public PlayerStateSnapshot StartState { get; set; }

        public bool HasError => !string.IsNullOrEmpty(Error);
    }

    private sealed class BranchValidationResult
    {
        public int OriginIndex { get; set; }
        public int BranchIndex { get; set; }
        public string DisplayName { get; set; }
        public MoveProfile Profile { get; set; }
        public Color ProfileColor { get; set; }
        public MoveEvaluation? Evaluation { get; set; }
        public string Error { get; set; }
        public Vector3 StartPosition { get; set; }
        public Vector3 TargetPosition { get; set; }
        public bool HasTarget { get; set; }

        public bool HasError => !string.IsNullOrEmpty(Error);
    }

    private static class GraphValidator
    {
        public static GraphValidationReport Build(RouteGraph graph)
        {
            var report = new GraphValidationReport();
            if (graph == null)
            {
                report.Summary = "No route selected.";
                return report;
            }

            var nodes = graph.Nodes;
            if (nodes == null || nodes.Count == 0)
            {
                report.Summary = "Route graph has no nodes.";
                return report;
            }

            var first = nodes[0];
            var firstResult = new NodeValidationResult
            {
                Index = 0,
                DisplayName = $"Node 0: {first.Label}",
                Profile = first.MoveProfile,
                ProfileColor = first.MoveProfile != null ? first.MoveProfile.DebugColor : graph.DefaultColor,
                Evaluation = null,
                Error = string.Empty,
                StateMismatch = false,
                StartPosition = first.WorldPosition,
                TargetPosition = first.WorldPosition,
                StartState = first.ExpectedState
            };
            report.AddNode(firstResult);

            ValidateBranches(graph, first, 0, first.WorldPosition, first.ExpectedState, report);

            Vector3 currentPosition = first.WorldPosition;
            PlayerStateSnapshot currentState = first.ExpectedState;

            for (int i = 1; i < nodes.Count; i++)
            {
                var node = nodes[i];
                var result = new NodeValidationResult
                {
                    Index = i,
                    DisplayName = $"Node {i}: {node.Label}",
                    Profile = node.MoveProfile,
                    ProfileColor = node.MoveProfile != null ? node.MoveProfile.DebugColor : graph.DefaultColor,
                    StartPosition = currentPosition,
                    TargetPosition = node.WorldPosition,
                    StartState = currentState
                };

                if (!node.TryEvaluate(currentPosition, currentState, out var evaluation, out string error))
                {
                    result.Error = error;
                }
                else
                {
                    result.Evaluation = evaluation;
                    currentPosition = node.WorldPosition;
                    currentState = evaluation.EndState;

                    if (!node.ExpectedState.ApproximatelyEquals(evaluation.EndState))
                    {
                        result.StateMismatch = true;
                        result.Error =
                            $"Expected {node.ExpectedState} but got {evaluation.EndState}.";
                    }
                }

                report.AddNode(result);
                ValidateBranches(graph, node, i, currentPosition, currentState, report);
            }

            report.Summary = report.HasErrors
                ? "Route contains validation issues. Review highlighted nodes and branches."
                : "Route validated successfully.";

            return report;
        }

        private static void ValidateBranches(RouteGraph graph, RouteNode node, int nodeIndex,
            Vector3 nodePosition, PlayerStateSnapshot nodeState, GraphValidationReport report)
        {
            if (!node.HasBranches)
            {
                return;
            }

            int branchIndex = 0;
            foreach (var branch in node.Branches)
            {
                var branchResult = new BranchValidationResult
                {
                    OriginIndex = nodeIndex,
                    BranchIndex = branchIndex,
                    DisplayName = $"Branch {branchIndex}: {node.Label} → {branch.TargetNodeIndex}",
                    Profile = branch.ProfileOverride != null ? branch.ProfileOverride : node.MoveProfile,
                    ProfileColor = branch.ColorTint,
                    StartPosition = nodePosition
                };

                if (!graph.TryGetNode(branch.TargetNodeIndex, out var target))
                {
                    branchResult.Error = $"Invalid branch target index {branch.TargetNodeIndex}.";
                    report.AddBranch(branchResult);
                    branchIndex++;
                    continue;
                }

                branchResult.HasTarget = true;
                branchResult.TargetPosition = target.WorldPosition;

                var profile = branchResult.Profile;
                if (profile == null)
                {
                    branchResult.Error = "No move profile available for branch transition.";
                    report.AddBranch(branchResult);
                    branchIndex++;
                    continue;
                }

                if (!profile.TryEvaluate(nodePosition, target.WorldPosition, nodeState, out var evaluation,
                        out string error))
                {
                    branchResult.Error = error;
                }
                else
                {
                    branchResult.Evaluation = evaluation;
                    if (!branch.ExpectedState.ApproximatelyEquals(evaluation.EndState))
                    {
                        branchResult.Error =
                            $"Expected {branch.ExpectedState} but got {evaluation.EndState}.";
                    }
                }

                if (branchResult.ProfileColor.a <= 0f)
                {
                    branchResult.ProfileColor = profile.DebugColor;
                }

                report.AddBranch(branchResult);
                branchIndex++;
            }
        }
    }
}
