// Bridge to TemplateData/student-consent.js: the 生徒の同意 answered on the page before Unity started.
mergeInto(LibraryManager.library, {
  // 0 = no consent page (another host), 1 = the student is still answering, 2 = done.
  GeoModelStudentConsent_GetState: function () {
    try {
      var api = window.GeoModelStudentConsent;
      if (!api) return 0;
      return api.isPending() ? 1 : 2;
    } catch (e) {
      console.warn("[StudentConsent] could not read the consent state:", e);
      return 0;
    }
  }
});
