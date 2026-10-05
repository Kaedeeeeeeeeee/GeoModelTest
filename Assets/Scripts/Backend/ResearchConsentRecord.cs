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
    /// 保護者同意と本人同意（どちらも WebGL ページで入力。ページがない環境ではタイトル画面で本人同意）をまとめ、
    /// コード不要の参加登録と一緒にサーバーへ送る。記録するかどうかはサーバーが判断する。
    /// </summary>
    public static class ResearchConsentRecord
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern string GeoModelGuardianConsent_GetPayload();
        [DllImport("__Internal")] private static extern void GeoModelGuardianConsent_Reset();
        [DllImport("__Internal")] private static extern int GeoModelGuardianConsent_GetState();
#endif

        /// <summary>The consent page shown before Unity starts (WebGL template).</summary>
        public enum PageState
        {
            /// <summary>No page flow (editor, other hosts): the title screen asks the student.</summary>
            None = 0,
            /// <summary>The guardian or the student is still answering on the page.</summary>
            Pending = 1,
            /// <summary>The guardian agreed and the student confirmed on the page.</summary>
            Complete = 2
        }

        /// <summary>Tests replace the page state. Null uses the WebGL page (None elsewhere).</summary>
        internal static Func<PageState> PageStateOverride;

        public static PageState ConsentPageState
        {
            get
            {
                if (PageStateOverride != null) return PageStateOverride();
#if UNITY_WEBGL && !UNITY_EDITOR
                try { return (PageState)GeoModelGuardianConsent_GetState(); }
                catch (Exception) { return PageState.None; }
#else
                return PageState.None;
#endif
            }
        }

        /// <summary>Tests replace the page bridge. Null uses the WebGL page (empty elsewhere).</summary>
        internal static Func<string> GuardianPayloadOverride;

        public static DateTime? StudentAssentedAtUtc { get; private set; }

        /// <summary>Called when the student confirms all three statements on the title screen.</summary>
        public static void RecordStudentAssent() => StudentAssentedAtUtc = DateTime.UtcNow;

        /// <summary>
        /// Guardian form plus student assent for the participation request, or null when either is missing.
        /// The page includes the student's confirmation; the title screen records it where there is no page.
        /// </summary>
        public static JObject BuildOpenPlayConsent()
        {
            JObject guardian = ReadGuardianAnswer();
            if (guardian == null || guardian.Value<bool?>("guardianAgreed") != true) return null;
            if (guardian.Value<bool?>("studentAssented") == true &&
                !string.IsNullOrEmpty(guardian.Value<string>("studentAssentedAt"))) return guardian;
            if (!StudentAssentedAtUtc.HasValue) return null;
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
            PageStateOverride = null;
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
