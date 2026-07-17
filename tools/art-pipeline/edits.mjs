// Teacher-review EDIT specs for the 11 knowledge banners.
// Unlike specs.mjs (generate-from-scratch), these feed the EXISTING banner as the
// base image (img2img edit) and ask Codex imagegen to change ONLY the flagged spot.
// Source of the edits: memory project_teacher-review-on-banners (Inagaki, 2026-06-05).
//
// Output goes to a STAGING dir (revised/), NOT over the originals — review, then swap in.

import { join } from "node:path";

export const SRC_DIR = "/Users/user/Unity/GeoModelTest/Assets/Resources/Story/Illustrations";
export const OUT_DIR = "/Users/user/Unity/GeoModelTest/tools/art-pipeline/revised";

export const WIDTH = 1536;
export const HEIGHT = 1024;

// One entry per output image. `edit` = the human-readable change(s); kind is just for logs.
export const EDITS = [
  {
    id: "01_weathering_order",
    kind: "illustration",
    topic: "大地のでき方: a 4-panel left-to-right sequence ①風化 ②侵食 ③運搬 ④堆積.",
    edit:
      "Panel ② labeled 侵食 (erosion) currently looks almost identical to panel ① 風化 (weathering) — both are just a rocky mountain. " +
      "Redraw ONLY panel ② so it clearly shows EROSION: flowing water (and/or wind) actively cutting and carving the rock — e.g. a stream " +
      "cutting a V-shaped valley into the rock and washing fragments downstream. It must look visibly DIFFERENT from panel ① weathering. " +
      "Keep panels ①, ③, ④, all the connecting arrows, the title 大地のでき方, and the four labels (風化/侵食/運搬/堆積) exactly as they are.",
  },
  {
    id: "02_rock_grainsize",
    kind: "text",
    topic: "粒の大きさで分かれる岩 (rocks by grain size: れき岩/砂岩/泥岩).",
    edit:
      "In the phrase 「粗い粒が積もる」 at the bottom-left, the furigana above the kanji 粒 is WRONG: it currently reads カ. " +
      "Change ONLY that furigana to read つぶ (so 粒 is read つぶ). Do not touch any other text, the three magnifier circles, " +
      "the れき岩/砂岩/泥岩 labels, the seabed cross-section, or the title.",
  },
  {
    id: "03_limestone",
    kind: "text",
    topic: "石灰岩 (limestone fizzing with acid).",
    edit:
      "The title line has TWO stray garbage symbols that must be deleted: " +
      "(1) right after 塩酸 there is a stray hooked 「）」 — delete it so that part reads 「うすい塩酸で」. " +
      "(2) right after 二酸化炭素 there is a stray 「♪」 music-note symbol — delete ONLY the ♪ (keep the round brackets) so that part reads 「あわ（二酸化炭素）が出る」. " +
      "After the fix the full title must read exactly: 「うすい塩酸で　あわ（二酸化炭素）が出る」. " +
      "Keep the furigana えんさん over 塩酸 and にさんかたんそ over 二酸化炭素. Everything else (limestone rock, dropper, fizzing bubbles, 成り立ち inset) identical.",
  },
  {
    id: "04_chert",
    kind: "text",
    topic: "チャート (chert, too hard to scratch).",
    edit:
      "Make TWO text fixes and one furigana fix: " +
      "(1) In the title 「くぎでも傷）がつかない」 delete the stray 「）」 after 傷 so it reads 「くぎでも傷がつかない」 (keep furigana きず over 傷). " +
      "(2) In the small caption under the radiolaria inset, change 「放散虫」 to 「放散虫など」 (keep furigana ほうさんちゅう over 放散虫). " +
      "(3) In that same small caption 「（とげのある小さなケイ酸の殻）が海にうかぶ」, give the reading where missing: 酸 → furigana さん, 殻 → furigana から (i.e. ケイ酸〔さん〕の殻〔から〕). " +
      "Everything else (the chert rock, the nail, the red ✕, the 成り立ち inset, the title チャート) identical.",
  },
  {
    id: "05_coral_env",
    kind: "illustration",
    topic: "サンゴ → あたたかくて浅い海 (coral = warm shallow sea).",
    edit:
      "The coral reef on the right is too large and too tall, reaching up close to the sea surface. " +
      "Redraw the coral SMALLER and LOWER so there is a clear band of open water between the TOP of the coral and the sea surface. " +
      "Keep the sunlight rays, the fish, the sun above the surface, the 「あたたかい」 thermometer icon, the title 「サンゴ → あたたかくて浅い海」, and all furigana exactly as they are.",
  },
  {
    id: "06_facies_fossil",
    kind: "content-add",
    topic: "示相化石＝環境がわかる (facies fossils → environment; サンゴ/シジミ/アサリ).",
    edit:
      "The middle card 「シジミ＝河口（汽水）」 needs a short clarifying note that 河口 is the place BETWEEN a river and the sea. " +
      "Add a small friendly caption near/under the シジミ card reading 「川と海の間（あいだ）」 (same illustration style and font, small). " +
      "Keep the three cards (サンゴ/シジミ/アサリ), their arrows and environments, the title, and all other text exactly as they are.",
  },
  {
    id: "07_ammonite_mesozoic",
    kind: "content-add",
    topic: "アンモナイト → 中生代 (ammonite = Mesozoic).",
    edit:
      "Add the age range of the Mesozoic to the 「中生代」 segment of the bottom timeline. " +
      "Place a small caption directly under (or beside) 「中生代」 reading 「約2億5,200万年〜約6,600万年前」 in a small friendly font. " +
      "Keep the ammonite, the dinosaur silhouette, 「恐竜と同じ時代」, the three-segment timeline (古生代/中生代/新生代), the arrow, the title, and all furigana otherwise identical.",
  },
  {
    id: "08_index_fossil_eras",
    kind: "illustration",
    topic: "示準化石＝時代がわかる (index fossils per era: サンヨウチュウ/アンモナイト/ビカリア・ナウマンゾウ).",
    edit:
      "In the 新生代 (Cenozoic) panel, the fossil labeled ビカリア is drawn WRONG — it is currently a smooth clam / bivalve shell. " +
      "Redraw it as a REAL Vicarya (ビカリア): an elongated, SCREW-SHAPED spiral gastropod sea-snail shell — a tall pointed conical spire with " +
      "several whorls and knobby spines, like a turret/auger shell — NOT a clam. Keep the Naumann elephant beside it. " +
      "Keep the 古生代 trilobite panel, the 中生代 ammonite+dinosaur panel, the colored era timeline bands, the title 示準化石＝時代がわかる, and all labels/furigana exactly as they are.",
  },
  {
    id: "09_tuff_volcano",
    kind: "text",
    topic: "凝灰岩は火山のしるし (tuff = sign of a volcano).",
    edit:
      "The title has a DOUBLED の: it currently reads 「凝灰岩は火山ののしるし」. Delete ONE の so the title reads exactly 「凝灰岩は火山のしるし」. " +
      "Keep the furigana ぎょうかいがん over 凝灰岩. Everything else (the erupting volcano, the ash plume, the strata column with the highlighted pale tuff layer, the bottom labels) identical.",
  },
  {
    id: "10_keybed_tilt",
    kind: "text",
    topic: "鍵層と地層の傾き (key bed → strata tilt; columns A/B/C).",
    edit:
      "Make TWO text changes: " +
      "(1) The left-side label box currently reads 「鍵層（火山灰の層）」. Replace it with the simpler wording 「たとえばここが鍵層」 and REMOVE the 「火山灰の層」 explanation entirely (keep furigana かぎそう over 鍵層). " +
      "(2) The bottom box currently reads 「東ほど深い＝東に傾く」. Add the subject 鍵層 to the first half so it reads 「鍵層が東ほど深い＝東に傾く」. " +
      "Keep the three columns A/B/C at the same scale, the dashed correlation line, the 東 arrow, and everything else identical.",
  },
  {
    id: "11_fold_fault",
    kind: "illustration+text",
    topic: "しゅう曲と断層 (fold vs fault).",
    edit:
      "Make TWO changes: " +
      "(1) Size balance: the しゅう曲 (fold) illustration on top is large while the 断層 (fault) illustration is a much smaller bottom-right inset. " +
      "Redraw so the fold illustration and the fault illustration are ROUGHLY THE SAME SIZE (give the fault its own comparable panel, not a tiny inset). " +
      "(2) The 「しゅう曲」 label has its reading きょく duplicated — there is furigana きょく above 曲 AND 「（きょく）」 written after it. Remove ONE: keep just the furigana きょく above 曲 and DELETE the 「（きょく）」 in parentheses. " +
      "Keep the left/right compression arrows, the captions 「＝おし縮められて曲がる」 and 「断層＝ずれる」, the title, and everything else identical.",
  },
];

const STYLE_GUARD = `# How to edit (CRITICAL)
- Image #1 is an EXISTING finished banner. It is already correct EXCEPT for the change described above.
- Treat this as a surgical edit: reproduce Image #1 as faithfully as possible — same layout, same illustrations,
  same colors, same flat educational style, same fonts, and ALL OTHER TEXT byte-for-byte the same.
- Change ONLY what the "# The change(s) to make" section says. Do NOT re-spell, move, restyle, or "improve" anything else.
- This is a DIAGRAM: no people, no faces, no watermark, no extra borders.
- Japanese: the notation 漢字（かな） means かな is the furigana (reading) of the kanji right before it — render it SMALL,
  above or beside that kanji. Keep every other kanji and kana exactly as in Image #1.`;

export function buildEditPrompt(spec, targetPath) {
  return `# Task
Edit the attached image (Image #1) and save the result as ONE ${WIDTH}x${HEIGHT} landscape PNG to:
${targetPath}

# What Image #1 is
${spec.topic}
It is used as a learning banner in a geology game for Japanese middle-school students (中学生).

# The change(s) to make
${spec.edit}

${STYLE_GUARD}

# Execution plan (FOLLOW EXACTLY)
1. ONE imagegen edit call, using the attached Image #1 as the base image to edit.
2. Locate the produced PNG in the imagegen cache (~/.codex/generated_images/<thread-id>/).
3. Copy it to: ${targetPath}
4. If it is not exactly ${WIDTH}x${HEIGHT}, run: sips -z ${HEIGHT} ${WIDTH} "${targetPath}"
5. Stop and report the absolute path.

# Hard restrictions on YOUR behavior (not on the image)
- Do NOT regenerate variations and pick. The FIRST imagegen output IS the answer.
- Do NOT pivot to hand-authoring SVG / canvas / Python PIL.
- Do NOT remap or "fix" the palette with ImageMagick. Trust the imagegen output.
- Just place the file at the target path and stop.`;
}

export function srcFor(id) {
  return join(SRC_DIR, `${id}.png`);
}
