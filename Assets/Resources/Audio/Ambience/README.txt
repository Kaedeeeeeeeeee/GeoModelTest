剧情环境音（AudioManager.PlayAmbience 按文件名加载：Resources/Audio/Ambience/<key>）

amb_classroom_chatter.wav
  来源：効果音ラボ「騒がしい高校の教室」（環境音ページ）
  https://soundeffect-lab.info/sound/environment/  原文件 high-school-class-room1.mp3
  加工：转单声道 44.1kHz 16bit WAV，去掉 mp3 首尾静音，首尾做 1.5 秒等功率交叉淡化，可无缝循环（16.9 秒）

amb_classroom_panic.wav
  来源：効果音ラボ「逃げ惑う人々」（声素材・大人数ページ）
  https://soundeffect-lab.info/sound/voice/people.html  原文件 run-away-people1.mp3
  加工：同上，首尾做 0.5 秒交叉淡化，可无缝循环（3.6 秒）

许可：効果音ラボ利用规约 https://soundeffect-lab.info/agreement/
  免费、可商用，不需要署名或链接（任意）。禁止把音源本身再分发，也禁止用于 AI 学习。
  FAQ（https://soundeffect-lab.info/faq/）「効果音を入れたアプリをGithubで公開する際、音源ファイルむき出しでも良いですか？」
  的回答是没有问题、不视为再分发（尽量隐藏音源文件即可）。
用 WAV 而不用 mp3/ogg：mp3 解码首尾有空白，循环会断；本机 ffmpeg 没有 libvorbis，只能做双声道、质量较差的 ogg。
Unity 导入时会按 AudioImporter 设置重新压缩，所以用无损 WAV 作源文件。
