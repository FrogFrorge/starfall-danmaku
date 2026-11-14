// Music room and options: the track list (names + short comments, Touhou style), the saved volume settings,
// and English for the strings added here (merged into Tr()'s table by Types.cs).
using Godot;

namespace DanmakuSTG;

public readonly record struct FMusicTrack(int Track, string Name, string Where, string Comment);

public static partial class Danmaku
{
	/** Track ids match Audio.cs: 0 title, 1 + 2*Stage road theme, 2 + 2*Stage boss theme. */
	public static readonly FMusicTrack[] MusicTracks =
	{
		new(0,  "星墜彈幕　～ Starfall Overture", "標題畫面",
			"天門之夜的前一晚，山上神社的天空開始掉星星。這是整個故事的主題。村子裡的人都還在睡，只有四個少年抬頭看見了。"),
		new(1,  "星光參道", "第一面道中",
			"通往神社的石階，每一階都映著星光。出發的夜晚總是有點興奮，也有點不安：天上明明還有這麼多星星，為什麼一顆接一顆不見了？"),
		new(2,  "天狼星詠　～ Star-Chanting Onmyoji", "第一面 Boss　天狼的主題",
			"天狼的主題。他把整片星空借走，只為了讀懂一句預言。他不是壞人，只是太想知道命運的答案，忘了星星也是大家的。"),
		new(3,  "四時繪卷", "第二面道中",
			"走進畫卷裡的路。前一步還是春天，下一步已經落雪。美得讓人想停下來看，可是季節亂了，山下的田正在受苦。"),
		new(4,  "千秋萬色　～ Painter of Four Seasons", "第二面 Boss　千秋的主題",
			"千秋的主題。一位想把四季畫到完美的畫師。他眼裡只有那幅畫，看不見畫外的人；直到有人站到他面前，他才想起，四季本來就是給人過的。"),
		new(5,  "雲上之路", "第三面道中",
			"雲海上的路。雨一直下，雷就打在身邊。明明已經飛到雲的上面了，暴風雨卻像是在後面追著人跑。"),
		new(6,  "嵐之支配者", "第三面 Boss　嵐的主題",
			"嵐的主題。天氣師的脾氣說變就變，就像夏天午後的雷陣雨。可是暴風雨過後，天空總是特別乾淨。"),
		new(7,  "四君子庭園", "第四面道中",
			"月光下的庭園，梅、蘭、竹、菊各守一角。安靜得像一幅水墨畫，可是每一片葉子都在看著你。這座庭園不歡迎客人。"),
		new(8,  "墨竹清風　～ Ink and Bamboo", "第四面 Boss　墨竹的主題",
			"墨竹的主題。一位驕傲的書生，每一個字都要寫得端端正正。他守了一輩子的規矩，所以最怕的，就是有人告訴他規矩錯了。"),
		new(9,  "五行仙壇", "第五面道中",
			"通往天宮前的最後一座仙壇。木生火、火生土，腳下的每一步都在輪轉。越往上爬，越覺得好像有人一直在等你來。"),
		new(10, "玄之又玄", "第五面 Boss　玄的主題",
			"玄的主題。知道真相、卻一個字也不能說的方士。他攔住你，不是因為討厭你，而是因為他知道上面有什麼在等著。"),
		new(11, "天宮", "第六面道中",
			"天上的宮殿。金碧輝煌，卻安靜得聽不到一點聲音。這裡很久很久沒有客人了，只有一個人一直守在這裡。"),
		new(12, "四象歸一　～ Guardian of the Four Symbols", "第六面 Boss　燁的主題",
			"燁的主題，最終 Boss。四神的守護者，一個人撐著結界撐了太久。他很強，也很累。這是他最後的力氣，也是他終於可以休息的那一刻。"),
		new(13, "雀莊・天和閣", "Extra 道中",
			"天宮後面的雀莊。燈籠一盞一盞亮著，洗牌的聲音從來沒停過。熱鬧得不像天界，好像每個人都在等著看下一手牌。"),
		new(14, "役滿的雀聖　～ Yakuman Saint", "Extra Boss　一索的主題",
			"一索的主題。愛笑、愛賭、從來沒輸過的雀聖。五百年來，他替老朋友保管著十三枚天印，等的不是一場勝利，而是一個能讓燁放心交出重擔的人。"),
		new(15, "崩落天宮・四凶之淵", "Extra-IF 道中",
			"如果，主角來不及的話。結界碎了，天宮塌了，天空裂開紅色的縫。熟悉的路變成了廢墟，可是燁還在上面，只能往前走。"),
		new(16, "四凶　～ The Four Perils", "Extra-IF Boss　四凶的主題",
			"四凶的主題。被壓在結界底下幾千年的上古凶獸。他們很兇，也很寂寞：從來沒有人問過他們想要什麼。這一夜，他們終於能大聲說話。"),
	};

	// ---- settings (user://settings.cfg, next to the language) ----
	static int GMusicVol = -1, GSfxVol = -1;

	static int LoadSetting(string Key, int Default)
	{
		var Cfg = new ConfigFile();
		return Cfg.Load("user://settings.cfg") == Error.Ok ? (int)Cfg.GetValue("Danmaku", Key, Default) : Default;
	}

	static void SaveSetting(string Key, int Value)
	{
		var Cfg = new ConfigFile();
		Cfg.Load("user://settings.cfg");
		Cfg.SetValue("Danmaku", Key, Value);
		Cfg.Save("user://settings.cfg");
	}

	/** Music / sound-effect volume, 0..10 (10 = 100%). */
	public static int GetMusicVolume() { if (GMusicVol < 0) GMusicVol = FMath.Clamp(LoadSetting("MusicVolume", 8), 0, 10); return GMusicVol; }
	public static int GetSfxVolume() { if (GSfxVol < 0) GSfxVol = FMath.Clamp(LoadSetting("SfxVolume", 8), 0, 10); return GSfxVol; }
	public static void SetMusicVolume(int V) { GMusicVol = FMath.Clamp(V, 0, 10); SaveSetting("MusicVolume", GMusicVol); }
	public static void SetSfxVolume(int V) { GSfxVol = FMath.Clamp(V, 0, 10); SaveSetting("SfxVolume", GSfxVol); }

	/** English for the strings added in the Godot version (merged into the main table). */
	static readonly (string Zh, string En)[] GTableExtra =
	{
		("音樂室", "Music Room"),
		("選項", "Options"),
		("音樂音量", "Music Volume"),
		("音效音量", "Sound Effects"),
		("返回", "Back"),
		("繼續", "Resume"),
		("重來", "Restart"),
		("回到主選單", "Back to Title"),
		("↑　↓　選擇　　Z　播放　　X　返回", "↑ ↓  Select    Z  Play    X  Back"),
		("↑　↓　選擇　　←　→　調整　　X　返回", "↑ ↓  Select    ← →  Adjust    X  Back"),
		("↑　↓　選擇　　Z　決定　　P　繼續", "↑ ↓  Select    Z  Confirm    P  Resume"),
		("播放中", "Now playing"),
		("這首曲子你還沒在遊戲裡聽過，曲名和介紹可能會劇透接下來的內容。確定要聽嗎？",
			"You haven't heard this song in the game yet. Its title and notes may spoil what's ahead. Listen anyway?"),
		("還沒在遊戲裡聽過的曲子。", "A song you haven't heard in the game yet."),
		("Z　聽　　X　取消", "Z  Listen    X  Cancel"),
		("Z　還是想聽", "Z  Listen anyway"),
		("標題畫面", "Title Screen"),
		("第一面道中", "Stage 1 Theme"), ("第二面道中", "Stage 2 Theme"), ("第三面道中", "Stage 3 Theme"),
		("第四面道中", "Stage 4 Theme"), ("第五面道中", "Stage 5 Theme"), ("第六面道中", "Stage 6 Theme"),
		("Extra 道中", "Extra Stage Theme"), ("Extra-IF 道中", "Extra-IF Stage Theme"),
		("第一面 Boss　天狼的主題", "Stage 1 Boss: Tenrou's Theme"),
		("第二面 Boss　千秋的主題", "Stage 2 Boss: Chiaki's Theme"),
		("第三面 Boss　嵐的主題", "Stage 3 Boss: Arashi's Theme"),
		("第四面 Boss　墨竹的主題", "Stage 4 Boss: Mochiku's Theme"),
		("第五面 Boss　玄的主題", "Stage 5 Boss: Gen's Theme"),
		("第六面 Boss　燁的主題", "Stage 6 Boss: Yoh's Theme"),
		("Extra Boss　一索的主題", "Extra Boss: Isso's Theme"),
		("Extra-IF Boss　四凶的主題", "Extra-IF Boss: Theme of the Four Perils"),
		("星墜彈幕　～ Starfall Overture", "Starfall Danmaku ~ Starfall Overture"),
		("星光參道", "Starlit Shrine Path"),
		("天狼星詠　～ Star-Chanting Onmyoji", "Song of Sirius ~ Star-Chanting Onmyoji"),
		("四時繪卷", "Scroll of the Four Seasons"),
		("千秋萬色　～ Painter of Four Seasons", "A Thousand Autumns of Colour ~ Painter of Four Seasons"),
		("雲上之路", "Road Above the Clouds"),
		("嵐之支配者", "Ruler of the Storm"),
		("四君子庭園", "Garden of the Four Gentlemen"),
		("墨竹清風　～ Ink and Bamboo", "Clear Wind Through Ink Bamboo ~ Ink and Bamboo"),
		("五行仙壇", "Altar of the Five Elements"),
		("玄之又玄", "Mystery Upon Mystery"),
		("天宮", "The Heavenly Palace"),
		("四象歸一　～ Guardian of the Four Symbols", "Four Symbols Become One ~ Guardian of the Four Symbols"),
		("雀莊・天和閣", "Mahjong Parlour: Tenhou Pavilion"),
		("役滿的雀聖　～ Yakuman Saint", "Saint of the Yakuman ~ Yakuman Saint"),
		("崩落天宮・四凶之淵", "Collapsed Heavenly Palace: Abyss of the Four Perils"),
		("四凶　～ The Four Perils", "The Four Perils"),
		("天門之夜的前一晚，山上神社的天空開始掉星星。這是整個故事的主題。村子裡的人都還在睡，只有四個少年抬頭看見了。",
			"The night before the Night of the Heaven's Gate, stars begin to fall over the mountain shrine. This is the theme of the whole story. The village sleeps on; only four young men look up and see it."),
		("通往神社的石階，每一階都映著星光。出發的夜晚總是有點興奮，也有點不安：天上明明還有這麼多星星，為什麼一顆接一顆不見了？",
			"Stone steps up to the shrine, each one lit by starlight. A night of setting out is always a little exciting and a little uneasy: with so many stars still in the sky, why are they vanishing one by one?"),
		("天狼的主題。他把整片星空借走，只為了讀懂一句預言。他不是壞人，只是太想知道命運的答案，忘了星星也是大家的。",
			"Tenrou's theme. He borrowed the whole starry sky just to understand a single prophecy. He isn't wicked; he wanted fate's answer so badly that he forgot the stars belong to everyone."),
		("走進畫卷裡的路。前一步還是春天，下一步已經落雪。美得讓人想停下來看，可是季節亂了，山下的田正在受苦。",
			"A road inside a painted scroll. One step is spring, the next is falling snow. Beautiful enough to make you stop and look, but the seasons are out of order and the fields below are suffering."),
		("千秋的主題。一位想把四季畫到完美的畫師。他眼裡只有那幅畫，看不見畫外的人；直到有人站到他面前，他才想起，四季本來就是給人過的。",
			"Chiaki's theme. A painter who wants to paint the four seasons perfectly. He sees only the painting, never the people outside it, until someone stands in front of him and he remembers that seasons are there to be lived in."),
		("雲海上的路。雨一直下，雷就打在身邊。明明已經飛到雲的上面了，暴風雨卻像是在後面追著人跑。",
			"A road above the sea of clouds. The rain never stops and thunder strikes close by. You've flown higher than the clouds, yet the storm seems to be chasing you."),
		("嵐的主題。天氣師的脾氣說變就變，就像夏天午後的雷陣雨。可是暴風雨過後，天空總是特別乾淨。",
			"Arashi's theme. The weather-master's temper turns in an instant, like a summer afternoon thunderstorm. But after the storm, the sky is always especially clear."),
		("月光下的庭園，梅、蘭、竹、菊各守一角。安靜得像一幅水墨畫，可是每一片葉子都在看著你。這座庭園不歡迎客人。",
			"A garden by moonlight, with plum, orchid, bamboo and chrysanthemum each guarding a corner. Quiet as an ink painting, yet every leaf is watching you. This garden does not welcome guests."),
		("墨竹的主題。一位驕傲的書生，每一個字都要寫得端端正正。他守了一輩子的規矩，所以最怕的，就是有人告訴他規矩錯了。",
			"Mochiku's theme. A proud scholar who writes every character just so. He has kept the rules all his life, so what he fears most is someone telling him the rules were wrong."),
		("通往天宮前的最後一座仙壇。木生火、火生土，腳下的每一步都在輪轉。越往上爬，越覺得好像有人一直在等你來。",
			"The last altar before the Heavenly Palace. Wood feeds fire, fire makes earth, and every step turns the wheel. The higher you climb, the more it feels like someone has been waiting for you."),
		("玄的主題。知道真相、卻一個字也不能說的方士。他攔住你，不是因為討厭你，而是因為他知道上面有什麼在等著。",
			"Gen's theme. A sorcerer who knows the truth but may not say a word of it. He stands in your way not because he dislikes you, but because he knows what is waiting above."),
		("天上的宮殿。金碧輝煌，卻安靜得聽不到一點聲音。這裡很久很久沒有客人了，只有一個人一直守在這裡。",
			"The palace in the sky. All gold and splendour, and not a single sound. No guest has come here in a very, very long time; only one person has kept watch."),
		("燁的主題，最終 Boss。四神的守護者，一個人撐著結界撐了太久。他很強，也很累。這是他最後的力氣，也是他終於可以休息的那一刻。",
			"Yoh's theme, the final boss. The guardian of the Four Gods, who has held up the barrier alone for far too long. He is strong, and very tired. This is his last strength, and the moment he can finally rest."),
		("天宮後面的雀莊。燈籠一盞一盞亮著，洗牌的聲音從來沒停過。熱鬧得不像天界，好像每個人都在等著看下一手牌。",
			"A mahjong parlour behind the Heavenly Palace. Lanterns glow one after another and the shuffle of tiles never stops. Too lively for heaven, as if everyone is waiting to see the next hand."),
		("一索的主題。愛笑、愛賭、從來沒輸過的雀聖。五百年來，他替老朋友保管著十三枚天印，等的不是一場勝利，而是一個能讓燁放心交出重擔的人。",
			"Isso's theme. A mahjong saint who loves to laugh, loves to gamble and has never lost. For five hundred years he has kept thirteen Heaven Seals safe for an old friend, waiting not for a win but for someone Yoh can trust with his burden."),
		("如果，主角來不及的話。結界碎了，天宮塌了，天空裂開紅色的縫。熟悉的路變成了廢墟，可是燁還在上面，只能往前走。",
			"What if the hero was too late? The barrier is shattered, the palace has fallen and the sky is split with red. The road you knew lies in ruins, but Yoh is still up there, so the only way is forward."),
		("四凶的主題。被壓在結界底下幾千年的上古凶獸。他們很兇，也很寂寞：從來沒有人問過他們想要什麼。這一夜，他們終於能大聲說話。",
			"Theme of the Four Perils, ancient beasts held under the barrier for thousands of years. They are fierce, and lonely too: no one ever asked them what they wanted. Tonight, at last, they can speak out loud."),
	};
}
