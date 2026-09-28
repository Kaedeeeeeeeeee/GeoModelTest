using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "GeoModel/Research Experience Settings")]
    public sealed class ResearchExperienceSettings : ScriptableObject
    {
        [SerializeField] private bool _warehouseInteractionEnabled;
        [SerializeField] private bool _encyclopediaEnabled;

        // Storage remains available to persistence; only player-facing access is gated.
        public static bool WarehouseInteractionEnabled
        {
            get
            {
                var settings = Resources.Load<ResearchExperienceSettings>("ResearchExperienceSettings");
                return settings != null && settings._warehouseInteractionEnabled;
            }
        }

        // Collection tracking keeps running; only the 図鑑 button, the O key and guide mentions are gated.
        // When re-enabling, restore the encyclopedia wording in the ui.guide.*menus texts.
        public static bool EncyclopediaEnabled
        {
            get
            {
                var settings = Resources.Load<ResearchExperienceSettings>("ResearchExperienceSettings");
                return settings != null && settings._encyclopediaEnabled;
            }
        }
    }
}
