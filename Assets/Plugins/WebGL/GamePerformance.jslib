mergeInto(LibraryManager.library, {
  GeoModel_IsPerformanceSampleEligible: function () {
    var overlay = document.getElementById('orientation-overlay');
    return !document.hidden && !(overlay && overlay.classList.contains('is-visible')) ? 1 : 0;
  },
  GeoModel_SetRenderResolution: function (width, height, quality, manual) {
    if (window.GeoModelPerformance) {
      window.GeoModelPerformance.setResolution(width, height);
      window.GeoModelPerformance.quality = quality;
      window.GeoModelPerformance.manual = manual !== 0;
    }
  }
});
