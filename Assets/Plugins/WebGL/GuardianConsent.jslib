// Bridge to TemplateData/guardian-consent.js: the 保護者同意 answered on the page before Unity started.
mergeInto(LibraryManager.library, {
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
