/* Unity startup UI. Only watches build downloads; game/backend requests are untouched. */
(function () {
  'use strict';

  window.GeoModelLoading = {
    start: function (canvas, config, loaderUrl) {
      var panel = document.getElementById('unity-loading-bar');
      var title = document.getElementById('loading-title');
      var status = document.getElementById('loading-status');
      var help = document.getElementById('loading-help');
      var bar = document.getElementById('unity-progress-bar-empty');
      var fill = document.getElementById('unity-progress-bar-full');
      var percent = document.getElementById('loading-progress-text');
      var retry = document.getElementById('loading-retry');
      var phase = 'loader';
      var idleMs = 0;
      var lastTick = performance.now();
      var originalFetch = window.fetch;
      var controllers = new Set();
      var resourceUrls = new Set(['dataUrl', 'frameworkUrl', 'codeUrl', 'memoryUrl', 'symbolsUrl']
        .filter(function (key) { return config[key]; })
        .map(function (key) { return new URL(config[key], document.baseURI).href; }));
      var retryKey = 'geomodel-loading-retry:' + location.pathname + ':' + config.productVersion;
      var freshDownload = false;
      try {
        freshDownload = sessionStorage.getItem(retryKey) === '1';
        sessionStorage.removeItem(retryKey);
      } catch (_) { /* Storage can be unavailable in an embedded/private browser. */ }
      if (freshDownload) config.cacheControl = function () { return 'no-store'; };

      async function discardFailedResponses() {
        if (!freshDownload) return;
        try {
          if (!window.caches) return;
          // Unity 6000.0 stores downloaded responses here, separately from IDBFS saves.
          // Remove only this build's URLs so a later ordinary reload cannot reuse
          // corrupt bytes after a successful cache-bypassing retry.
          var name = 'UnityCache_' + config.companyName + '_' + config.productName;
          if (!await caches.has(name)) return;
          var cache = await caches.open(name);
          await Promise.all(Array.from(resourceUrls).map(function (url) { return cache.delete(url); }));
        } catch (error) {
          console.warn('[G-LAB loading] Could not discard cached build responses', error);
        }
      }

      document.getElementById('loading-version').textContent = config.productVersion;
      panel.dataset.state = phase;

      function active() { return phase !== 'failed' && phase !== 'ready'; }
      function activity() { idleMs = 0; }
      function restoreFetch() {
        if (window.fetch === watchedFetch) window.fetch = originalFetch;
      }
      function cleanup() {
        clearInterval(watchdog);
        // Unity's cache can attempt a fallback fetch after an aborted request.
        // Keep build URLs blocked on failure until the user reloads the iframe.
        if (phase === 'ready') restoreFetch();
        window.removeEventListener('error', onError);
        window.removeEventListener('unhandledrejection', onRejection);
        window.removeEventListener('offline', onOffline);
        window.removeEventListener('online', onOnline);
        document.removeEventListener('visibilitychange', onVisibility);
      }
      function fail(kind, detail) {
        if (!active()) return false;
        phase = 'failed';
        panel.dataset.state = phase;
        panel.setAttribute('aria-busy', 'false');
        title.textContent = '読み込みが止まりました';
        status.textContent = kind === 'startup'
          ? 'ゲームを起動できませんでした。'
          : 'ゲームのデータを読み込めませんでした。';
        help.textContent = kind === 'startup'
          ? 'ほかのタブやアプリを閉じて、もう一度お試しください。'
          : 'インターネットの接続を確認して、もう一度お試しください。';
        retry.hidden = false;
        cleanup();
        controllers.forEach(function (controller) { controller.abort(); });
        controllers.clear();
        console.error('[G-LAB loading]', kind, detail);
        return true;
      }

      // Pass each chunk directly to Unity without duplicating the entire data file.
      // This records real transfer activity even when Unity's percentage hasn't moved.
      async function watchedFetch(input, options) {
        var url = new URL(input instanceof Request ? input.url : input, document.baseURI).href;
        if (!resourceUrls.has(url) || phase === 'ready') return originalFetch.call(window, input, options);
        if (phase === 'failed') throw new Error('Game startup has stopped');
        var controller = new AbortController();
        var sourceSignal = (options && options.signal) || (input instanceof Request && input.signal);
        function abort() { controller.abort(); }
        if (sourceSignal) {
          if (sourceSignal.aborted) abort();
          else sourceSignal.addEventListener('abort', abort, { once: true });
        }
        controllers.add(controller);
        function finish() {
          controllers.delete(controller);
          if (sourceSignal) sourceSignal.removeEventListener('abort', abort);
        }
        try {
          var requestOptions = Object.assign({}, options, { signal: controller.signal });
          if (freshDownload) requestOptions.cache = 'reload';
          var response = await originalFetch.call(window, input, requestOptions);
          // Unity uses 304 when revalidating a previously cached data file.
          if (!response.ok && response.status !== 304) throw new Error('HTTP ' + response.status);
          activity();
          if (!response.body || response.status === 304) { finish(); return response; }
          var reader = response.body.getReader();
          var stream = new ReadableStream({
            async pull(sink) {
              try {
                var chunk = await reader.read();
                if (chunk.done) { finish(); sink.close(); }
                else { activity(); sink.enqueue(chunk.value); }
              } catch (error) {
                finish();
                fail('network', error);
                sink.error(error);
              }
            },
            cancel(reason) { finish(); return reader.cancel(reason); }
          });
          var monitored = new Response(stream, {
            status: response.status, statusText: response.statusText, headers: response.headers
          });
          // Unity and its cache expect the response's original URL and metadata.
          ['url', 'type', 'redirected'].forEach(function (key) {
            Object.defineProperty(monitored, key, { value: response[key] });
          });
          return monitored;
        } catch (error) {
          finish();
          fail('network', error);
          throw error;
        }
      }

      function isUnityError(detail) {
        return /Unknown data format|WebAssembly|wasm|memory access|out of memory|abort\(|Unity|\.unityweb|\.framework\.js/i.test(String(detail));
      }
      function onError(event) {
        if (isUnityError(event.filename + ' ' + event.message))
          fail(/Unknown data format/i.test(event.message) ? 'network' : 'startup', event.message);
      }
      function onRejection(event) {
        if (isUnityError(event.reason))
          fail(/Unknown data format/i.test(String(event.reason)) ? 'network' : 'startup', event.reason);
      }
      function onOffline() {
        if (!active()) return;
        status.textContent = 'インターネットの接続を待っています…';
        help.textContent = '接続が戻ると読み込みを続けます。接続を確認してください。';
        retry.hidden = false;
      }
      function onOnline() { activity(); renderStatus(); }
      function onVisibility() {
        // Time in a background tab must not be treated as a stalled download.
        lastTick = performance.now();
        activity();
      }
      function renderStatus() {
        if (!active()) return;
        if (navigator.onLine === false) { onOffline(); return; }
        if (idleMs >= 15000) {
          status.textContent = phase === 'starting'
            ? '起動に時間がかかっています…'
            : 'データが届くのを待っています…';
          help.textContent = phase === 'starting'
            ? 'この画面を開いたまま、もう少しお待ちください。'
            : '通信に時間がかかっています。接続を確認して、そのままお待ちください。';
          retry.hidden = false;
          return;
        }
        status.textContent = phase === 'starting' ? 'ゲームを起動しています…'
          : phase === 'loader' ? '読み込みの準備をしています…' : 'ゲームのデータをダウンロードしています…';
        help.textContent = phase === 'starting' ? 'まもなく始まります。このままお待ちください。'
          : '初回はダウンロードに時間がかかることがあります。この画面を開いたままお待ちください。';
        retry.hidden = true;
      }
      function progress(value) {
        if (!active()) return;
        activity();
        // Unity's 0–90% covers transfers; the remainder covers initialization.
        var next = value >= 0.9 ? 'starting' : 'downloading';
        phase = next;
        panel.dataset.state = phase;
        var bounded = Math.max(0, Math.min(100, Math.floor(value * 100)));
        fill.style.width = bounded + '%';
        percent.textContent = bounded + '%';
        bar.setAttribute('aria-valuenow', String(bounded));
        renderStatus();
      }

      retry.onclick = function () {
        retry.disabled = true;
        retry.textContent = '読み込みをやり直しています…';
        try { sessionStorage.setItem(retryKey, '1'); } catch (_) {}
        // Reload the same iframe. Never clear PlayerPrefs, invitation identity or saves.
        location.reload();
      };
      window.fetch = watchedFetch;
      window.addEventListener('error', onError);
      window.addEventListener('unhandledrejection', onRejection);
      window.addEventListener('offline', onOffline);
      window.addEventListener('online', onOnline);
      document.addEventListener('visibilitychange', onVisibility);
      var watchdog = setInterval(function () {
        var now = performance.now();
        if (!document.hidden) idleMs += now - lastTick;
        lastTick = now;
        if (idleMs >= (phase === 'starting' ? 180000 : 90000)) fail(phase === 'starting' ? 'startup' : 'network', 'No loading activity');
        else renderStatus();
      }, 1000);
      renderStatus();

      var script = document.createElement('script');
      script.src = freshDownload ? loaderUrl + '?retry=' + Date.now() : loaderUrl;
      script.onerror = function () { fail('network', 'Loader script unavailable'); };
      script.onload = function () {
        if (!active()) return;
        activity();
        Promise.resolve().then(function () {
          return createUnityInstance(canvas, config, progress);
        }).then(function (instance) {
          if (!active()) { instance.Quit(); return; }
          phase = 'ready';
          panel.dataset.state = phase;
          panel.setAttribute('aria-busy', 'false');
          panel.hidden = true;
          window.unityInstance = instance;
          cleanup();
        }).catch(function (error) { fail('startup', error); });
      };
      discardFailedResponses().then(function () {
        if (active()) document.body.appendChild(script);
      });
      return {
        fail: fail,
        handleBanner: function (message, type) {
          if (phase === 'ready') return false;
          // Unity may emit English fallback warnings after a failed transfer.
          // Keep diagnostics in the console and the actionable Japanese UI visible.
          if (type === 'error') fail('startup', message);
          else console.warn('[G-LAB loading]', message);
          return true;
        }
      };
    }
  };
}());
