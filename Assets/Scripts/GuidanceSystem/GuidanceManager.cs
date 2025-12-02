using System.Collections.Generic;
using UnityEngine;

namespace GuidanceSystem
{
    /// <summary>
    /// Renders a curved guidance line between the player and a configured destination.
    /// </summary>
    public class GuidanceManager : MonoBehaviour
    {
        private static GuidanceManager _instance;
        public static GuidanceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("GuidanceManager");
                    _instance = go.AddComponent<GuidanceManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public static GuidanceManager Current => _instance;

        [Header("Line Appearance")]
        [SerializeField] private Gradient lineGradient;
        [SerializeField] private AnimationCurve widthCurve = AnimationCurve.Linear(0f, 0.15f, 1f, 0.15f);
        [SerializeField] private float lineHeightOffset = 0.1f;
        [SerializeField] private float arcHeight = 0.73f;
        [SerializeField, Range(4, 64)] private int segmentCount = 32;

        private readonly Dictionary<string, GuidanceTarget> registeredTargets = new Dictionary<string, GuidanceTarget>();
        private LineRenderer lineRenderer;
        private Transform playerTransform;
        private GuidanceTarget activeTarget;
        private string pendingTargetId;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeLineRenderer();
        }

        private void InitializeLineRenderer()
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.enabled = false;
            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.positionCount = segmentCount;
            lineRenderer.useWorldSpace = true;
            lineRenderer.numCornerVertices = 6;
            lineRenderer.numCapVertices = 6;
            lineRenderer.widthCurve = widthCurve;
            lineRenderer.textureMode = LineTextureMode.Stretch;
            if (lineGradient == null || lineGradient.colorKeys.Length == 0)
            {
                lineGradient = new Gradient
                {
                    colorKeys = new[]
                    {
                        new GradientColorKey(new Color(0.3f, 0.8f, 1f), 0f),
                        new GradientColorKey(new Color(0.2f, 0.4f, 1f), 1f)
                    },
                    alphaKeys = new[]
                    {
                        new GradientAlphaKey(0.85f, 0f),
                        new GradientAlphaKey(0.95f, 1f)
                    }
                };
            }
            lineRenderer.colorGradient = lineGradient;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        private void Update()
        {
            if (lineRenderer == null) return;

            if (playerTransform == null || activeTarget == null)
            {
                lineRenderer.enabled = false;
                return;
            }

            UpdateLinePositions();
        }

        private void UpdateLinePositions()
        {
            Vector3 start = playerTransform.position + Vector3.up * lineHeightOffset;
            Vector3 end = activeTarget.GetAnchorPosition(lineHeightOffset);
            Vector3 control = (start + end) * 0.5f + Vector3.up * arcHeight;

            if (lineRenderer.positionCount != segmentCount)
            {
                lineRenderer.positionCount = segmentCount;
            }

            for (int i = 0; i < segmentCount; i++)
            {
                float t = i / (segmentCount - 1f);
                // Quadratic Bézier interpolation
                Vector3 point = Mathf.Pow(1 - t, 2) * start +
                                2 * (1 - t) * t * control +
                                Mathf.Pow(t, 2) * end;
                lineRenderer.SetPosition(i, point);
            }

            lineRenderer.enabled = true;
        }

        public void RegisterPlayer(Transform player)
        {
            playerTransform = player;
        }

        public void RegisterTarget(GuidanceTarget target)
        {
            if (target == null || string.IsNullOrEmpty(target.TargetId)) return;
            registeredTargets[target.TargetId] = target;

            if (!string.IsNullOrEmpty(pendingTargetId) && pendingTargetId == target.TargetId)
            {
                ActivateTarget(pendingTargetId);
            }
        }

        public void UnregisterTarget(GuidanceTarget target)
        {
            if (target == null || string.IsNullOrEmpty(target.TargetId)) return;
            if (registeredTargets.TryGetValue(target.TargetId, out var t) && t == target)
            {
                registeredTargets.Remove(target.TargetId);
                if (activeTarget == target)
                {
                    ClearTarget();
                }
            }
        }

        public void ActivateTarget(string targetId)
        {
            if (string.IsNullOrEmpty(targetId))
            {
                ClearTarget();
                return;
            }

            if (registeredTargets.TryGetValue(targetId, out var target))
            {
                activeTarget = target;
                pendingTargetId = null;
                lineRenderer.enabled = true;
            }
            else
            {
                pendingTargetId = targetId;
                activeTarget = null;
                lineRenderer.enabled = false;
            }
        }

        public void ClearTarget()
        {
            activeTarget = null;
            pendingTargetId = null;
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }

        public bool TryGetTarget(string targetId, out GuidanceTarget target)
        {
            return registeredTargets.TryGetValue(targetId, out target);
        }

        public bool TryGetTargetPosition(string targetId, out Vector3 position)
        {
            if (registeredTargets.TryGetValue(targetId, out var target))
            {
                position = target.WorldPosition;
                return true;
            }

            position = default;
            return false;
        }
    }
}
