# ジオクエスト 操作ガイド動画

> 保存版（2026-09-15 の更新前に収録）。参加コードの入力やアンケート、道具一覧などに旧版の画面が含まれます。当時の制作資料・完成動画として保存しており、現在の参加者向けに配布する前に改訂が必要です。最新の変更と確認結果は [公開版の確認記録](../reports/2026-09-15-teacher-feedback/release/README.md) を参照してください。

日本の中学生向けに、スマートフォン・タブレットの操作を説明する動画です。

- 完成動画：`output/video/ジオクエスト_操作ガイド_日本語.mp4`
- 長さ：3分17秒。1920 × 1080、30 fps、H.264 / AAC。約20 MB。
- 短い日本語の操作字幕を画面内に表示。読み上げ全文の SRT も同じフォルダーにあります。
- ゲーム＋アンケートの約20分に、このガイドを見る時間は含めません。

## 映像と音声

ゲーム映像は EgoLite の実際の WebGL キャンバスから録画しました。録画用コピーをネイティブ 1920 × 1080、Unity 品質レベル5に設定しています。録画は可変フレームレートで、編集時に30 fpsにそろえています。ゲームのソースや共有ビルドは変更していません。

シーン・クエストの状態は録画用のローカルセッションで準備し、操作場面を別々に収録しました。全プレイの連続記録ではありません。選択肢と結果画面は既存マニュアルの実画面、アンケートはページ画像を補助的に使用しています。自由回答と完了画面は EgoLite 内の実際の HTML/CSS をローカルの模擬応答で表示し、DOM を画像化しました。参加者の回答や本番の送信を使用していません。

音声は所有者の既存の音声プロファイルを使い、ローカル Dots TTS / MLX で日本語を生成しました。読み方を安定させるため、一部の漢字は読み上げ入力だけひらがなにしています。画面内に AI 合成音声であることを記載しています。声のプロファイルや参照音声は配布ファイルに含めていません。

## 編集元

- `edit.json`：採用した映像、トリミング、字幕、操作マークの時間。
- `narration-ja.json`：日本語台本と読み上げ用表記。未採用の候補文も残しています。
- `Logs/manual-video/ego/`：EgoLite の元録画・画面画像・操作時刻。
- `Logs/manual-video/audio/`：文ごとの音声。
- `Logs/manual-video/render/timeline.json`：完成版のチャプター開始時刻。
- 既存画面の出典：`Docs/manual-ja/assets/sources.txt`。

ゲームの操作確認により、フェーズシフターのタッチ操作は「調べる」であることを再確認し、PDFの対応箇所も修正しました。PDFの自由回答画面も空欄の例に更新しています。

## 検証

19章の中間・末尾フレームを確認し、最後の4章は完成MP4から再確認しました。全編のデコードが成功し、0.4秒以上の黒画面は検出されませんでした。完成音声は約 -18 LUFS、true peak -1.5 dBTP。日本語の操作語はローカル Whisper による転写で照合しています。字幕の全文SRTは文単位の概算タイミングです。

## 再編集

プロジェクトルートで、Pillow の入った Python と FFmpeg を使用します。

```sh
python3 tools/manual-video/render.py --only 17-return 18-survey
python3 tools/manual-video/render.py --assemble
```

`--only` を省略すると全章を再描画します。フォントは `tmp/pdfs/testee-flow/NotoSansJP.ttf`、元素材は上記のローカル `Logs` に必要です。`Logs` はGit管理対象外です。録画環境の準備は `prepare_player.py` / `serve.py`、ブラウザー操作と録画は `ego.mjs`、DOM画像化は `dom_image.mjs`、音声生成は `narrate.py` にあります。

音声を再生成する場合は、ローカルの Dots TTS ランタイム、使用許可のある音声プロファイル、モデルの場所を明示します。これらは Git に含めません。

```sh
python3 tools/manual-video/narrate.py Docs/manual-video/narration-ja.json \
  --runtime /path/to/dots_tts_mlx \
  --profile /path/to/speaker.dtprofile \
  --weights /path/to/model-weights
```
