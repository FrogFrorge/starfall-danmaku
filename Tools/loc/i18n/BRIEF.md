# Translation brief: Starfall Danmaku (星墜彈幕)

A Touhou-style vertical bullet-hell shooter. `source.json` holds every string the game shows: `id`, `zh`
(Traditional Chinese, the original) and `en` (the English, which is the game's main language). Translate into
Japanese (`ja`) or French (`fr`). Use both the Chinese and the English: the Chinese is the original meaning, the
English shows the tone and the length that fits the screen.

## The story in short
On the Night of the Heaven's Gate, four young men climb to the Heavenly Palace: they chase a star-reader who
stole the stars, a painter who broke the seasons, a weather-master, a proud scholar, a cryptic sorcerer and,
at the top, Yoh, the tired guardian of the Four Gods who has held the barrier alone too long. The Extra stage is
Isso's mahjong parlour: Isso is a cheerful mahjong saint and a GOOD person who has kept Yoh's thirteen Heaven
Seals safe for 500 years. Extra-IF: the hero arrives too late, the barrier breaks, and the Four Perils (ancient
beasts from Chinese myth, drawn as young men) are loose. They are the villains, fierce but lonely.

Player characters (speaker 0 in conversations):
- 蓮 Ren: the village shrine's young onmyoji, earnest, hot-headed, says what he thinks.
- 蒼 Sou: an archer/hunter of very few words; short, blunt sentences.
- 晴 Haru: a wandering yamabushi, cheerful, always hungry, uses "~".
- 雪 Yuki: a quiet, polite puppeteer; his dolls ("the children", 孩子們) sense what people can't.

## Names (always the same)
| zh | en / fr | ja |
|---|---|---|
| 蓮 / 蒼 / 晴 / 雪 | Ren / Sou / Haru / Yuki | 蓮 / 蒼 / 晴 / 雪 |
| 天狼 | Tenrou | 天狼 |
| 千秋 | Chiaki | 千秋 |
| 嵐 | Arashi | 嵐 |
| 墨竹 | Mochiku | 墨竹 |
| 玄 | Gen | 玄 |
| 燁 | Yoh | 燁 |
| 一索 | Isso (some old English says "Iisou": always write Isso) | 一索 |
| 饕餮 / 檮杌 / 混沌 / 窮奇 | Taotie / Taowu / Hundun / Qiongqi | 饕餮 / 檮杌 / 混沌 / 窮奇 |
| 四凶 | the Four Perils / les Quatre Fléaux | 四凶 |
| 天門之夜 | Night of the Heaven's Gate / la Nuit de la Porte céleste | 天門の夜 |
| 天宮 | Heavenly Palace / le Palais céleste | 天宮 |
| 結界 | barrier / la barrière | 結界 |
| 天印 | Heaven Seals / les Sceaux célestes | 天印 |
| 天和閣 | Tenhou Pavilion / le Pavillon Tenhou | 天和閣 |
| 四神 | the Four Gods / les Quatre Dieux | 四神 |

## Game terms
| zh | en | fr | ja |
|---|---|---|---|
| 符卡 | spell card | carte de sort | スペルカード |
| 靈符 (bomb) | Bomb | Bombe | ボム |
| 殘機 | Lives | Vies | 残機 |
| 靈力 | Power | Puissance | 霊力 |
| 擦彈 | Graze | Frôlement | グレイズ |
| 碎片 | pieces | fragments | かけら |
| 道中 | stage / road | parcours | 道中 |
| 低速 | focused | ralenti | 低速 |
| 判定 | hitbox | zone d'impact | 当たり判定 |
| 耐久符卡 | survival card | carte d'endurance | 耐久スペル |
| 最終符卡 | last spell | dernière carte | ラストスペル |
| Easy / Normal / Hard / Lunatic | keep in English in every language |
| Extra / Extra-IF | keep as is |

## Rules
- Spell card names keep their shape. Chinese/Japanese: 日輪「赫耀天環」. For **ja**, keep the kanji names
  (Touhou style), converted to Japanese shinjitai where they differ (靈→霊, 劍→剣, 歸→帰…); do not translate them
  into kana. For **fr**, follow the English structure with French quotes: `Roue solaire « Anneau céleste éclatant »`
  (sign word, space, « name »).
- Keep every key name, number and symbol exactly: Z, X, P, R, Shift, WASD, ↑ ↓ ← →, ×1.5, 20%, 60 s, Boss.
- UI strings (menus, labels, help lines) must stay about as short as the English. Help lines with several keys:
  **fr** copies the English spacing (two spaces between groups); **ja** copies the Chinese full-width spacing (　).
- Dialogue must sound like natural speech from that character, not a literal translation. ja: Ren casual and
  direct (俺), Sou terse (俺, very short), Haru bright and childish (ぼく, 〜), Yuki soft and polite (ぼく, です・ます);
  bosses each in their own voice (Tenrou elegant, Arashi rough, Mochiku stiff and formal, Gen cryptic, Yoh solemn,
  Isso playful, the beasts arrogant). fr: tu between characters, natural spoken French.
- Use the typographic conventions of each language: fr uses « », a space before ! ? : ; and ’ for apostrophes is
  fine; ja uses 「」, 『』, ！？ full-width, …… for pauses.
- Endings are narration in past tense, gentle and literary.
- Never leave a string empty. If the English is empty, translate from the Chinese.
