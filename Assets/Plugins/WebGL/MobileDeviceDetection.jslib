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
  },

  // Fullscreen for the title-screen 「全画面にする」 button (2026-11-26). Browsers only enter fullscreen
  // inside a user gesture, so the button arms a one-shot listener on press and the browser's own
  // pointerup/touchend runs the request; the click callback tries again within the gesture window.
  // iPhone has no page fullscreen and iPadOS always leaves it on a downward swipe (2026-09-29 report),
  // so Apple touch devices keep the Safari toolbar guide instead.
  $GeoModelFullscreen: {
    armed: null,
    pending: false,
    isActive: function () {
      return !!(document.fullscreenElement || document.webkitFullscreenElement);
    },
    request: function () {
      try {
        if (GeoModelFullscreen.pending || GeoModelFullscreen.isActive()) return;
        var el = document.documentElement;
        var result = el.requestFullscreen ? el.requestFullscreen({ navigationUI: "hide" })
          : el.webkitRequestFullscreen ? el.webkitRequestFullscreen() : null;
        var lock = function () {
          // Android Chrome can keep the game landscape while it is fullscreen; elsewhere this rejects.
          try {
            if (window.matchMedia("(pointer: coarse)").matches && screen.orientation && screen.orientation.lock)
              screen.orientation.lock("landscape").catch(function () {});
          } catch (e) {}
        };
        if (result && result.then) {
          // The armed release and the click callback both ask; only the first one goes to the browser.
          GeoModelFullscreen.pending = true;
          result.then(function () { GeoModelFullscreen.pending = false; lock(); },
            function (e) { GeoModelFullscreen.pending = false; console.warn("[Fullscreen] request refused:", e); });
        } else lock();
      } catch (e) {
        console.warn("[Fullscreen] request failed:", e);
      }
    },
    disarm: function () {
      var armed = GeoModelFullscreen.armed;
      if (!armed) return;
      GeoModelFullscreen.armed = null;
      clearTimeout(armed.timer);
      ["pointerup", "touchend", "mouseup", "keyup"].forEach(function (type) {
        document.removeEventListener(type, armed.handler, true);
      });
    }
  },

  // 0 = do not offer fullscreen (Apple touch device, no API, iframe without permission, already fullscreen
  // or a home-screen / F11 window); 1 = offer it with a mouse/keyboard; 2 = offer it on a touch screen.
  GeoModelTest_FullscreenAvailability__deps: ["$GeoModelFullscreen"],
  GeoModelTest_FullscreenAvailability: function () {
    try {
      var ua = navigator.userAgent || "";
      var appleTouch = /iPhone|iPod|iPad/.test(ua) || (navigator.platform === "MacIntel" && (navigator.maxTouchPoints || 0) > 1);
      if (appleTouch) return 0;
      var el = document.documentElement;
      if (!(document.fullscreenEnabled || document.webkitFullscreenEnabled)) return 0;
      if (!(el.requestFullscreen || el.webkitRequestFullscreen)) return 0;
      if (GeoModelFullscreen.isActive()) return 0;
      var mm = window.matchMedia ? function (q) { return window.matchMedia(q).matches; } : function () { return false; };
      if (mm("(display-mode: fullscreen)") || mm("(display-mode: standalone)")) return 0;
      if (window.innerWidth >= screen.width - 1 && window.innerHeight >= screen.height - 1) return 0;
      return mm("(pointer: coarse)") ? 2 : 1;
    } catch (e) {
      console.warn("[Fullscreen] availability check failed:", e);
      return 0;
    }
  },

  GeoModelTest_ArmFullscreen__deps: ["$GeoModelFullscreen"],
  GeoModelTest_ArmFullscreen: function () {
    try {
      GeoModelFullscreen.disarm();
      var armed = {};
      armed.handler = function () {
        GeoModelFullscreen.disarm();
        GeoModelFullscreen.request();
      };
      // A press that never becomes a release (dragged away, cancelled) must not fullscreen a later tap.
      armed.timer = setTimeout(GeoModelFullscreen.disarm, 3000);
      GeoModelFullscreen.armed = armed;
      ["pointerup", "touchend", "mouseup", "keyup"].forEach(function (type) {
        document.addEventListener(type, armed.handler, true);
      });
    } catch (e) {
      console.warn("[Fullscreen] arm failed:", e);
    }
  },

  GeoModelTest_RequestFullscreen__deps: ["$GeoModelFullscreen"],
  GeoModelTest_RequestFullscreen: function () {
    GeoModelFullscreen.disarm();
    GeoModelFullscreen.request();
  }
});
