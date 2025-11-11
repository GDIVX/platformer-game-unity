
using System;
using System.Collections.Generic;
using System.Linq;
using RoutePlanning.Profiles;
using RoutePlanning.RoutePlanning;
using Runtime.Player.Movement;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class RouteAuthoringWindow : EditorWindow
{
    private const float LibraryWidth = 280f;
    private const string PlacementIdle = "Place Route Gizmo";
    private const string PlacementActive = "Click in Scene to Place";

    private readonly List<MoveProfile> _allProfiles = new List<MoveProfile>();
    private readonly List<NodeEntry> _nodeEntries = new List<NodeEntry>();
    private readonly List<MoveProfile> _visibleProfiles = new List<MoveProfile>();

    private RouteGraph _activeGraph;
    private SerializedObject _graphSerialized;
    private SerializedProperty _nodesProperty;

    private MoveProfile _selectedProfile;
    private int _selectedNodeIndex = -1;

    private MoveProfile _pendingPlacementProfile;
    private Vector3 _pendingPlacementPosition;
    private bool _pendingPlacementValid;

    private ObjectField _graphField;
    private ToolbarSearchField _profileSearchField;
    private ListView _profileListView;
    private Label _profileDetailLabel;
    private Button _placementButton;

    private ListView _nodeListView;
    private VisualElement _nodeInspector;
    private Button _deleteNodeButton;
    private Button _snapToSimulationButton;
    private HelpBox _validationBox;

    [MenuItem("Tools/Route Authoring")]
    public static void ShowWindow()
    {
        var window = GetWindow<RouteAuthoringWindow>();
        window.titleContent = new GUIContent("Route Authoring");
        window.Show();
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        Undo.undoRedoPerformed += OnUndoRedoPerformed;
        Selection.selectionChanged += OnEditorSelectionChanged;

        BuildUI();
        LoadProfiles();

        if (Selection.activeObject is RouteGraph graph)
        {
            SetActiveGraph(graph);
        }
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        Selection.selectionChanged -= OnEditorSelectionChanged;
        CancelPlacement();
    }

    private void BuildUI()
    {
        var root = rootVisualElement;
        root.Clear();
        root.style.flexDirection = FlexDirection.Row;

        var libraryPanel = new VisualElement
        {
            style =
            {
                flexBasis = LibraryWidth,
                flexShrink = 0f,
                flexGrow = 0f,
                paddingLeft = 6,
                paddingRight = 6,
                paddingTop = 6,
                paddingBottom = 6,
                backgroundColor = new Color(0.13f, 0.13f, 0.13f)
            }
        };

        var libraryHeader = new Label("Route Library")
        {
            style =
            {
                unityFontStyleAndWeight = FontStyle.Bold,
                marginBottom = 4
            }
        };
        libraryPanel.Add(libraryHeader);

        _profileSearchField = new ToolbarSearchField();
        _profileSearchField.RegisterValueChangedCallback(evt => FilterProfiles(evt.newValue));
        libraryPanel.Add(_profileSearchField);

        _profileListView = new ListView
        {
            selectionType = SelectionType.Single,
            style =
            {
                flexGrow = 1f,
                marginTop = 4,
                marginBottom = 4
            }
        };
        _profileListView.makeItem = MakeProfileItem;
        _profileListView.bindItem = BindProfileItem;
        _profileListView.onSelectionChange += OnProfileSelectionChanged;
        _profileListView.RegisterCallback<PointerDownEvent>(OnProfilePointerDown);
        libraryPanel.Add(_profileListView);

        _profileDetailLabel = new Label("Select a profile to see details.")
        {
            style =
            {
                whiteSpace = WhiteSpace.Normal,
                unityTextAlign = TextAnchor.UpperLeft,
                marginBottom = 6
            }
        };
        libraryPanel.Add(_profileDetailLabel);

        _placementButton = new Button(BeginPlacement)
        {
            text = PlacementIdle,
            style =
            {
                marginTop = 4,
                marginBottom = 4
            }
        };
        libraryPanel.Add(_placementButton);

        root.Add(libraryPanel);

        var editorPanel = new VisualElement
        {
            style =
            {
                flexGrow = 1f,
                flexDirection = FlexDirection.Column,
                paddingLeft = 8,
                paddingRight = 8,
                paddingTop = 6,
                paddingBottom = 6
            }
        };

        var graphRow = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                alignItems = Align.Center,
                marginBottom = 6
            }
        };

        var graphLabel = new Label("Active Route Graph")
        {
            style =
            {
                unityFontStyleAndWeight = FontStyle.Bold,
                marginRight = 6
            }
        };
        graphRow.Add(graphLabel);

        _graphField = new ObjectField
        {
            objectType = typeof(RouteGraph),
            allowSceneObjects = false,
            style =
            {
                flexGrow = 1f
            }
        };
        _graphField.RegisterValueChangedCallback(evt => SetActiveGraph(evt.newValue as RouteGraph));
        graphRow.Add(_graphField);

        editorPanel.Add(graphRow);

        _validationBox = new HelpBox("", HelpBoxMessageType.Info)
        {
            style =
            {
                display = DisplayStyle.None,
                marginBottom = 6
            }
        };
        editorPanel.Add(_validationBox);

        var nodeHeader = new Label("Placed Gizmos")
        {
            style =
            {
                unityFontStyleAndWeight = FontStyle.Bold,
                marginBottom = 4
            }
        };
        editorPanel.Add(nodeHeader);

        _nodeListView = new ListView
        {
            selectionType = SelectionType.Single,
            style =
            {
                flexGrow = 1f,
                minHeight = 160
            }
        };
        _nodeListView.makeItem = MakeNodeItem;
        _nodeListView.bindItem = BindNodeItem;
        _nodeListView.onSelectionChange += OnNodeSelectionChanged;
        editorPanel.Add(_nodeListView);

        var nodeButtons = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                justifyContent = Justify.FlexEnd,
                marginTop = 4,
                marginBottom = 4
            }
        };

        _deleteNodeButton = new Button(DeleteSelectedNode) { text = "Delete" };
        nodeButtons.Add(_deleteNodeButton);

        _snapToSimulationButton = new Button(SnapSelectedNodeToSimulation)
        {
            text = "Revert to Simulation",
            style =
            {
                marginLeft = 6
            }
        };
        nodeButtons.Add(_snapToSimulationButton);

        editorPanel.Add(nodeButtons);

        var inspectorHeader = new Label("Gizmo Inspector")
        {
            style =
            {
                unityFontStyleAndWeight = FontStyle.Bold,
                marginBottom = 4
            }
        };
        editorPanel.Add(inspectorHeader);

        _nodeInspector = new ScrollView
        {
            style =
            {
                flexGrow = 1f,
                minHeight = 200
            }
        };
        editorPanel.Add(_nodeInspector);

        root.Add(editorPanel);

        UpdateActionStates();
    }

    private VisualElement MakeProfileItem()
    {
        var row = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                alignItems = Align.Center,
                paddingLeft = 4,
                paddingRight = 4,
                paddingTop = 2,
                paddingBottom = 2
            }
        };

        var swatch = new VisualElement
        {
            style =
            {
                width = 12,
                height = 12,
                marginRight = 6,
                borderBottomLeftRadius = 2,
                borderBottomRightRadius = 2,
                borderTopLeftRadius = 2,
                borderTopRightRadius = 2
            }
        };
        swatch.name = "swatch";
        row.Add(swatch);

        var label = new Label
        {
            style =
            {
                flexGrow = 1f,
                whiteSpace = WhiteSpace.NoWrap
            }
        };
        label.name = "label";
        row.Add(label);

        return row;
    }

    private void BindProfileItem(VisualElement element, int index)
    {
        var swatch = element.Q<VisualElement>("swatch");
        var label = element.Q<Label>("label");

        MoveProfile profile = null;
        if (index >= 0 && index < _visibleProfiles.Count)
        {
            profile = _visibleProfiles[index];
        }

        label.text = profile != null ? profile.name : "<None>";
        swatch.style.backgroundColor = profile != null ? profile.DebugColor : new Color(0.25f, 0.25f, 0.25f);
    }

    private VisualElement MakeNodeItem()
    {
        var row = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                alignItems = Align.Center,
                paddingLeft = 4,
                paddingRight = 4,
                paddingTop = 2,
                paddingBottom = 2
            }
        };

        var swatch = new VisualElement
        {
            style =
            {
                width = 10,
                height = 10,
                marginRight = 6,
                borderBottomLeftRadius = 2,
                borderBottomRightRadius = 2,
                borderTopLeftRadius = 2,
                borderTopRightRadius = 2
            }
        };
        swatch.name = "swatch";
        row.Add(swatch);

        var label = new Label
        {
            style =
            {
                flexGrow = 1f,
                whiteSpace = WhiteSpace.NoWrap
            }
        };
        label.name = "label";
        row.Add(label);

        return row;
    }

    private void BindNodeItem(VisualElement element, int index)
    {
        var swatch = element.Q<VisualElement>("swatch");
        var label = element.Q<Label>("label");

        NodeEntry entry = null;
        if (index >= 0 && index < _nodeEntries.Count)
        {
            entry = _nodeEntries[index];
        }

        label.text = entry != null ? $"{index + 1}. {entry.DisplayName}" : string.Empty;
        swatch.style.backgroundColor = entry != null ? entry.Color : Color.clear;
    }

    private void LoadProfiles()
    {
        _allProfiles.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:MoveProfile"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var profile = AssetDatabase.LoadAssetAtPath<MoveProfile>(path);
            if (profile != null && !_allProfiles.Contains(profile))
            {
                _allProfiles.Add(profile);
            }
        }

        _allProfiles.Sort((a, b) => string.Compare(a?.name, b?.name, StringComparison.OrdinalIgnoreCase));
        FilterProfiles(_profileSearchField?.value ?? string.Empty);
    }

    private void FilterProfiles(string filter)
    {
        _visibleProfiles.Clear();

        if (string.IsNullOrWhiteSpace(filter))
        {
            _visibleProfiles.AddRange(_allProfiles);
        }
        else
        {
            var lower = filter.Trim().ToLowerInvariant();
            foreach (var profile in _allProfiles)
            {
                if (profile != null && profile.name.ToLowerInvariant().Contains(lower))
                {
                    _visibleProfiles.Add(profile);
                }
            }
        }

        _profileListView.itemsSource = _visibleProfiles;
        _profileListView.Rebuild();

        if (_selectedProfile != null && !_visibleProfiles.Contains(_selectedProfile))
        {
            SetSelectedProfile(null);
        }
    }

    private void OnProfileSelectionChanged(IEnumerable<object> selection)
    {
        var profile = selection.OfType<MoveProfile>().FirstOrDefault();
        SetSelectedProfile(profile);
    }

    private void OnProfilePointerDown(PointerDownEvent evt)
    {
        if (evt.clickCount == 2)
        {
            BeginPlacement();
        }
    }

    private void SetSelectedProfile(MoveProfile profile)
    {
        _selectedProfile = profile;

        if (profile == null)
        {
            _profileListView.ClearSelection();
            _profileDetailLabel.text = "Select a profile to see details.";
        }
        else
        {
            int index = _visibleProfiles.IndexOf(profile);
            if (index >= 0)
            {
                _profileListView.SetSelectionWithoutNotify(index);
            }

            _profileDetailLabel.text =
                $"<b>{profile.name}</b>\nStamina Cost: {profile.StaminaCost:F1}\nTrajectory Samples: {profile.TrajectorySamples}";
        }

        UpdateActionStates();
    }

    private void BeginPlacement()
    {
        if (_selectedProfile == null)
        {
            EditorUtility.DisplayDialog("Route Authoring", "Select a route profile to place first.", "OK");
            return;
        }

        if (_activeGraph == null)
        {
            EditorUtility.DisplayDialog("Route Authoring", "Assign a RouteGraph asset to edit.", "OK");
            return;
        }

        _pendingPlacementProfile = _selectedProfile;
        _pendingPlacementValid = false;
        if (_placementButton != null)
        {
            _placementButton.text = PlacementActive;
        }

        SceneView.FocusWindowIfItsOpen<SceneView>();
        SceneView.RepaintAll();
    }

    private void CancelPlacement()
    {
        _pendingPlacementProfile = null;
        _pendingPlacementValid = false;
        if (_placementButton != null)
        {
            _placementButton.text = PlacementIdle;
        }
    }

    private void OnNodeSelectionChanged(IEnumerable<object> selection)
    {
        var entry = selection.OfType<NodeEntry>().FirstOrDefault();
        if (entry != null)
        {
            SelectNode(entry.Index);
        }
        else
        {
            SelectNode(-1);
        }
    }

    private void SelectNode(int index)
    {
        _selectedNodeIndex = index;

        if (index < 0)
        {
            _nodeListView.ClearSelection();
        }
        else
        {
            for (int i = 0; i < _nodeEntries.Count; i++)
            {
                if (_nodeEntries[i].Index == index)
                {
                    _nodeListView.SetSelectionWithoutNotify(i);
                    break;
                }
            }
        }

        UpdateNodeInspector();
        UpdateActionStates();
        SceneView.RepaintAll();
    }

    private void UpdateNodeInspector()
    {
        _nodeInspector.Clear();

        if (_graphSerialized == null || _nodesProperty == null ||
            _selectedNodeIndex < 0 || _selectedNodeIndex >= _nodesProperty.arraySize)
        {
            return;
        }

        _graphSerialized.Update();
        var nodeProperty = _nodesProperty.GetArrayElementAtIndex(_selectedNodeIndex);

        var labelField = new PropertyField(nodeProperty.FindPropertyRelative("_label"), "Label");
        labelField.Bind(_graphSerialized);
        _nodeInspector.Add(labelField);

        var positionField = new PropertyField(nodeProperty.FindPropertyRelative("_worldPosition"), "World Position");
        positionField.Bind(_graphSerialized);
        _nodeInspector.Add(positionField);

        var profileField = new PropertyField(nodeProperty.FindPropertyRelative("_moveProfile"), "Move Profile");
        profileField.Bind(_graphSerialized);
        _nodeInspector.Add(profileField);

        var useSelectedProfile = new Button(AssignSelectedProfileToNode)
        {
            text = _selectedProfile != null ? $"Use Selected Profile ({_selectedProfile.name})" : "Use Selected Profile",
            style =
            {
                marginTop = 4,
                marginBottom = 4
            }
        };
        useSelectedProfile.SetEnabled(_selectedProfile != null);
        _nodeInspector.Add(useSelectedProfile);

        var expectedFoldout = new Foldout { text = "Expected State" };
        var expectedState = nodeProperty.FindPropertyRelative("_expectedState");
        expectedFoldout.Add(new PropertyField(expectedState.FindPropertyRelative("_velocity"), "Velocity"));
        expectedFoldout.Add(new PropertyField(expectedState.FindPropertyRelative("_stamina"), "Stamina"));
        expectedFoldout.Add(new PropertyField(expectedState.FindPropertyRelative("_dashCooldown"), "Dash Cooldown"));
        expectedFoldout.Add(new PropertyField(expectedState.FindPropertyRelative("_airDashCooldown"), "Air Dash Cooldown"));
        expectedFoldout.Add(new PropertyField(expectedState.FindPropertyRelative("_glideTimeRemaining"), "Glide Time"));
        expectedFoldout.Add(new PropertyField(expectedState.FindPropertyRelative("_flightTimeRemaining"), "Flight Time"));
        expectedFoldout.Add(new PropertyField(expectedState.FindPropertyRelative("_airDashCount"), "Air Dash Count"));
        expectedFoldout.Bind(_graphSerialized);
        _nodeInspector.Add(expectedFoldout);

        var detailsFoldout = new Foldout { text = "Details" };
        detailsFoldout.Add(new PropertyField(nodeProperty.FindPropertyRelative("_designerNotes"), "Notes"));
        detailsFoldout.Add(new PropertyField(nodeProperty.FindPropertyRelative("_important"), "Important"));
        detailsFoldout.Add(new PropertyField(nodeProperty.FindPropertyRelative("_colorOverride"), "Color Override"));
        detailsFoldout.Bind(_graphSerialized);
        _nodeInspector.Add(detailsFoldout);

        var branchesFoldout = new Foldout { text = "Branches" };
        branchesFoldout.Add(new PropertyField(nodeProperty.FindPropertyRelative("_branches")));
        branchesFoldout.Bind(_graphSerialized);
        _nodeInspector.Add(branchesFoldout);
    }

    private void AssignSelectedProfileToNode()
    {
        if (_selectedProfile == null || _graphSerialized == null || _nodesProperty == null ||
            _selectedNodeIndex < 0 || _selectedNodeIndex >= _nodesProperty.arraySize)
        {
            return;
        }

        Undo.RegisterCompleteObjectUndo(_activeGraph, "Assign Move Profile");
        _graphSerialized.Update();
        var nodeProperty = _nodesProperty.GetArrayElementAtIndex(_selectedNodeIndex);
        nodeProperty.FindPropertyRelative("_moveProfile").objectReferenceValue = _selectedProfile;
        _graphSerialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(_activeGraph);

        UpdateNodeEntries();
        UpdateValidationMessages();
        SceneView.RepaintAll();
    }

    private void SetActiveGraph(RouteGraph graph)
    {
        if (_activeGraph == graph)
        {
            return;
        }

        _activeGraph = graph;

        if (_graphField != null)
        {
            _graphField.SetValueWithoutNotify(graph);
        }

        if (_activeGraph != null)
        {
            _graphSerialized = new SerializedObject(_activeGraph);
            _nodesProperty = _graphSerialized.FindProperty("_nodes");
        }
        else
        {
            _graphSerialized = null;
            _nodesProperty = null;
        }

        _selectedNodeIndex = -1;
        UpdateNodeEntries();
        UpdateNodeInspector();
        UpdateValidationMessages();
        UpdateActionStates();
        SceneView.RepaintAll();
    }

    private void UpdateNodeEntries()
    {
        _nodeEntries.Clear();

        if (_activeGraph != null)
        {
            var nodes = _activeGraph.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                var color = node.ColorOverride.a > 0f
                    ? node.ColorOverride
                    : node.MoveProfile != null ? node.MoveProfile.DebugColor : _activeGraph.DefaultColor;

                _nodeEntries.Add(new NodeEntry
                {
                    Index = i,
                    DisplayName = string.IsNullOrEmpty(node.Label) ? $"Node {i + 1}" : node.Label,
                    Color = color,
                    Profile = node.MoveProfile
                });
            }
        }

        _nodeListView.itemsSource = _nodeEntries;
        _nodeListView.Rebuild();
    }

    private void UpdateActionStates()
    {
        bool hasGraph = _activeGraph != null;
        bool hasProfile = _selectedProfile != null;
        bool hasNodeSelection = _selectedNodeIndex >= 0 && _selectedNodeIndex < _nodeEntries.Count;

        _placementButton?.SetEnabled(hasGraph && hasProfile);
        _deleteNodeButton?.SetEnabled(hasNodeSelection);
        _snapToSimulationButton?.SetEnabled(hasNodeSelection);
    }

    private void DeleteSelectedNode()
    {
        if (_graphSerialized == null || _nodesProperty == null ||
            _selectedNodeIndex < 0 || _selectedNodeIndex >= _nodesProperty.arraySize)
        {
            return;
        }

        Undo.RegisterCompleteObjectUndo(_activeGraph, "Delete Route Gizmo");
        _graphSerialized.Update();
        _nodesProperty.DeleteArrayElementAtIndex(_selectedNodeIndex);
        _graphSerialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(_activeGraph);

        _selectedNodeIndex = -1;
        UpdateNodeEntries();
        UpdateNodeInspector();
        UpdateValidationMessages();
        UpdateActionStates();
        SceneView.RepaintAll();
    }

    private void SnapSelectedNodeToSimulation()
    {
        if (_activeGraph == null || _selectedNodeIndex < 0 || _selectedNodeIndex >= _activeGraph.Nodes.Count)
        {
            return;
        }

        var node = _activeGraph.Nodes[_selectedNodeIndex];
        if (node.MoveProfile == null)
        {
            EditorUtility.DisplayDialog("Route Authoring", "Assign a move profile before reverting.", "OK");
            return;
        }

        var (startPos, startState) = GetSimulationStart(_selectedNodeIndex, _activeGraph.Nodes);
        if (!node.MoveProfile.TryEvaluate(startPos, node.WorldPosition, startState, out var evaluation, out var error))
        {
            EditorUtility.DisplayDialog("Route Authoring", $"Simulation failed: {error}", "OK");
            return;
        }

        Undo.RegisterCompleteObjectUndo(_activeGraph, "Snap Gizmo to Simulation");
        _graphSerialized.Update();
        var nodeProperty = _nodesProperty.GetArrayElementAtIndex(_selectedNodeIndex);
        nodeProperty.FindPropertyRelative("_worldPosition").vector3Value = GetTrajectoryEnd(evaluation, node.WorldPosition);
        ApplySnapshotToProperty(nodeProperty.FindPropertyRelative("_expectedState"), evaluation.EndState);
        _graphSerialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(_activeGraph);

        UpdateNodeEntries();
        UpdateNodeInspector();
        UpdateValidationMessages();
        SceneView.RepaintAll();
    }

    private (Vector3 position, PlayerStateSnapshot state) GetSimulationStart(int nodeIndex, IReadOnlyList<RouteNode> nodes)
    {
        if (nodeIndex <= 0)
        {
            return (nodes[nodeIndex].WorldPosition, default);
        }

        var previous = nodes[nodeIndex - 1];
        return (previous.WorldPosition, previous.ExpectedState);
    }

    private void PlacePendingNode(Vector3 position)
    {
        if (_pendingPlacementProfile == null || _graphSerialized == null || _nodesProperty == null)
        {
            return;
        }

        Undo.RegisterCompleteObjectUndo(_activeGraph, "Add Route Gizmo");
        _graphSerialized.Update();
        int newIndex = _nodesProperty.arraySize;
        _nodesProperty.InsertArrayElementAtIndex(newIndex);
        var nodeProperty = _nodesProperty.GetArrayElementAtIndex(newIndex);
        nodeProperty.FindPropertyRelative("_label").stringValue = _pendingPlacementProfile.name;
        nodeProperty.FindPropertyRelative("_worldPosition").vector3Value = position;
        nodeProperty.FindPropertyRelative("_moveProfile").objectReferenceValue = _pendingPlacementProfile;
        ApplySnapshotToProperty(nodeProperty.FindPropertyRelative("_expectedState"), default);
        nodeProperty.FindPropertyRelative("_designerNotes").stringValue = string.Empty;
        nodeProperty.FindPropertyRelative("_important").boolValue = false;
        nodeProperty.FindPropertyRelative("_colorOverride").colorValue = new Color(0f, 0f, 0f, 0f);
        var branches = nodeProperty.FindPropertyRelative("_branches");
        branches.arraySize = 0;

        _graphSerialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(_activeGraph);

        TryPopulateNodeFromSimulation(newIndex);

        CancelPlacement();
        UpdateNodeEntries();
        SelectNode(newIndex);
        UpdateValidationMessages();
    }

    private void TryPopulateNodeFromSimulation(int nodeIndex)
    {
        if (_activeGraph == null || nodeIndex < 0 || nodeIndex >= _activeGraph.Nodes.Count)
        {
            return;
        }

        var node = _activeGraph.Nodes[nodeIndex];
        if (node.MoveProfile == null)
        {
            return;
        }

        var (startPos, startState) = GetSimulationStart(nodeIndex, _activeGraph.Nodes);
        if (!node.MoveProfile.TryEvaluate(startPos, node.WorldPosition, startState, out var evaluation, out _))
        {
            return;
        }

        _graphSerialized.Update();
        var nodeProperty = _nodesProperty.GetArrayElementAtIndex(nodeIndex);
        ApplySnapshotToProperty(nodeProperty.FindPropertyRelative("_expectedState"), evaluation.EndState);
        _graphSerialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(_activeGraph);
    }

    private void ApplySnapshotToProperty(SerializedProperty stateProperty, PlayerStateSnapshot snapshot)
    {
        if (stateProperty == null)
        {
            return;
        }

        stateProperty.FindPropertyRelative("_velocity").vector2Value = snapshot.Velocity;
        stateProperty.FindPropertyRelative("_stamina").floatValue = snapshot.Stamina;
        stateProperty.FindPropertyRelative("_dashCooldown").floatValue = snapshot.DashCooldown;
        stateProperty.FindPropertyRelative("_airDashCooldown").floatValue = snapshot.AirDashCooldown;
        stateProperty.FindPropertyRelative("_glideTimeRemaining").floatValue = snapshot.GlideTimeRemaining;
        stateProperty.FindPropertyRelative("_flightTimeRemaining").floatValue = snapshot.FlightTimeRemaining;
        stateProperty.FindPropertyRelative("_airDashCount").intValue = snapshot.AirDashCount;
    }

    private void OnUndoRedoPerformed()
    {
        if (_activeGraph == null)
        {
            return;
        }

        _graphSerialized = new SerializedObject(_activeGraph);
        _nodesProperty = _graphSerialized.FindProperty("_nodes");
        UpdateNodeEntries();
        UpdateNodeInspector();
        UpdateValidationMessages();
        SceneView.RepaintAll();
    }

    private void OnEditorSelectionChanged()
    {
        if (Selection.activeObject is RouteGraph graph)
        {
            SetActiveGraph(graph);
        }
    }

    private void UpdateValidationMessages()
    {
        if (_validationBox == null)
        {
            return;
        }

        if (_activeGraph == null)
        {
            _validationBox.style.display = DisplayStyle.None;
            return;
        }

        bool isValid = _activeGraph.TryValidate(out var errors);
        if (isValid)
        {
            if (errors.Count == 0)
            {
                _validationBox.text = "All transitions simulate successfully.";
                _validationBox.messageType = HelpBoxMessageType.Info;
            }
            else
            {
                _validationBox.text = string.Join("
", errors);
                _validationBox.messageType = HelpBoxMessageType.Warning;
            }
        }
        else
        {
            _validationBox.text = string.Join("
", errors);
            _validationBox.messageType = HelpBoxMessageType.Error;
        }

        _validationBox.style.display = DisplayStyle.Flex;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (_activeGraph == null)
        {
            return;
        }

        Handles.zTest = CompareFunction.LessEqual;

        DrawPendingPlacement(sceneView);
        DrawRouteNodes();
    }

    private void DrawPendingPlacement(SceneView sceneView)
    {
        if (_pendingPlacementProfile == null)
        {
            return;
        }

        var evt = Event.current;
        UpdatePlacementPreview(evt);
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        if (_pendingPlacementValid)
        {
            Handles.color = _pendingPlacementProfile.DebugColor;
            Handles.DrawWireDisc(_pendingPlacementPosition, Vector3.up, 0.75f);
            Handles.SphereHandleCap(0, _pendingPlacementPosition, Quaternion.identity,
                HandleUtility.GetHandleSize(_pendingPlacementPosition) * 0.08f, EventType.Repaint);
            Handles.Label(_pendingPlacementPosition + Vector3.up * HandleUtility.GetHandleSize(_pendingPlacementPosition) * 0.4f,
                $"{_pendingPlacementProfile.name} (click to confirm)");
        }

        if (evt.type == EventType.MouseDown && evt.button == 0)
        {
            if (_pendingPlacementValid)
            {
                PlacePendingNode(_pendingPlacementPosition);
            }
            evt.Use();
        }
        else if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape)
        {
            CancelPlacement();
            evt.Use();
        }
    }

    private void UpdatePlacementPreview(Event evt)
    {
        if (evt == null)
        {
            return;
        }

        if (evt.type != EventType.MouseMove && evt.type != EventType.Layout && evt.type != EventType.Repaint)
        {
            return;
        }

        var ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
        if (Physics.Raycast(ray, out var hit, 500f))
        {
            _pendingPlacementPosition = hit.point;
            _pendingPlacementValid = true;
        }
        else
        {
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out var distance))
            {
                _pendingPlacementPosition = ray.GetPoint(distance);
                _pendingPlacementValid = true;
            }
            else
            {
                _pendingPlacementValid = false;
            }
        }
    }

    private void DrawRouteNodes()
    {
        var nodes = _activeGraph.Nodes;
        if (nodes.Count == 0)
        {
            return;
        }

        PlayerStateSnapshot lastState = default;
        Vector3 lastPosition = nodes[0].WorldPosition;
        bool hasState = false;

        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            var color = node.ColorOverride.a > 0f
                ? node.ColorOverride
                : node.MoveProfile != null ? node.MoveProfile.DebugColor : _activeGraph.DefaultColor;

            Vector3 startPos;
            PlayerStateSnapshot startState;
            if (i == 0 || !hasState)
            {
                if (i == 0)
                {
                    startPos = node.WorldPosition;
                    startState = default;
                }
                else
                {
                    var prev = nodes[i - 1];
                    startPos = prev.WorldPosition;
                    startState = prev.ExpectedState;
                }
            }
            else
            {
                startPos = lastPosition;
                startState = lastState;
            }

            bool hasEvaluation = false;
            MoveEvaluation evaluation = default;
            string error = string.Empty;

            if (node.MoveProfile != null)
            {
                hasEvaluation = node.MoveProfile.TryEvaluate(startPos, node.WorldPosition, startState, out evaluation, out error);
            }
            else
            {
                error = "No move profile assigned.";
            }

            var size = HandleUtility.GetHandleSize(node.WorldPosition) * 0.1f;
            Handles.color = color;

            if (_selectedNodeIndex == i)
            {
                EditorGUI.BeginChangeCheck();
                var newPosition = Handles.PositionHandle(node.WorldPosition, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RegisterCompleteObjectUndo(_activeGraph, "Move Route Gizmo");
                    _graphSerialized.Update();
                    var nodeProperty = _nodesProperty.GetArrayElementAtIndex(i);
                    nodeProperty.FindPropertyRelative("_worldPosition").vector3Value = newPosition;
                    _graphSerialized.ApplyModifiedProperties();
                    EditorUtility.SetDirty(_activeGraph);
                    UpdateNodeEntries();
                    UpdateValidationMessages();
                    Repaint();
                }
            }
            else
            {
                if (Handles.Button(node.WorldPosition, Quaternion.identity, size, size, Handles.SphereHandleCap))
                {
                    SelectNode(i);
                }
            }

            Handles.SphereHandleCap(0, node.WorldPosition, Quaternion.identity, size, EventType.Repaint);
            Handles.Label(node.WorldPosition + Vector3.up * size * 2f, node.Label);

            if (hasEvaluation && evaluation.Trajectory != null && evaluation.Trajectory.Count > 1)
            {
                Handles.DrawAAPolyLine(4f, evaluation.Trajectory.ToArray());
                if (evaluation.CollisionIndex.HasValue && evaluation.CollisionIndex.Value >= 0 &&
                    evaluation.CollisionIndex.Value < evaluation.Trajectory.Count)
                {
                    Handles.color = Color.red;
                    var collisionPoint = evaluation.Trajectory[evaluation.CollisionIndex.Value];
                    Handles.SphereHandleCap(0, collisionPoint, Quaternion.identity, size * 1.5f, EventType.Repaint);
                }

                lastState = evaluation.EndState;
                lastPosition = node.WorldPosition;
                hasState = true;
            }
            else
            {
                Handles.color = Color.yellow;
                Handles.DrawDottedLine(startPos, node.WorldPosition, 4f);
                if (!string.IsNullOrEmpty(error))
                {
                    Handles.Label(node.WorldPosition + Vector3.up * size * 3f, error);
                }
                hasState = false;
            }
        }
    }

    private static Vector3 GetTrajectoryEnd(MoveEvaluation evaluation, Vector3 fallback)
    {
        if (evaluation.Trajectory != null && evaluation.Trajectory.Count > 0)
        {
            return evaluation.Trajectory[evaluation.Trajectory.Count - 1];
        }

        return fallback;
    }

    private struct NodeEntry
    {
        public int Index;
        public string DisplayName;
        public Color Color;
        public MoveProfile Profile;
    }
}
