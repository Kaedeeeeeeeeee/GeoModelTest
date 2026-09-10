using System;
using UnityEngine;

namespace Backend
{
    [CreateAssetMenu(fileName = "SurveySettings", menuName = "GeoModel/Survey Settings")]
    public sealed class SurveySettings : ScriptableObject
    {
        [SerializeField] private string _pageUrl = "";
        public string PageUrl
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_pageUrl)) return _pageUrl.Trim();
                string path = Application.streamingAssetsPath.TrimEnd('/') + "/Survey/index.html";
                return Uri.TryCreate(path, UriKind.Absolute, out var uri) && (uri.Scheme == "https" || uri.Scheme == "http")
                    ? path : new Uri(path).AbsoluteUri;
            }
        }
    }
}
