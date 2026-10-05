// Bridge to TemplateData/guardian-consent.js: the 保護者同意 answered on the page before Unity started.
mergeInto(LibraryManager.library, {
  // 0 = no consent page (another host), 1 = the guardian or student is still answering, 2 = both done.
  GeoModelGuardianConsent_GetState: function () {
    try {
      var api = window.GeoModelGuardianConsent;
      if (!api) return 0;
      return api.isPending() ? 1 : 2;
    } catch (e) {
      console.warn("[GuardianConsent] could not read the consent state:", e);
      return 0;
    }
  },

  GeoModelGuardianConsent_GetPayload: function () {
    var json = "";
    try {
      if (window.GeoModelGuardianConsent) json = window.GeoModelGuardianConsent.payloadJson() || "";
    } catch (e) {
      console.warn("[GuardianConsent] could not read the guardian answer:", e);
    }
    var size = lengthBytesUTF8(json) + 1;
    var buffer = _malloc(size);
    stringToUTF8(json, buffer, size);
    return buffer;
  },

  // After the server rejects the stored answer, the next page load asks the guardian again.
  GeoModelGuardianConsent_Reset: function () {
    try {
      if (window.GeoModelGuardianConsent) window.GeoModelGuardianConsent.reset();
    } catch (e) {
      console.warn("[GuardianConsent] could not clear the guardian answer:", e);
    }
  }
});
