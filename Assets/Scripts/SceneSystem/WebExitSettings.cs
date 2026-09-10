using UnityEngine;

namespace SceneSystem
{
    [CreateAssetMenu(menuName = "GeoModel/Web Exit Settings")]
    public sealed class WebExitSettings : ScriptableObject
    {
        [SerializeField] private string _gamePageUrl;
        public string GamePageUrl => _gamePageUrl;
    }
}
