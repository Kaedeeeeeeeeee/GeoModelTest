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
  }
});
