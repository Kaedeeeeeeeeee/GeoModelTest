# Fixed quiz choice order

Version: `story-formative-v2-fixed-order`. Choices were shuffled once during authoring (seed 20260925), with correct positions balanced across 1/2/3 (4/4/3 questions) and saved in the story JSON. No runtime randomization. All players, languages, retries and reloads use these arrays. Dialogue/question order is unchanged to preserve the teaching sequence. Stable question IDs, choice IDs, correctness and feedback are unchanged; the version distinguishes new observations from v1. Historical records are not migrated.

| Resource | Question ID | Display order (stable choice IDs) | Correct position (1-based) |
|---|---|---|---|
| beat2 | q.rock_mudstone | q.rock_mudstone.conglomerate, q.rock_mudstone.sandstone, q.rock_mudstone.mudstone | 3 |
| beat2 | q.rock_limestone | q.rock_limestone.tuff, q.rock_limestone.limestone, q.rock_limestone.chert | 2 |
| beat2 | q.rock_chert | q.rock_chert.chert, q.rock_chert.conglomerate, q.rock_chert.mudstone | 1 |
| beat3 | q.fossil_coral_env | q.fossil_coral_env.deep_cold_sea, q.fossil_coral_env.warm_shallow_sea, q.fossil_coral_env.brackish_water | 2 |
| beat3 | q.fossil_facies_term | q.fossil_facies_term.index_fossil, q.fossil_facies_term.facies_fossil | 2 |
| beat3 | q.fossil_ammonite_era | q.fossil_ammonite_era.cenozoic, q.fossil_ammonite_era.paleozoic, q.fossil_ammonite_era.mesozoic | 3 |
| beat3 | q.fossil_index_term | q.fossil_index_term.index_fossil, q.fossil_index_term.facies_fossil | 1 |
| beat3 | q.tuff_volcano | q.tuff_volcano.river, q.tuff_volcano.volcano, q.tuff_volcano.glacier | 2 |
| beat4 | q.strata_tilt | q.strata_tilt.north, q.strata_tilt.west, q.strata_tilt.east | 3 |
| beat4 | q.fold_term | q.fold_term.fold, q.fold_term.unconformity, q.fold_term.fault | 1 |
| quest1.2 | q.weathering_order | q.weathering_order.correct_sequence, q.weathering_order.reverse_sequence, q.weathering_order.mixed_sequence | 1 |
