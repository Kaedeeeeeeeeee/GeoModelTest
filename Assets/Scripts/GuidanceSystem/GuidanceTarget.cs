using UnityEngine;

namespace GuidanceSystem
{
    /// <summary>
    /// Scene anchor that can be selected as a guidance destination.
    /// </summary>
    [DisallowMultipleComponent]
    public class GuidanceTarget : MonoBehaviour
    {
        [SerializeField] private string targetId = "default.target";
        [SerializeField] private float verticalOffset = 0.1f;
        [SerializeField] private float detectionRadius = 4f;

        public string TargetId => targetId;
        public float DetectionRadius => detectionRadius;
        public Vector3 WorldPosition => transform.position;

        private void OnEnable()
        {
            GuidanceManager.Instance?.RegisterTarget(this);
        }

        private void OnDisable()
        {
            if (GuidanceManager.Current != null)
            {
                GuidanceManager.Current.UnregisterTarget(this);
            }
        }

        public Vector3 GetAnchorPosition(float additionalOffset)
        {
            return transform.position + Vector3.up * (verticalOffset + additionalOffset);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.5f);
            Gizmos.DrawSphere(transform.position + Vector3.up * verticalOffset, 0.25f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * verticalOffset);
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
