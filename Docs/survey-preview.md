# 教师用问卷预览

分享入口：https://geoquest-survey-preview.pages.dev/

无需游戏通关、参与者邀请码或访问凭证。与正式问卷一样分 5 页填写 14 道日文题目，使用「戻る」「次へ」和末页「回答を送信する」按钮。预览不提供额外的总览、打印或章节跳转按钮，必答校验与正式问卷一致。

题目及选项读取正式问卷的 `Assets/StreamingAssets/Survey/questions.js`，样式复用 `survey.css`。预览不加载 `config.js` 或 `survey.js`，没有提交接口或浏览器持久化；试填答案只保存在当前页面内存中，刷新即清除。完成试填时明确显示「回答は送信されていません」。正式游戏入口及通关凭证校验不受影响。

源入口为 `Assets/StreamingAssets/Survey/preview.html`。更新 `questions.json` 后先运行 `python3 tools/sync-survey.py`，然后打包：

```sh
python3 tools/build-survey-preview.py --output Build/SurveyPreview
```

脚本会检查题目生成文件是否同步，并仅打包预览需要的静态文件。产物中的 `survey-preview-offline.html` 是可直接作为附件分享的独立网页，离线也能查看和试填。

独立预览使用 Cloudflare Pages 项目 `geoquest-survey-preview`，不需要重新构建 Unity 或替换 itch.io 上的游戏：

```sh
wrangler pages deploy Build/SurveyPreview --project-name geoquest-survey-preview --branch main
```

预览可由任何持有链接的人查看，已设置不被搜索引擎索引；不包含参与者数据。
