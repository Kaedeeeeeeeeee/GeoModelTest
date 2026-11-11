using System;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Backend
{
    /// <summary>
    /// 生徒の同意: the WebGL page asks the student before the game (StudentConsent.jslib).
    /// The title screen keeps New Game locked until the page is done. Nothing is stored or sent.
    /// </summary>
    public static class StudentConsentPage
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int GeoModelStudentConsent_GetState();
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

        internal static void ResetForTests() => StateOverride = null;
    }
}
