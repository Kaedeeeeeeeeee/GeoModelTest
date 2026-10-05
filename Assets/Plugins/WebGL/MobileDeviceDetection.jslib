mergeInto(LibraryManager.library, {
  GeoModelTest_IsMobileBrowser: function () {
    try {
      var userAgent = navigator.userAgent || navigator.vendor || "";
      var platform = navigator.platform || "";
      var maxTouchPoints = navigator.maxTouchPoints || 0;
      var mobileUserAgent = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini|Mobile|Tablet/i.test(userAgent);
      var iPadDesktopUserAgent = platform === "MacIntel" && maxTouchPoints > 1;

      return (mobileUserAgent || iPadDesktopUserAgent) ? 1 : 0;
    } catch (e) {
      console.warn("[MobileDeviceDetection] mobile browser detection failed:", e);
      return 0;
    }
  },

  // Safari on iPhone/iPad can hide its toolbars from the page menu; other browsers there cannot.
  // Returns device * 1000 + Safari major version (1 = iPhone, 2 = iPad), or 0 when not applicable
  // (another browser, an in-app web view, a home-screen web app, or not an Apple touch device).
  GeoModelTest_SafariToolbarDevice: function () {
    try {
      var ua = navigator.userAgent || "";
      var iPadDesktop = navigator.platform === "MacIntel" && (navigator.maxTouchPoints || 0) > 1;
      var device = /iPhone|iPod/.test(ua) ? 1 : (/iPad/.test(ua) || iPadDesktop) ? 2 : 0;
      if (!device || navigator.standalone === true) return 0;
      var version = /Version\/(\d+)/.exec(ua);
      var safari = /Safari\//.test(ua) && version &&
        !/CriOS|FxiOS|EdgiOS|OPiOS|GSA|YaBrowser|DuckDuckGo|Line\/|FBAN|FBAV|Instagram/.test(ua);
      return safari ? device * 1000 + Math.min(999, parseInt(version[1], 10) || 0) : 0;
    } catch (e) {
      console.warn("[MobileDeviceDetection] Safari detection failed:", e);
      return 0;
    }
  }
});
