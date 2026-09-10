"""Build the self-contained local HTML gallery and Markdown report from reviewed evidence."""
from pathlib import Path
import html
import json

root=Path(__file__).resolve().parents[2]
directory=root/'Docs/reports/2026-09-10'
items=json.loads((directory/'items.json').read_text())
esc=html.escape
cards=[]
md=['# 四批修改验收与截图','',
'本轮覆盖 BUG-001～010，以及 BUG-011 的对话记录和参与者登录两项。下面每项附实际运行截图和简短说明。原图来自需求文档，仅代表记录问题时的旧版状态。','',
'截图说明：Unity 6000.0.51f1；桌面 1366×768、平板比例 1024×768。截图使用独立测试进度；钻塔阶段通过编辑器测试夹具进入，问答则逐题操作实际按钮，故意答错两次。长文分页使用重复句子的压力测试文本。平板比例视口不等于实际 iPad Safari 测试。','']
for item in items:
    figures=[]
    for filename,caption in item['images']:
        assert (directory/'screenshots'/filename).exists(), filename
        figures.append(f'<figure><button class="zoom" data-image="screenshots/{esc(filename)}" aria-label="放大：{esc(caption)}"><img src="screenshots/{esc(filename)}" loading="lazy" alt="{esc(caption)}"></button><figcaption>{esc(caption)}</figcaption></figure>')
    before=''
    if item.get('before'):
        before=f'<details><summary>查看需求文档中的原画面</summary><img class="before" src="before/{esc(item["before"])}" loading="lazy" alt="需求记录时的旧版截图"><p class="caption">原图来自 bugList.md，非本次运行截图。</p></details>'
    cards.append(f'<article data-batch="{item["batch"]}" id="bug-{item["id"]}"><div class="meta">BUG-{item["id"]} <span>第 {item["batch"]} 批</span></div><h2>{esc(item["title"])}</h2><p>{esc(item["summary"])}</p><div class="figures">{"".join(figures)}</div>{before}</article>')
    md += [f'## BUG-{item["id"]} · {item["title"]}','',item['summary'],'']
    for filename,caption in item['images']:
        md += [f'![{caption}]({directory / "screenshots" / filename})','',caption,'']
verification=(directory/'verification.md').read_text() if (directory/'verification.md').exists() else '最终验证记录正在补充。'
md += ['## 验证与使用','',verification,'',
'代码与素材来源见 [素材说明](../../art/2026-09-10-remediation-assets.md)，本地复现方法见 [QA 工具说明](../../../tools/qa/README.md)。','']
(directory/'README.md').write_text('\n'.join(md))
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Geo Model · 改善验收</title><style>
:root{color-scheme:light;--ink:#142d35;--muted:#59717a;--mint:#18765e;--line:#d7e1df}*{box-sizing:border-box}body{margin:0;background:#f2f5f1;color:var(--ink);font:16px/1.7 -apple-system,BlinkMacSystemFont,"PingFang SC","Noto Sans CJK SC",sans-serif}header{background:#102b34;color:#eef8f1;padding:64px max(24px,calc((100vw - 1180px)/2));border-bottom:7px solid #63d6b6}header .brand{color:#84dfc4;letter-spacing:.12em;font-size:13px}h1{font-size:clamp(30px,4vw,50px);font-weight:650;line-height:1.2;margin:20px 0}header p{max-width:800px;color:#c7dcd6}.stats{display:flex;gap:16px;flex-wrap:wrap;margin-top:26px}.stats span{border:1px solid #476669;padding:8px 18px;border-radius:4px}main{max-width:1228px;margin:auto;padding:24px}.filters{display:flex;gap:8px;flex-wrap:wrap;position:sticky;top:0;background:#f2f5f1f5;padding:16px 0;z-index:2;backdrop-filter:blur(10px)}.filters button{font:inherit;padding:8px 18px;border:1px solid var(--line);border-radius:4px;background:white;color:var(--ink);cursor:pointer}.filters button[aria-pressed=true]{background:var(--ink);color:white;border-color:var(--ink)}.note{font-size:14px;color:var(--muted);border-left:3px solid #60af97;padding:0 16px;margin:12px 0 28px}article{background:white;padding:30px;margin:0 0 28px;border:1px solid var(--line);border-radius:8px;scroll-margin-top:100px}.meta{font-weight:700;color:var(--mint);font-size:13px;letter-spacing:.06em}.meta span{color:var(--muted);font-weight:400;margin-left:16px}h2{font-size:27px;line-height:1.4;margin:8px 0 12px}article>p{max-width:900px;margin:0 0 24px}.figures{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,470px),1fr));gap:20px}figure{margin:0;min-width:0}.zoom{display:block;width:100%;padding:0;background:#10232c;border:0;cursor:zoom-in}.zoom img{display:block;width:100%;height:auto}figcaption,.caption{font-size:14px;color:var(--muted);margin-top:9px}details{margin-top:22px;border-top:1px solid var(--line);padding-top:15px}summary{cursor:pointer;color:var(--mint);font-size:14px}.before{display:block;max-width:100%;max-height:600px;margin-top:18px}.verification{padding:28px;background:#e2eeE8;border-radius:8px;white-space:pre-line}.verification a{color:var(--mint)}footer{color:var(--muted);font-size:14px;padding:35px 0}dialog{border:0;border-radius:6px;padding:12px;background:#10232c;max-width:96vw;max-height:95vh}dialog::backdrop{background:#051219df}dialog img{display:block;max-width:92vw;max-height:85vh;object-fit:contain}dialog button{float:right;background:transparent;color:white;border:1px solid #628a88;padding:6px 20px;margin-bottom:10px;cursor:pointer}[hidden]{display:none!important}@media(max-width:600px){header{padding:38px 22px}main{padding:14px}article{padding:20px 14px}h2{font-size:23px}}
</style><header><div class="brand">G-LAB / GEO MODEL</div><h1>逐项看这次的变化</h1><p>四批修改的实际运行画面。每一项都配有简短说明，点击图片可以放大；有原始截图的项目可以展开对照。</p><div class="stats"><span>4 批修改</span><span>12 个改善项</span><span>三语界面</span></div></header><main><nav class="filters" aria-label="按批次筛选"><button aria-pressed="true" data-filter="all">全部项目</button><button aria-pressed="false" data-filter="1">第 1 批 · 菜单与保存</button><button aria-pressed="false" data-filter="2">第 2 批 · 阅读与道具</button><button aria-pressed="false" data-filter="3">第 3 批 · 调查与结算</button><button aria-pressed="false" data-filter="4">第 4 批 · 历史与研究</button></nav><p class="note">截图来自 Unity 实际运行；部分流程使用独立测试进度进入指定阶段。1024×768 为平板比例视口，并非实体 iPad Safari。长文分页图使用压力测试文本。</p>'''+''.join(cards)+f'''<section class="verification"><strong>验证记录</strong>\n{esc(verification)}</section><footer>2026-09-10 · Unity 6000.0.51f1 · 本地验收记录<br><a href="README.md">Markdown 版本</a> · <a href="verification.md">完整验证说明</a></footer></main><dialog><button id="close">关闭 ✕</button><img alt="放大的实际运行截图"></dialog><script>const dialog=document.querySelector('dialog');document.querySelectorAll('[data-filter]').forEach(button=>button.onclick=()=>{{document.querySelectorAll('[data-filter]').forEach(b=>b.setAttribute('aria-pressed',String(b===button)));document.querySelectorAll('article').forEach(a=>a.hidden=button.dataset.filter!=='all'&&a.dataset.batch!==button.dataset.filter)}});document.querySelectorAll('[data-image]').forEach(button=>button.onclick=()=>{{dialog.querySelector('img').src=button.dataset.image;dialog.showModal()}});document.querySelector('#close').onclick=()=>dialog.close();dialog.onclick=e=>{{if(e.target===dialog)dialog.close()}};</script></html>'''
(directory/'index.html').write_text(page)
print(f'Generated HTML and Markdown for {len(items)} items')
