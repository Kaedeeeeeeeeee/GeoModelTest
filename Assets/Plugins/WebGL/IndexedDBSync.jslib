mergeInto(LibraryManager.library, {
  // 把 emscripten IDBFS 内存文件系统同步到 IndexedDB（持久层）。
  // populate=false: 内存→IDB（保存）。每次关键写入后调用，避免页面刷新时丢档。
  GeoModelTest_SyncFsToIDB: function () {
    try {
      if (window.geoModelSyncStatus === 1) {
        window.geoModelSyncAgain = true;
        return;
      }
      window.geoModelSyncStatus = 1;
      if (typeof FS !== 'undefined' && FS.syncfs) {
        var synchronize = function () { FS.syncfs(false, function (err) {
          if (!err && window.geoModelSyncAgain) {
            window.geoModelSyncAgain = false;
            synchronize();
            return;
          }
          window.geoModelSyncStatus = err ? -1 : 2;
          if (err) {
            console.warn('[WebGLFileSync] syncfs failed:', err);
          }
        }); };
        synchronize();
      } else window.geoModelSyncStatus = -1;
    } catch (e) {
      window.geoModelSyncStatus = -1;
      console.warn('[WebGLFileSync] syncfs threw:', e);
    }
  },

  GeoModelTest_SyncStatus: function () { return window.geoModelSyncStatus || 0; },

  GeoModelTest_OpenSurvey: function (urlPtr, labelPtr) {
    var target;
    try {
      target = new URL(UTF8ToString(urlPtr), window.location.href);
      if (target.protocol !== 'https:' && !(target.protocol === 'http:' && target.hostname === '127.0.0.1')) return;
    } catch (_) { return; }
    // Async saving may consume user activation. Keep a real link for sandboxed embeds.
    var overlay = document.createElement('div');
    overlay.style.cssText = 'position:fixed;inset:0;z-index:2147483647;display:grid;place-items:center;background:#10232c;font:24px sans-serif';
    var link = document.createElement('a');
    link.href = target.href; link.target = '_top'; link.rel = 'noopener noreferrer';
    link.textContent = UTF8ToString(labelPtr);
    link.style.cssText = 'color:#10232c;background:#61d4b3;padding:24px 48px;border-radius:8px';
    overlay.appendChild(link); document.body.appendChild(overlay);
    if (document.fullscreenElement && document.exitFullscreen) document.exitFullscreen().catch(function () {});
    try { window.top.location.href = target.href; } catch (_) {}
  },

  GeoModelTest_ReturnToGamePage: function (urlPtr, labelPtr) {
    var target = UTF8ToString(urlPtr);
    try {
      var configured = target ? new URL(target) : null;
      if (!configured || configured.protocol !== 'https:') {
        var referrer = new URL(document.referrer);
        target = (referrer.hostname === 'itch.io' || referrer.hostname.endsWith('.itch.io'))
          ? referrer.href : 'https://itch.io';
      }
    } catch (_) { target = 'https://itch.io'; }
    // A real user click remains available when iframe navigation or fullscreen is restricted.
    var overlay = document.createElement('div');
    overlay.style.cssText = 'position:fixed;inset:0;z-index:2147483647;display:grid;place-items:center;background:#10232c;font:24px sans-serif';
    var link = document.createElement('a');
    link.href = target;
    link.target = '_top';
    link.textContent = UTF8ToString(labelPtr);
    link.style.cssText = 'color:#10232c;background:#61d4b3;padding:24px 48px;border-radius:8px';
    overlay.appendChild(link);
    document.body.appendChild(overlay);
    if (document.fullscreenElement && document.exitFullscreen) document.exitFullscreen().catch(function () {});
    try { window.top.location.href = target; } catch (_) {}
  },

  // 检查 URL 查询参数是否包含给定 flag（值为 "1"/"true" 视为开启）。
  // 用于 dev 测试：?resetstory=1 触发剧情进度重置。
  GeoModelTest_QueryUrlFlag: function (flagPtr) {
    try {
      var flag = UTF8ToString(flagPtr);
      var params = new URLSearchParams(window.location.search);
      var v = params.get(flag);
      return (v === '1' || v === 'true') ? 1 : 0;
    } catch (e) {
      console.warn('[WebGLStartupReset] QueryUrlFlag threw:', e);
      return 0;
    }
  }
});
