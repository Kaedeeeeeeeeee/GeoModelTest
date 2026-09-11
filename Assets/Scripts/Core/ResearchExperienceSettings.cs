using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "GeoModel/Research Experience Settings")]
    public sealed class ResearchExperienceSettings : ScriptableObject
    {
        [SerializeField] private bool _warehouseInteractionEnabled;

        // Storage remains available to persistence; only player-facing access is gated.
        public static bool WarehouseInteractionEnabled
        {
            get
            {
                var settings = Resources.Load<ResearchExperienceSettings>("ResearchExperienceSettings");
                return settings != null && settings._warehouseInteractionEnabled;
            }
        }
    }
}
