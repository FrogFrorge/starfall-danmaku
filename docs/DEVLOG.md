# Development log

Each round started with my feedback on the latest build; Claude Code then changed the game, tested it with the bot and sent back screenshots or videos. Dates are in Taiwan time (UTC+8).

## Unreal Engine 5.8 (C++)

**6 November: a first playable game.** I asked for a Touhou-like STG in the latest Unreal Engine. The first art was drawn in code, then rendered in Blender, then replaced with AI-generated portraits so that the characters, UI and backgrounds share one style. I asked for different portraits and in-game sprites for the player characters, and for every character to move a little during play.

**6 November: difficulty and recovery.** The first four spell cards were far too dense, so Easy, Normal, Hard and Lunatic were added, boss HP was halved, and stage 2 was based on a p5.js danmaku prototype of mine (four seasons). I asked for Touhou-style life and bomb recovery (pieces), and for an Extra stage.

**6 November: structure and story.** Every stage got its own road, 1–3 minutes long. The last bosses of the main story were connected and explain their motives; an earlier boss (Gen) introduces the Extra stage and warns you to retreat, with three new spell cards; the Extra boss's story fills in the main story; conversations grow longer as the truth comes closer. Thirteen Orphans and the chrysanthemums of 「君子之約」 became pieces that bombs and the death clear can't remove.

**7 November: Lunatic needs to feel like Lunatic.** I asked for stronger roads and non-spells, stronger cards for Gen's fire and metal and Yoh's white tiger, Riichi and Chinitsu to be harder (the next spiral starts before the last one ends), Tenhou to bring back every earlier hand, three seconds of invulnerability at the start of each card, last spells immune to bomb damage, an English version, after-fight conversations, and real sprite sheets instead of wobbling the whole picture.

**7 November: Extra-IF.** My idea for a second Extra route: the hero is too late and the four perils (Taotie, Taowu, Hundun, Qiongqi) break loose. A different beast blocks the road for each character (Haru, a big eater, meets Taotie), each with three cards; the boss fight is a relay like a Touhou Extra stage, ending with all four together. Each boss also got a theme with its own character.

**7 November: tuning.** Character speeds and two traits for each character; spell practice; HP rules for the Extra-IF relay; many card changes (for example Taotie's maw turning each swallowed bullet into three green ones, Taowu's tails rotating, Qiongqi's crescent fans becoming rings). Separate, denser roads for Normal, Hard and Lunatic, and a bot sweep to check that Lunatic cards really are harder than Normal.

## Godot 4.6 (C#)

**9 November: the move to Godot.** I decided Godot suited the game better. The C++ was ported to C# almost line by line, with the same balance and the same deterministic simulation, plus `Play.bat` and `Editor.bat` to start it.

**9 November: presentation.** New speed numbers (Ren 8 / 4, Sou 10 / 5, Haru 9 / 6, Yuki 7 / 3); smaller sprites with the same hitboxes, as in Touhou; clothes that sway; all four beasts' portraits at once for the last two Extra-IF cards; theme prompts for every stage and boss; an options menu, a music room and "back to title" in the pause menu; points and power items at 50% opacity.

**9 November: the writing.** I gave a set of writing rules (natural Taiwanese Mandarin, a reason for every line, no knowledge without a source) and every conversation was rewritten to follow them. Touhou-style endings were added for each character. My AI-generated songs went into `Godot/Music/`.

**9 November: a long round of card changes.** Survival cards make the boss see-through and harmless to touch; a gentler start to the Extra road; the aim lines of Riichi and Arrogance no longer kill; Tenhou in three phases with a stacking title and a timer that starts at a third of its HP; Thirteen Orphans one tile first, then two every five seconds; Big Four Winds as a single volley of four rings at four speeds; and many Extra-IF fixes, including a nerf for Sou's version of the last card.

**10 November: power and unlocks.** Power now adds shots at each level instead of only damage. 「顛倒是非」 was redesigned twice so the boss can actually be attacked. The non-spells before the last two Extra-IF cards were removed. Spell practice and the music room now start locked, Touhou style, with a spoiler warning before an unheard song plays. A Windows build was exported for testing, and bot runs on Lunatic, Extra and Extra-IF were recorded without bombing during spell cards.
