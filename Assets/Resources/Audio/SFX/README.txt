sfx_objects_fall.wav（地震时桌上的东西掉落，quest1.1 第 3 句的 sfx）
  来源：Pixabay「Heavy Book Falling」
  https://pixabay.com/sound-effects/film-special-effects-heavy-book-falling-96257/
  加工：转单声道 44.1kHz 16bit WAV，裁掉首尾静音，末尾 0.25 秒淡出（1.07 秒）
  许可：Pixabay Content License（https://pixabay.com/service/license-summary/）
    免费、可商用，不需要署名。禁止把素材原样单独再分发或出售；作为游戏的一部分使用没有问题。

知识幻灯片的效果音（2026-11-23 追加，KnowledgeSlides 每一步的 sfx 字段按文件名播放）
  全部来自効果音ラボ（https://soundeffect-lab.info/）。许可：免费、可商用、不需要署名；禁止把音源本身再分发。
  FAQ 明确说放在 GitHub 公开的游戏工程里不算再分发（https://soundeffect-lab.info/faq/）。
  加工：去掉开头静音，截取最多 2.5 秒，淡入淡出（环境类 0.15 秒淡入、0.3 秒淡出），单声道 44.1kHz 16bit WAV；
  0.5 秒以上的按 -18 LUFS 统一响度、峰值不超过 -1 dBFS，更短的按峰值 -3 dBFS 对齐。
  用 WAV 是因为本机 ffmpeg 没有 libvorbis；Unity 导入时会按导入设置重新压缩。

slide_pop.wav（0.18 秒）
  原名：決定ボタンを押す34（ポン。柔らかい音）  https://soundeffect-lab.info/sound/button/mp3/decision34.mp3（页面 https://soundeffect-lab.info/sound/sub-category/business.html）
  截取：开头静音后 0 秒起；响度：peak -3 dBFS
slide_chime.wav（1.35 秒）
  原名：クイズ正解2（鉄琴）  https://soundeffect-lab.info/sound/anime/mp3/correct2.mp3（页面 https://soundeffect-lab.info/sound/anime/）
  截取：开头静音后 0 秒起；响度：-21.1 → -18 LUFS
slide_rain.wav（2.5 秒）
  原名：雨が降る2（街の小雨）  https://soundeffect-lab.info/sound/environment/mp3/rain2.mp3（页面 https://soundeffect-lab.info/sound/animal/）
  截取：开头静音后 1.0 秒起；响度：-31.3 → -18 LUFS, peak limited to -1 dBFS
slide_roll.wav（0.61 秒）
  原名：石が転がる（岩の上）  https://soundeffect-lab.info/sound/various/mp3/stone-rolls1.mp3（页面 https://soundeffect-lab.info/sound/various/various3.html）
  截取：开头静音后 0 秒起；响度：-30.6 → -18 LUFS, peak limited to -1 dBFS
slide_splash.wav（0.61 秒）
  原名：水面に石投げ1（小さな石）  https://soundeffect-lab.info/sound/various/mp3/water-throw-stone1.mp3（页面 https://soundeffect-lab.info/sound/various/）
  截取：开头静音后 0 秒起；响度：-32.1 → -18 LUFS, peak limited to -1 dBFS
slide_sand.wav（2.0 秒）
  原名：崖から石がパラパラ落ちる  https://soundeffect-lab.info/sound/battle/mp3/cliff-stone-falls1.mp3（页面 https://soundeffect-lab.info/sound/battle/battle2.html）
  截取：开头静音后 0 秒起；响度：-29.5 → -18 LUFS, peak limited to -1 dBFS
slide_sink.wav（0.86 秒）
  原名：水中で息を吐く1（ブクッ）  https://soundeffect-lab.info/sound/various/mp3/underwater-breathing1.mp3（页面 https://soundeffect-lab.info/sound/sub-category/summer.html）
  截取：开头静音后 0 秒起；响度：-20.5 → -18 LUFS
slide_drip.wav（0.14 秒）
  原名：水滴2（ピチャッ）  https://soundeffect-lab.info/sound/various/mp3/water-drop2.mp3（页面 https://soundeffect-lab.info/sound/various/）
  截取：开头静音后 0 秒起；响度：peak -3 dBFS
slide_fizz.wav（2.2 秒）
  原名：グラスに炭酸飲料を注ぐ（シュワーッ）  https://soundeffect-lab.info/sound/various/mp3/glass-carbonated-drink1.mp3（页面 https://soundeffect-lab.info/sound/various/various2.html）
  截取：开头静音后 0.3 秒起；响度：-32.9 → -18 LUFS, peak limited to -1 dBFS
slide_shells.wav（0.76 秒）
  原名：碁石を碁笥から取り出す（ジャラッ）  https://soundeffect-lab.info/sound/various/mp3/go-take-out1.mp3（页面 https://soundeffect-lab.info/sound/various/various3.html）
  截取：开头静音后 0 秒起；响度：-31.6 → -18 LUFS, peak limited to -1 dBFS
slide_swish.wav（0.9 秒）
  原名：高速移動（シュン）  https://soundeffect-lab.info/sound/battle/mp3/highspeed-movement1.mp3（页面 https://soundeffect-lab.info/sound/battle/）
  截取：开头静音后 0 秒起；响度：-25.6 → -18 LUFS, peak limited to -1 dBFS
slide_scrape.wav（0.9 秒）
  原名：バケツを持ち上げる（鉄の摩擦音）  https://soundeffect-lab.info/sound/various/mp3/bucket1.mp3（页面 https://soundeffect-lab.info/sound/various/various2.html）
  截取：开头静音后 0 秒起；响度：-31.3 → -18 LUFS, peak limited to -1 dBFS
slide_sparkle.wav（1.5 秒）
  原名：きらーん1（目が光る音）  https://soundeffect-lab.info/sound/anime/mp3/eye-shine1.mp3（页面 https://soundeffect-lab.info/sound/anime/）
  截取：开头静音后 0 秒起；响度：-20.1 → -18 LUFS
slide_bubbles.wav（2.0 秒）
  原名：水ぶくぶく1（フラスコの中の液体が泡立つ）  https://soundeffect-lab.info/sound/various/mp3/water-bukubuku1.mp3（页面 https://soundeffect-lab.info/sound/various/）
  截取：开头静音后 0 秒起；响度：-27.5 → -18 LUFS
slide_eruption.wav（2.0 秒）
  原名：爆発2（ドーン）  https://soundeffect-lab.info/sound/battle/mp3/bomb2.mp3（页面 https://soundeffect-lab.info/sound/battle/battle2.html）
  截取：开头静音后 0 秒起；响度：-20.2 → -18 LUFS
slide_wind.wav（2.5 秒）
  原名：風が吹く1（ヒュオオオ）  https://soundeffect-lab.info/sound/environment/mp3/wind1.mp3（页面 https://soundeffect-lab.info/sound/animal/）
  截取：开头静音后 0.5 秒起；响度：-31.1 → -18 LUFS
slide_rumble.wav（2.5 秒）
  原名：ゴゴゴゴ  https://soundeffect-lab.info/sound/anime/mp3/gogogogo1.mp3（页面 https://soundeffect-lab.info/sound/anime/）
  截取：开头静音后 0 秒起；响度：-16.9 → -18 LUFS
slide_bend.wav（0.16 秒）
  原名：ぷよん（弾力のある物体）  https://soundeffect-lab.info/sound/anime/mp3/puyon1.mp3（页面 https://soundeffect-lab.info/sound/anime/）
  截取：开头静音后 0 秒起；响度：peak -3 dBFS
