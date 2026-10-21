// Emails the researcher once when Testee's status link reaches its target, so they can ask Testee to stop
// distribution right away. Runs from the cron trigger. The database claims the notice before sending and
// the claim is released if the email fails, so the next run retries and a sent notice is never repeated.
const jst = (value) =>
  new Intl.DateTimeFormat("ja-JP", {
    timeZone: "Asia/Tokyo", year: "numeric", month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit", hourCycle: "h23",
  }).format(new Date(value));

function message(status, pageUrl) {
  const lines = [
    `「${status.label}」的完了者数已达到目标。`,
    "",
    `完了：${status.completed} / 目标 ${status.target} 名`,
    `保護者同意 ${status.consented} ／ ゲームクリア ${status.gameCleared} ／ アンケート回答 ${status.surveyed}`,
    `要確認（未计入完了数）：${status.review.length} 名`,
    `确认时间：${jst(status.generatedAt)}（日本时间）`,
    "",
    `回收状况页面：${pageUrl}`,
    "",
    "下一步：请联系テスティー停止配信，并处理「要確認」里的人。",
    "这封邮件对每个回收状况链接只发送一次。",
  ];
  const escape = (text) => text.replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;");
  const html = lines.map((line) => (line ? `<p style="margin:0 0 4px">${escape(line)}</p>` : "<br>"))
    .join("\n").replace(escape(pageUrl), `<a href="${pageUrl}">${escape(pageUrl)}</a>`);
  return {
    subject: `【回收状况】已达到目标 ${status.target} 名：${status.label}`,
    text: lines.join("\n"),
    html: `<div style="font-family:sans-serif;font-size:15px;line-height:1.7">${html}</div>`,
  };
}

export async function notifyTarget(env) {
  // Nothing to do until the link token, the notice secret and the mail binding are configured.
  if (!env.STATUS_NOTIFY_TOKEN || !env.STATUS_NOTIFY_SECRET || !env.NOTIFY_EMAIL) return;
  const call = (action) => fetch(env.STATUS_API, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${env.STATUS_NOTIFY_TOKEN}`,
      // The link token alone only reads; claim/release also need this secret shared with Supabase.
      "X-Notify-Secret": env.STATUS_NOTIFY_SECRET,
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ action }),
  });
  const claimed = await call("claim");
  if (!claimed.ok) throw new Error(`Status claim failed (HTTP ${claimed.status})`);
  const status = await claimed.json();
  if (!status.notify) return;
  try {
    await env.NOTIFY_EMAIL.send({
      to: env.NOTIFY_TO,
      from: { email: env.NOTIFY_FROM, name: "geo-q 回収状況" },
      ...message(status, `${env.PUBLIC_ORIGIN}/status/${env.STATUS_NOTIFY_TOKEN}`),
    });
    console.log(`Target notice sent for ${status.label}: ${status.completed}/${status.target}`);
  } catch (error) {
    await call("release");
    throw error;
  }
}
