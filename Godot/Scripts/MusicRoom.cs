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
			"標題畫面的曲子，也是整個遊戲的主題。流星劃過山上的神社，天門之夜的前一晚。想寫成一首聽了就想按下「開始遊戲」的曲子。"),
		new(1,  "星光參道", "第一面道中",
			"第一面的道中曲。走上滿天星光的神社石階，旅程才剛開始。輕快、好奇，還不知道星星為什麼一顆一顆在消失。"),
		new(2,  "天狼星詠　～ Star-Chanting Onmyoji", "第一面 Boss　天狼的主題",
			"天狼的主題。讀星辰占卜命運的陰陽師，冷靜、優雅，又帶著一點不祥。旋律像星座一樣一閃一閃，算到最後一顆星時突然轉成小調。"),
		new(3,  "四時繪卷", "第二面道中",
			"第二面的道中曲。走進一幅會動的畫卷，春花、夏日、秋葉、冬雪依序流過，每一段換一種季節的顏色。"),
		new(4,  "千秋萬色　～ Painter of Four Seasons", "第二面 Boss　千秋的主題",
			"千秋的主題。一筆一劃都化成彈幕的畫師，為了畫完他的四季，連山下的稻田都凍壞了。美麗，但下筆毫不留情；中段轉調，像是翻到下一個季節。"),
		new(5,  "雲上之路", "第三面道中",
			"第三面的道中曲。在雲海上方飛行，地平線那頭的烏雲正在聚集，雨一直下不停。越往後越緊張。"),
		new(6,  "嵐之支配者", "第三面 Boss　嵐的主題",
			"嵐的主題。晴、雨、雷、霧全聽他號令的天氣師，脾氣比雷還急。這遊戲裡最吵的一首，太鼓和電吉他一起打雷。"),
		new(7,  "四君子庭園", "第四面道中",
			"第四面的道中曲。月光下的四君子庭園，梅、蘭、竹、菊。表面像水墨畫一樣安靜，底下的節奏卻一直在催。"),
		new(8,  "墨竹清風　～ Ink and Bamboo", "第四面 Boss　墨竹的主題",
			"墨竹的主題。守著庭園的驕傲書生，筆鋒就是刀鋒。前半端正得像一篇楷書，後半忍不住露出鋒芒。"),
		new(9,  "五行仙壇", "第五面道中",
			"第五面的道中曲。登上五行仙壇，木、火、土、金、水在身邊打轉。旋律每一段都交給代表一種元素的樂器。"),
		new(10, "玄之又玄", "第五面 Boss　玄的主題",
			"玄的主題。守著天宮祕密的方士，知道的比他說出口的多。沉重、嚴肅，同一個動機繞過五種音色，像五行相生。"),
		new(11, "天宮", "第六面道中",
			"第六面的道中曲。天上宮殿的大門，旅程的終點。莊嚴又有點寂寞，好像這座宮殿很久沒有客人了。"),
		new(12, "四象歸一　～ Guardian of the Four Symbols", "第六面 Boss　燁的主題",
			"燁的主題，最終 Boss。一個人守了結界太久的四神守護者。青龍、白虎、朱雀、玄武四段各不相同，最後合而為一；英勇裡帶著一點疲憊和悲傷。"),
		new(13, "雀莊・天和閣", "Extra 道中",
			"Extra 的道中曲。天宮後面的豪華雀莊・天和閣，燈籠搖晃、洗牌聲不斷。爵士風的二胡，有點狡猾地越跑越快。"),
		new(14, "役滿的雀聖　～ Yakuman Saint", "Extra Boss　一索的主題",
			"一索的主題。五百年前從燁手上贏走十三枚天印、從來不輸的雀聖。最炫、最快、最愛現的一首，每個突然的停頓都是他亮牌的瞬間。"),
		new(15, "崩落天宮・四凶之淵", "Extra-IF 道中",
			"Extra-IF 的道中曲。如果來不及的話——結界碎了，天宮崩塌，天空裂開紅色的縫。黑暗、沉重，但還是得往前走。"),
		new(16, "四凶　～ The Four Perils", "Extra-IF Boss　四凶的主題",
			"四凶的主題。被四神鎮壓了幾千年的上古凶獸：饕餮、檮杌、混沌、窮奇。貪食、不馴、無面、展翼四個動機輪流登場，最後全部疊在一起。"),
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
		("標題畫面的曲子，也是整個遊戲的主題。流星劃過山上的神社，天門之夜的前一晚。想寫成一首聽了就想按下「開始遊戲」的曲子。",
			"The title theme, and the theme of the whole game. Shooting stars over a mountain shrine on the eve of the Night of the Heaven's Gate. A tune that makes you want to press Start."),
		("第一面的道中曲。走上滿天星光的神社石階，旅程才剛開始。輕快、好奇，還不知道星星為什麼一顆一顆在消失。",
			"The stage 1 theme. Climbing the shrine's stone steps under a starry sky, at the very start of the journey. Light and curious; nobody knows yet why the stars are going out one by one."),
		("天狼的主題。讀星辰占卜命運的陰陽師，冷靜、優雅，又帶著一點不祥。旋律像星座一樣一閃一閃，算到最後一顆星時突然轉成小調。",
			"Tenrou's theme. An onmyoji who reads fate in the stars: calm, elegant and a little ominous. The melody twinkles like a constellation, then turns minor when the reading reaches its last star."),
		("第二面的道中曲。走進一幅會動的畫卷，春花、夏日、秋葉、冬雪依序流過，每一段換一種季節的顏色。",
			"The stage 2 theme. Walking into a scroll painting that moves: spring blossoms, summer sun, autumn leaves and winter snow drift past, each section in a new season's colour."),
		("千秋的主題。一筆一劃都化成彈幕的畫師，為了畫完他的四季，連山下的稻田都凍壞了。美麗，但下筆毫不留情；中段轉調，像是翻到下一個季節。",
			"Chiaki's theme. A painter whose every stroke becomes danmaku, so set on finishing his four seasons that the rice fields below froze. Beautiful but merciless with the brush; the key change in the middle turns the page to the next season."),
		("第三面的道中曲。在雲海上方飛行，地平線那頭的烏雲正在聚集，雨一直下不停。越往後越緊張。",
			"The stage 3 theme. Flying over a sea of clouds while storm clouds gather on the horizon and the rain refuses to stop. It gets tenser the further you go."),
		("嵐的主題。晴、雨、雷、霧全聽他號令的天氣師，脾氣比雷還急。這遊戲裡最吵的一首，太鼓和電吉他一起打雷。",
			"Arashi's theme. A weather-master who commands sun, rain, thunder and fog, with a temper quicker than lightning. The loudest track in the game: taiko and electric guitar thunder together."),
		("第四面的道中曲。月光下的四君子庭園，梅、蘭、竹、菊。表面像水墨畫一樣安靜，底下的節奏卻一直在催。",
			"The stage 4 theme. The Garden of the Four Gentlemen by moonlight: plum, orchid, bamboo and chrysanthemum. Calm as an ink painting on the surface, with a rhythm underneath that keeps pushing."),
		("墨竹的主題。守著庭園的驕傲書生，筆鋒就是刀鋒。前半端正得像一篇楷書，後半忍不住露出鋒芒。",
			"Mochiku's theme. The proud scholar who guards the garden, his brush as sharp as a blade. The first half is as proper as formal calligraphy; the second can't hide its edge."),
		("第五面的道中曲。登上五行仙壇，木、火、土、金、水在身邊打轉。旋律每一段都交給代表一種元素的樂器。",
			"The stage 5 theme. Climbing the Altar of the Five Elements as wood, fire, earth, metal and water swirl around you. Each section hands the melody to an instrument for one element."),
		("玄的主題。守著天宮祕密的方士，知道的比他說出口的多。沉重、嚴肅，同一個動機繞過五種音色，像五行相生。",
			"Gen's theme. The sorcerer who keeps the Heavenly Palace's secrets and knows more than he says. Heavy and serious; one motif cycles through five tone colours, like the five elements generating one another."),
		("第六面的道中曲。天上宮殿的大門，旅程的終點。莊嚴又有點寂寞，好像這座宮殿很久沒有客人了。",
			"The stage 6 theme. The gates of the palace in the sky, the end of the journey. Solemn and a little lonely, as if the palace hasn't had a visitor in a very long time."),
		("燁的主題，最終 Boss。一個人守了結界太久的四神守護者。青龍、白虎、朱雀、玄武四段各不相同，最後合而為一；英勇裡帶著一點疲憊和悲傷。",
			"Yoh's theme, the final boss. The guardian of the Four Gods, who has held the barrier alone for far too long. Four sections for the azure dragon, white tiger, vermilion bird and black tortoise finally merge into one; heroic, with a touch of weariness and sorrow."),
		("Extra 的道中曲。天宮後面的豪華雀莊・天和閣，燈籠搖晃、洗牌聲不斷。爵士風的二胡，有點狡猾地越跑越快。",
			"The Extra stage theme. Tenhou Pavilion, the lavish mahjong parlour behind the Heavenly Palace, with swaying lanterns and the endless clatter of shuffled tiles. Jazzy erhu that slyly keeps speeding up."),
		("一索的主題。五百年前從燁手上贏走十三枚天印、從來不輸的雀聖。最炫、最快、最愛現的一首，每個突然的停頓都是他亮牌的瞬間。",
			"Isso's theme. The mahjong saint who never loses, who won thirteen heavenly seals from Yoh five hundred years ago. The flashiest, fastest, most show-off track; every sudden break is the moment he reveals his hand."),
		("Extra-IF 的道中曲。如果來不及的話——結界碎了，天宮崩塌，天空裂開紅色的縫。黑暗、沉重，但還是得往前走。",
			"The Extra-IF stage theme. What if you were too late? The barrier is broken, the Heavenly Palace has fallen, and the sky is cracked with red. Dark and heavy, but you still have to go on."),
		("四凶的主題。被四神鎮壓了幾千年的上古凶獸：饕餮、檮杌、混沌、窮奇。貪食、不馴、無面、展翼四個動機輪流登場，最後全部疊在一起。",
			"Theme of the Four Perils, ancient beasts held down by the Four Gods for thousands of years: Taotie, Taowu, Hundun and Qiongqi. Four motifs (gluttony, defiance, facelessness and wings) take turns, then all pile on at once."),
	};
}
