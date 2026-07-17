// Knowledge-illustration banner specs for the GeoModelTest story quiz flow.
// One image per quiz questionId. JP furigana labels are baked in (Japanese-only for now).
// See memory: project_knowledge-illustrations-plan.

export const ANCHOR = "/Users/user/Unity/GeoModelTest/Assets/Resources/Tachie/kaede.png";
export const OUT_DIR = "/Users/user/Unity/GeoModelTest/Assets/Resources/Story/Illustrations";

// Target render size — gpt-image landscape native (no distorting resize needed).
export const WIDTH = 1536;
export const HEIGHT = 1024;

// 11 specs, in play order. `id` is the output filename stem; `questionId` ties it to the story JSON.
export const SPECS = [
  {
    id: "01_weathering_order",
    questionId: "q.weathering_order",
    title: "大地のでき方 4つのはたらき",
    subject: "The four processes that turn mountain rock into layered strata, shown as a left-to-right sequence.",
    composition:
      "Four stages across the wide banner, connected by big friendly arrows. " +
      "Stage 1: a rocky mountain peak. Stage 2: the rock surface cracking and crumbling into smaller fragments. " +
      "Stage 3: a river carrying gravel, sand and mud downstream. " +
      "Stage 4: sediment settling on the sea floor, building up flat horizontal strata (shown as a cross-section on the right).",
    labels: ["風化（ふうか）", "侵食（しんしょく）", "運搬（うんぱん）", "堆積（たいせき）"],
  },
  {
    id: "02_rock_grainsize",
    questionId: "q.rock_mudstone",
    title: "粒の大きさで分かれる岩",
    subject: "Sedimentary rocks classified by particle size: gravel-rock, sandstone, mudstone.",
    composition:
      "Three round magnifier circles in a row, each showing particles of decreasing size from left to right: " +
      "big rounded pebbles (over 2mm), medium sand grains, then very fine smooth mud (under 0.06mm). " +
      "Below them a thin seabed cross-section showing coarse particles deposited near the shore and the finest particles deposited far offshore in deeper water.",
    labels: ["れき岩（がん） 2mm以上（いじょう）", "砂岩（さがん）", "泥岩（でいがん） 0.06mm以下（いか）"],
  },
  {
    id: "03_limestone",
    questionId: "q.rock_limestone",
    title: "石灰岩",
    subject: "Limestone fizzes carbon dioxide when dilute hydrochloric acid is dropped on it; it forms from shells and coral.",
    composition:
      "Center-left: a chunk of pale grey limestone with a glass dropper releasing one drop of dilute acid onto it, fizzing bubbles rising off the surface. " +
      "Small rounded inset at the bottom-right: seashells and coral piling up and compacting into rock, showing the origin.",
    labels: ["石灰岩（せっかいがん）", "うすい塩酸（えんさん）で あわ（二酸化炭素（にさんかたんそ））が出（で）る"],
  },
  {
    id: "04_chert",
    questionId: "q.rock_chert",
    title: "チャート",
    subject: "Chert is so hard that a nail cannot scratch it; it forms from the silica shells of radiolaria.",
    composition:
      "Center-left: a hard glassy chert rock with an iron nail dragging across it leaving NO scratch, marked with a small red cross / 'no scratch' symbol. " +
      "Small rounded inset at the bottom-right: microscopic radiolaria (tiny spiky silica shells) accumulating to form the rock.",
    labels: ["チャート", "くぎでも傷（きず）がつかない（とてもかたい）"],
  },
  {
    id: "05_coral_env",
    questionId: "q.fossil_coral_env",
    title: "サンゴが教える海",
    subject: "Coral lives only in warm, shallow, sunlit sea.",
    composition:
      "A bright warm shallow-sea scene: sunlight rays piercing clear blue-green shallow water, a healthy colorful coral reef on the sea floor, " +
      "a friendly sun above the surface, and a small thermometer icon reading 'warm'. The mood clearly says warm and shallow.",
    labels: ["サンゴ → あたたかくて浅（あさ）い海（うみ）"],
  },
  {
    id: "06_facies_fossil",
    questionId: "q.fossil_facies_term",
    title: "示相化石（環境を示す）",
    subject: "Facies fossils tell the environment in which the strata formed.",
    composition:
      "A header band across the top, then three paired cards in a row. Each card: a fossil on the left, an arrow, and its environment on the right. " +
      "Card 1: coral leads to a warm shallow sea. Card 2: a shijimi clam leads to a brackish river-mouth. Card 3: an asari clam leads to a shallow sea.",
    labels: [
      "示相化石（しそうかせき）＝環境（かんきょう）がわかる",
      "サンゴ＝あたたかい浅（あさ）い海（うみ）",
      "シジミ＝河口（かこう）・汽水（きすい）",
      "アサリ＝浅（あさ）い海（うみ）",
    ],
  },
  {
    id: "07_ammonite_mesozoic",
    questionId: "q.fossil_ammonite_era",
    title: "アンモナイト＝中生代",
    subject: "The ammonite is an index fossil of the Mesozoic era, the same age as the dinosaurs.",
    composition:
      "Center: a large detailed ammonite (spiral shell) fossil. Behind it, the silhouette of a dinosaur. " +
      "Along the bottom, a simple horizontal timeline split into three segments; the middle segment is highlighted and an arrow points from the ammonite to it.",
    labels: ["アンモナイト → 中生代（ちゅうせいだい）", "恐竜（きょうりゅう）と同（おな）じ時代（じだい）", "古生代（こせいだい）", "中生代（ちゅうせいだい）", "新生代（しんせいだい）"],
  },
  {
    id: "08_index_fossil_eras",
    questionId: "q.fossil_index_term",
    title: "示準化石と地質年代",
    subject: "Index fossils tell the geological age; a timeline shows a representative index fossil for each era.",
    composition:
      "A horizontal geological timeline divided into three eras left to right, each a different earthy band color, each with its representative fossil illustrated above it. " +
      "Paleozoic: a trilobite. Mesozoic: an ammonite with a small dinosaur. Cenozoic: a Vicarya shell with a Naumann elephant.",
    labels: [
      "示準化石（しじゅんかせき）＝時代（じだい）がわかる",
      "古生代（こせいだい） サンヨウチュウ",
      "中生代（ちゅうせいだい） アンモナイト",
      "新生代（しんせいだい） ビカリア・ナウマンゾウ",
    ],
  },
  {
    id: "09_tuff_volcano",
    questionId: "q.tuff_volcano",
    title: "凝灰岩は火山のしるし",
    subject: "A tuff layer (compacted volcanic ash) inside a strata column is evidence of a past volcanic eruption.",
    composition:
      "Right side: a volcano erupting, with an ash plume drifting left and falling. " +
      "Left side: a vertical strata column (columnar section) with several rock layers; one pale distinct layer in the middle is highlighted as the volcanic-ash (tuff) layer, " +
      "with falling ash particles visually connecting the eruption to that layer.",
    labels: ["凝灰岩（ぎょうかいがん）＝火山灰（かざんばい）がおし固（かた）まった層（そう）", "火山（かざん）の噴火（ふんか）のしるし"],
  },
  {
    id: "10_keybed_tilt",
    questionId: "q.strata_tilt",
    title: "鍵層と地層の傾き",
    subject: "Correlating a key bed across three strata columns reveals the tilt of the strata.",
    composition:
      "Three vertical strata columns labeled A, B, C placed left to right at the same scale. " +
      "The same pale volcanic-ash key bed appears in all three but at progressively deeper positions toward the right (east), " +
      "connected by a dashed correlation line that slopes downward to the right, showing the whole strata tilts and sinks toward the east.",
    labels: ["A", "B", "C", "鍵層（かぎそう）（火山灰（かざんばい）の層（そう））", "東（ひがし）ほど深（ふか）い＝東（ひがし）に傾（かたむ）く"],
  },
  {
    id: "11_fold_fault",
    questionId: "q.fold_term",
    title: "しゅう曲と断層",
    subject: "Folding: strata bent into waves by compression, contrasted with a fault where strata slip and are displaced.",
    composition:
      "Main center: horizontal strata layers squeezed by two big arrows from the left and right, bending into smooth wavy folds. " +
      "Small rounded inset at the bottom-right: the same layers cut and slipped along a diagonal fault line, one side shifted up, shown for contrast.",
    labels: ["しゅう曲（きょく）＝おし縮（ちぢ）められて曲（ま）がる", "断層（だんそう）＝ずれる"],
  },
];

const STYLE_BLOCK = `# Visual language (study Image #1 ONLY for palette & line quality)
- Flat, clean educational-illustration style: simple rounded shapes, gentle soft outlines, minimal soft shading.
- Limited earthy palette: warm ochre and brown earth tones, sandy beige, sea blue/teal for water, ONE warm accent (volcanic orange-red). Soft cream / off-white background.
- Cohesive "friendly textbook diagram" feel — clear and legible, not childish, not photorealistic.
- A subtle rounded-rectangle card frame so it reads as a banner overlaid on a game screen.

# Hard rules (override everything else, including the reference image)
- This is a DIAGRAM. Do NOT depict any person, human, character, or face anywhere. Image #1 is ONLY a palette / line-quality reference, never content to copy.
- No watermark, no UI chrome, no extra borders beyond the soft card frame.
- Landscape orientation, about 3:2.`;

export function buildPrompt(spec, targetPath) {
  const labelLines = spec.labels.map((l) => `  - ${l}`).join("\n");
  return `# Task
Generate ONE ${WIDTH}x${HEIGHT} landscape PNG (an educational illustration banner) and save it to:
${targetPath}

# Context
This banner appears in a geology learning game for Japanese middle-school students (中学生).
It sits above a dialogue box while a character explains the concept, then a quiz is asked.
It must teach the concept at a glance and read clearly even when shrunk to a wide strip.

# Subject (WHAT to depict)
${spec.subject}

# Composition (wide landscape, reads left-to-right)
${spec.composition}

# Japanese text labels to render (place as clean captions near the elements they describe)
Use a rounded, friendly Japanese sans-serif. Keep every label crisp and correctly spelled.
The notation 漢字（かな） means the kana in parentheses is the reading (furigana) of the kanji just before it:
render that kana SMALLER, placed above or right beside its kanji — do not drop it, do not enlarge it.
Labels:
${labelLines}

${STYLE_BLOCK}

# Execution plan (FOLLOW EXACTLY)
1. ONE imagegen call at ${WIDTH}x${HEIGHT} landscape. Pass Image #1 as the style/palette reference.
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
