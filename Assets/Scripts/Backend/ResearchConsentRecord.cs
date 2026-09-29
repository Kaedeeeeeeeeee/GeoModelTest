using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Backend
{
    /// <summary>
    /// 保護者同意（WebGL ページで入力）と本人同意（タイトル画面）をまとめ、
    /// コード不要の参加登録と一緒にサーバーへ送る。記録するかどうかはサーバーが判断する。
    /// </summary>
    public static class ResearchConsentRecord
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern string GeoModelGuardianConsent_GetPayload();
        [DllImport("__Internal")] private static extern void GeoModelGuardianConsent_Reset();
#endif

        /// <summary>Tests replace the page bridge. Null uses the WebGL page (empty elsewhere).</summary>
        internal static Func<string> GuardianPayloadOverride;

        public static DateTime? StudentAssentedAtUtc { get; private set; }

        /// <summary>Called when the student confirms all three statements on the title screen.</summary>
        public static void RecordStudentAssent() => StudentAssentedAtUtc = DateTime.UtcNow;

        /// <summary>
        /// Guardian form plus student assent for the participation request, or null when either is missing.
        /// </summary>
        public static JObject BuildOpenPlayConsent()
        {
            if (!StudentAssentedAtUtc.HasValue) return null;
            JObject guardian = ReadGuardianAnswer();
            if (guardian == null || guardian.Value<bool?>("guardianAgreed") != true) return null;
            guardian["studentAssented"] = true;
            guardian["studentAssentedAt"] = StudentAssentedAtUtc.Value.ToString("o", CultureInfo.InvariantCulture);
            return guardian;
        }

        /// <summary>Makes the page ask the guardian again after the server rejected the stored answer.</summary>
        public static void DiscardGuardianAnswer()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (GuardianPayloadOverride == null) GeoModelGuardianConsent_Reset();
#endif
        }

        internal static void ResetForTests()
        {
            StudentAssentedAtUtc = null;
            GuardianPayloadOverride = null;
        }

        private static JObject ReadGuardianAnswer()
        {
            string json = GuardianPayloadOverride != null ? GuardianPayloadOverride() : ReadPage();
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                // Keep timestamps as the page wrote them instead of re-formatting them as DateTime.
                using var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None };
                return JObject.Load(reader);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string ReadPage()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return GeoModelGuardianConsent_GetPayload();
#else
            return null;
#endif
        }
    }
}
