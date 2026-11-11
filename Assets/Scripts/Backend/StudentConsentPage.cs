using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Backend
{
    /// <summary>
    /// 生徒の同意: the WebGL page asks the student before the game (StudentConsent.jslib).
    /// The title screen keeps New Game locked until the page is done; New Game sends the assent with
    /// the code-free activation and the server stores it.
    /// </summary>
    public static class StudentConsentPage
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int GeoModelStudentConsent_GetState();
        [DllImport("__Internal")] private static extern string GeoModelStudentConsent_GetPayload();
#endif

        public enum PageState
        {
            /// <summary>No consent page (editor, other hosts): New Game is ready at once.</summary>
            None = 0,
            /// <summary>The student is still answering on the page.</summary>
            Pending = 1,
            /// <summary>The student confirmed all three statements on the page.</summary>
            Complete = 2
        }

        /// <summary>Tests replace the page state. Null uses the WebGL page (None elsewhere).</summary>
        internal static Func<PageState> StateOverride;

        public static PageState State
        {
            get
            {
                if (StateOverride != null) return StateOverride();
#if UNITY_WEBGL && !UNITY_EDITOR
                try { return (PageState)GeoModelStudentConsent_GetState(); }
                catch (Exception) { return PageState.None; }
#else
                return PageState.None;
#endif
            }
        }

        /// <summary>Tests replace the page answer. Null uses the WebGL page (empty elsewhere).</summary>
        internal static Func<string> PayloadOverride;

        /// <summary>The student's answer for the participation request, or null before the student confirmed.</summary>
        public static JObject BuildConsent()
        {
            string json = PayloadOverride != null ? PayloadOverride() : ReadPage();
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                // Keep the timestamp as the page wrote it instead of re-formatting it as DateTime.
                using var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None };
                var answer = JObject.Load(reader);
                return answer.Value<bool?>("studentAssented") == true &&
                       !string.IsNullOrEmpty(answer.Value<string>("studentAssentedAt")) ? answer : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static void ResetForTests()
        {
            StateOverride = null;
            PayloadOverride = null;
        }

        private static string ReadPage()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { return GeoModelStudentConsent_GetPayload(); }
            catch (Exception) { return null; }
#else
            return null;
#endif
        }
    }
}
