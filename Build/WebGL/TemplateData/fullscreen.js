(function () {
  'use strict';

  window.GeoModelFullscreen = {
    install: function () {
      var touchDevice = (window.matchMedia && window.matchMedia('(pointer: coarse)').matches) ||
        /Android|iPhone|iPad|iPod/.test(navigator.userAgent) ||
        (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
      if (!touchDevice) return;

      // Fullscreen the document, not just the canvas: keep safe areas, loading UI
      // and the portrait orientation overlay in the same fullscreen subtree.
      var root = document.documentElement;
      var helper = document.getElementById('fullscreen-help');
      var message = document.getElementById('fullscreen-message');
      var retry = document.getElementById('fullscreen-retry');
      var dismiss = document.getElementById('fullscreen-dismiss');
      var attempted = false;
      var pending = false;
      var entered = !!fullscreenElement();
      var hideTimer;
      var requestTimer;

      function fullscreenElement() {
        return document.fullscreenElement || document.webkitFullscreenElement;
      }

      function standalone() {
        return navigator.standalone === true || (window.matchMedia &&
          (window.matchMedia('(display-mode: standalone)').matches ||
           window.matchMedia('(display-mode: fullscreen)').matches));
      }

      function requestMethod() {
        if (root.requestFullscreen && document.fullscreenEnabled !== false) return root.requestFullscreen;
        if (root.webkitRequestFullscreen && document.webkitFullscreenEnabled !== false) return root.webkitRequestFullscreen;
        return null;
      }

      function hideHelper() {
        clearTimeout(hideTimer);
        helper.hidden = true;
      }

      function showHelper(text, canRetry) {
        clearTimeout(hideTimer);
        message.textContent = text;
        message.hidden = !text;
        retry.hidden = !canRetry;
        helper.hidden = false;
        // Keep controls discoverable after failure/exit without covering gameplay
        // indefinitely. The browser/itch.io fullscreen control remains available.
        hideTimer = setTimeout(hideHelper, 10000);
      }

      function failed() {
        if (!pending) return;
        pending = false;
        clearTimeout(requestTimer);
        showHelper('全画面にできませんでした。もう一度お試しください。', !!requestMethod());
      }

      function changed() {
        if (fullscreenElement()) {
          entered = true;
          attempted = true;
          pending = false;
          clearTimeout(requestTimer);
          hideHelper();
        } else if (entered) {
          entered = false;
          pending = false;
          attempted = true;
          clearTimeout(requestTimer);
          // An explicit exit (including iPad's swipe gesture) must stay exited.
          // Only this optional button can request fullscreen again.
          showHelper('', !!requestMethod());
        }
      }

      function request() {
        if (pending) return;
        attempted = true;
        if (fullscreenElement() || standalone()) return;
        var method = requestMethod();
        if (!method) {
          var iPhoneSafari = /iPhone/.test(navigator.userAgent) &&
            /Safari/.test(navigator.userAgent) && !/CriOS|FxiOS|EdgiOS|OPiOS/.test(navigator.userAgent);
          var unsupportedSafari = iPhoneSafari && !root.requestFullscreen && !root.webkitRequestFullscreen;
          showHelper(unsupportedSafari ?
            'Safariの「ぁあ」→「ツールバーを非表示」で、画面を広くできます。' :
            'この画面では全画面にできません。横向きでお楽しみください。', false);
          return;
        }
        pending = true;
        hideHelper();
        // Call synchronously from pointerup/touchend/click. Touch pointerdown is
        // too early for transient user activation, and a delayed call can lose it.
        try {
          var result = method === root.requestFullscreen ?
            method.call(root, { navigationUI: 'hide' }) : method.call(root);
          if (result && typeof result.then === 'function') result.then(changed, failed);
          if (pending) requestTimer = setTimeout(failed, 2000);
        } catch (error) {
          failed();
        }
      }

      function firstGesture(event) {
        if (attempted || event.isTrusted !== true) return;
        if (!event.target || !event.target.closest('#unity-canvas, #orientation-overlay')) return;
        // Do not cancel or stop the original gameplay action.
        request();
      }

      document.addEventListener(window.PointerEvent ? 'pointerup' : 'touchend', firstGesture,
        { capture: true, passive: true });
      document.addEventListener('fullscreenchange', changed);
      document.addEventListener('webkitfullscreenchange', changed);
      document.addEventListener('fullscreenerror', failed);
      document.addEventListener('webkitfullscreenerror', failed);
      retry.addEventListener('click', request);
      dismiss.addEventListener('click', hideHelper);
    }
  };
}());
