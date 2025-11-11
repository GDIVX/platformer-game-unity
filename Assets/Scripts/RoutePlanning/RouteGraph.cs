using System;
using System.Collections.Generic;
using RoutePlanning.Profiles;
using Runtime.Player.Movement;
using Runtime.Player.Movement.Math;
using UnityEngine;

namespace RoutePlanning
{
    [CreateAssetMenu(menuName = "Route Planning/Route Graph", fileName = "RouteGraph")]
    public class RouteGraph : ScriptableObject
    {
        [SerializeField] private string _author;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Color _defaultColor = new Color(0.3f, 0.7f, 1f, 0.8f);
        [SerializeField] private List<string> _tags = new List<string>();
        [SerializeField] private List<RouteNode> _nodes = new List<RouteNode>();

        public string Author => _author;
        public string Description => _description;
        public Color DefaultColor => _defaultColor;
        public IReadOnlyList<string> Tags => _tags;
        public IReadOnlyList<RouteNode> Nodes => _nodes;

        public bool TryGetNode(int index, out RouteNode node)
        {
            if (index < 0 || index >= _nodes.Count)
            {
                node = default;
                return false;
            }

            node = _nodes[index];
            return true;
        }

        public bool TryValidate(out List<string> errors)
        {
            errors = new List<string>();
            if (_nodes.Count == 0)
            {
                return true;
            }

            bool isValid = true;

            var firstNode = _nodes[0];
            Vector3 currentPosition = firstNode.WorldPosition;
            PlayerStateSnapshot currentState = firstNode.ExpectedState;

            ValidateNodeBranches(firstNode, currentPosition, currentState, errors, ref isValid);

            for (int i = 1; i < _nodes.Count; i++)
            {
                var node = _nodes[i];
                if (!node.TryEvaluate(currentPosition, currentState, out var evaluation, out string error))
                {
                    errors.Add($"Node {i} ({node.Label}) failed: {error}");
                    isValid = false;
                    continue;
                }

                if (!node.ExpectedState.ApproximatelyEquals(evaluation.EndState))
                {
                    errors.Add($"Node {i} ({node.Label}) state mismatch. Expected {node.ExpectedState} but got {evaluation.EndState}.");
                    isValid = false;
                }

                currentPosition = node.WorldPosition;
                currentState = evaluation.EndState;

                ValidateNodeBranches(node, currentPosition, currentState, errors, ref isValid);
            }

            return isValid;
        }

        private void ValidateNodeBranches(RouteNode node, Vector3 nodePosition, PlayerStateSnapshot nodeState,
            List<string> errors, ref bool isValid)
        {
            if (!node.HasBranches)
            {
                return;
            }

            foreach (var branch in node.Branches)
            {
                if (!TryGetNode(branch.TargetNodeIndex, out var target))
                {
                    errors.Add($"Node '{node.Label}' has an invalid branch target index {branch.TargetNodeIndex}.");
                    isValid = false;
                    continue;
                }

                if (!branch.TryValidate(node, target, nodePosition, nodeState, out string branchError))
                {
                    errors.Add($"Node '{node.Label}' branch to '{target.Label}' failed: {branchError}");
                    isValid = false;
                }
            }
        }
    }

    [Serializable]
    public struct RouteNode
    {
        [SerializeField] private string _label;
        [SerializeField] private Vector3 _worldPosition;
        [SerializeField] private MoveProfile _moveProfile;
        [SerializeField] private PlayerStateSnapshot _expectedState;
        [SerializeField] private Color _colorOverride;
        [SerializeField, TextArea] private string _designerNotes;
        [SerializeField] private bool _important;
        [SerializeField] private List<RouteBranch> _branches;

        public string Label => string.IsNullOrEmpty(_label) ? "Node" : _label;
        public Vector3 WorldPosition => _worldPosition;
        public MoveProfile MoveProfile => _moveProfile;
        public PlayerStateSnapshot ExpectedState => _expectedState;
        public Color ColorOverride => _colorOverride;
        public string DesignerNotes => _designerNotes;
        public bool Important => _important;
        public IReadOnlyList<RouteBranch> Branches => _branches ?? Array.Empty<RouteBranch>();
        public bool HasBranches => _branches != null && _branches.Count > 0;

        public bool TryEvaluate(Vector3 previousPosition, PlayerStateSnapshot previousState,
            out MoveEvaluation evaluation, out string error)
        {
            if (_moveProfile == null)
            {
                var fallbackTrajectory = MovementMathUtility.CreateLinearTrajectory(previousPosition, _worldPosition, 2);
                evaluation = new MoveEvaluation(previousState, fallbackTrajectory, 0f);
                if (!ExpectedState.ApproximatelyEquals(previousState))
                {
                    error = "No move profile assigned and expected state does not match previous state.";
                    return false;
                }

                error = string.Empty;
                return true;
            }

            return _moveProfile.TryEvaluate(previousPosition, _worldPosition, previousState, out evaluation, out error);
        }
    }

    [Serializable]
    public struct RouteBranch
    {
        [SerializeField] private int _targetNodeIndex;
        [SerializeField] private MoveProfile _profileOverride;
        [SerializeField] private PlayerStateSnapshot _expectedState;
        [SerializeField, TextArea] private string _conditionDescription;
        [SerializeField] private Color _colorTint;

        public int TargetNodeIndex => _targetNodeIndex;
        public MoveProfile ProfileOverride => _profileOverride;
        public PlayerStateSnapshot ExpectedState => _expectedState;
        public string ConditionDescription => _conditionDescription;
        public Color ColorTint => _colorTint;

        public bool TryValidate(RouteNode origin, RouteNode target, Vector3 originPosition,
            PlayerStateSnapshot originState, out string error)
        {
            var profile = _profileOverride != null ? _profileOverride : origin.MoveProfile;
            if (profile == null)
            {
                error = "No move profile available for branch transition.";
                return false;
            }

            if (!profile.TryEvaluate(originPosition, target.WorldPosition, originState, out var evaluation, out error))
            {
                return false;
            }

            if (!ExpectedState.ApproximatelyEquals(evaluation.EndState))
            {
                error = $"Expected state {ExpectedState} does not match evaluated state {evaluation.EndState}.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
