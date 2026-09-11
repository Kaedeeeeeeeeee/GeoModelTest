(function (root) {
  'use strict';

  // Keep the existing CSS letterbox and input mapping, but decouple GPU pixels from DPI.
  // A fixed internal aspect ratio also avoids reallocations on every browser resize.
  function install(canvas, config) {
    config.matchWebGLToCanvasSize = false;
    config.devicePixelRatio = 1;
    var state = {
      logicalProcessors: Number(root.navigator.hardwareConcurrency) || 0,
      memoryMb: (Number(root.navigator.deviceMemory) || 0) * 1024,
      quality: 1,
      manual: false,
      setResolution: function (width, height) {
        if (!Number.isFinite(width) || !Number.isFinite(height) || width <= 0 || height <= 0) return;
        // All Web profiles have a 720p ceiling; Very Low uses 540p.
        var targetWidth = Math.max(320, Math.min(1280, Math.round(width / 16) * 16));
        var targetHeight = targetWidth * 9 / 16;
        if (canvas.width !== targetWidth) canvas.width = targetWidth;
        if (canvas.height !== targetHeight) canvas.height = targetHeight;
        state.width = targetWidth;
        state.height = targetHeight;
      }
    };
    root.GeoModelPerformance = state;
    state.setResolution(1280, 720);
    return state;
  }

  root.GeoModelPerformanceInstaller = { install: install };
})(window);
