// Emails the researcher once when Testee's status link reaches its target, so they can ask Testee to stop
// distribution right away. Runs from the cron trigger. The database hands out a 10-minute claim before
// sending; the claim is confirmed after the email is sent and released if sending fails. A claim that is
// neither (a lost response, a crash) expires, so the next run retries: the email is never skipped, and is
// repeated only if a confirmation is lost.
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
  const call = (action, claimId) => fetch(env.STATUS_API, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${env.STATUS_NOTIFY_TOKEN}`,
      // The link token alone only reads; claim/confirm/release also need this secret shared with Supabase.
      "X-Notify-Secret": env.STATUS_NOTIFY_SECRET,
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ action, claimId }),
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
  } catch (error) {
    const released = await call("release", status.claimId).catch(() => null);
    if (!released?.ok) console.error("Releasing the target notice failed; it is retried when the claim expires.");
    throw error;
  }
  console.log(`Target notice sent for ${status.label}: ${status.completed}/${status.target}`);
  const confirmed = await call("confirm", status.claimId);
  const result = confirmed.ok ? await confirmed.json() : null;
  if (!result?.done) throw new Error("The target notice was sent but not confirmed; it may be sent again when the claim expires.");
}
