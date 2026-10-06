// Serves the Unity WebGL build on our own domain.
// Everything except the three .unityweb files is a Workers static asset. Those
// three are gzip files on disk: the data file is over the 25 MiB asset limit,
// and all of them must be sent with Content-Encoding: gzip so the browser
// decompresses them natively (the loader's JS fallback costs too much memory on
// iPhone X). They live in R2 under releases/<RELEASE>/Build/.
const UNITYWEB_TYPES = {
  "WebGL.data.unityweb": "application/octet-stream",
  "WebGL.framework.js.unityweb": "application/javascript",
  "WebGL.wasm.unityweb": "application/wasm",
};

export default {
  async fetch(request, env) {
    const name = new URL(request.url).pathname.match(/^\/Build\/([^/]+)$/)?.[1];
    const type = name && UNITYWEB_TYPES[name];
    if (!type) return new Response("Not found", { status: 404, headers: { "X-Robots-Tag": "noindex" } });
    if (request.method !== "GET" && request.method !== "HEAD") {
      return new Response(null, { status: 405, headers: { Allow: "GET, HEAD" } });
    }

    // Unity's data cache revalidates with If-None-Match / If-Modified-Since.
    const conditions = new Headers();
    for (const header of ["If-None-Match", "If-Modified-Since"]) {
      const value = request.headers.get(header);
      if (value) conditions.set(header, value);
    }
    const object = await env.BUILDS.get(`releases/${env.RELEASE}/Build/${name}`, { onlyIf: conditions });
    if (!object) return new Response("Not found", { status: 404, headers: { "X-Robots-Tag": "noindex" } });

    const headers = new Headers({
      "Content-Type": type,
      "Content-Encoding": "gzip",
      "Cache-Control": "no-cache",
      ETag: object.httpEtag,
      "Last-Modified": object.uploaded.toUTCString(),
      "X-Robots-Tag": "noindex",
    });
    if (!("body" in object)) return new Response(null, { status: 304, headers });
    headers.set("Content-Length", String(object.size));
    return new Response(request.method === "HEAD" ? null : object.body, { headers, encodeBody: "manual" });
  },
};
