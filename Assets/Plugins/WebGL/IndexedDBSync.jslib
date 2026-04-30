mergeInto(LibraryManager.library, {
  // 把 emscripten IDBFS 内存文件系统同步到 IndexedDB（持久层）。
  // populate=false: 内存→IDB（保存）。每次关键写入后调用，避免页面刷新时丢档。
  GeoModelTest_SyncFsToIDB: function () {
    try {
      if (typeof FS !== 'undefined' && FS.syncfs) {
        FS.syncfs(false, function (err) {
          if (err) {
            console.warn('[WebGLFileSync] syncfs failed:', err);
          }
        });
      }
    } catch (e) {
      console.warn('[WebGLFileSync] syncfs threw:', e);
    }
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
