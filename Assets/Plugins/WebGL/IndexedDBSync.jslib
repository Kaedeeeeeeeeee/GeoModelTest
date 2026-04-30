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
  }
});
