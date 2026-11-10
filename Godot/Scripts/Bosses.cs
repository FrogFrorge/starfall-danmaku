// Port of DanmakuBosses.cpp.
// Boss / stage table. One entry per stage: boss name, stage title, phases and the pre-fight dialogue.
// Phase = { HP, seconds, is spell, spell name, pattern, survival card, minimum difficulty }.
// Dialogue has one conversation per player character (蓮, 蒼, 晴, 雪); speaker 0 = player, 1 = boss.
//
// Story: the 四神結界 that protects the land will break on the coming 天門之夜. Its guardian 燁 is fading and can't
// leave the heavenly palace. The barrier's power only follows a victor, so he sent dreams ("天啟") to five masters,
// telling each to gather one power — stars, seasons, weather, plant spirit, the five elements. Their gathering caused
// the incidents, and whoever beats them carries those powers up to the palace. The conversations get longer and closer
// to the truth stage by stage. Extra: 一索 won the barrier's 13 seal stones (天印) from 燁 500 years ago — the very
// first crack in the barrier — and kept them so the guardian would finally look for help instead of burning out alone.
//
// Personalities
//   蓮 Ren  : hot-blooded, righteous, charges in       蒼 Sou  : cool, terse, sharp-tongued
//   晴 Haru : easygoing, cheerful, always hungry        雪 Yuki : soft-spoken, polite, talks to his dolls
//   天狼 theatrical star-reader / 千秋 whimsical painter / 嵐 hot-headed weather master /
//   墨竹 stiff, formal scholar / 玄 cryptic sorcerer / 燁 solemn final guardian / 一索 playful gambler

namespace DanmakuSTG;

public partial class DanmakuWorld
{
	public static FBossDef FindBossDef(int Stage)
	{
		// the conversations live in Story*.cs; written into these tables the first time any is looked up
		Story.Apply(FindBossDefRaw);
		return FindBossDefRaw(Stage);
	}

	static FBossDef FindBossDefRaw(int Stage)
	{
		switch (Stage)
		{
		case 0: return BossTable.Stage1;
		case 1: return BossTable.Stage2;
		case 2: return BossTable.Stage3;
		case 3: return BossTable.Stage4;
		case 4: return BossTable.Stage5;
		case 5: return BossTable.Stage6;
		case DanmakuWorld.ExtraStage: return BossTable.StageEx;
		case DanmakuWorld.IfStage: return BossTable.StageIf;
		default: return null;
		}
	}

	// The C++ function-local statics. A nested static class keeps them lazy (built on the first FindBossDef call) and
	// initialised in textual order, so every def that references another (StageEx -> StageExMid, StageIf -> the four
	// character midbosses) comes after the one it references.
	static class BossTable
	{

		// ------------------------------------------------------------------ stage 1
		public static readonly FBossDef Stage1 = new FBossDef
		{
			Name = "天狼", Title = "星詠陰陽師", StageTitle = "第一面", StageSub = "星光參道",
			Phases = new()
			{
				new(500f, 30, false, "", static (w, t) => w.Pattern_NonSpell1(t)),
				new(800f, 45, true, "日輪「赫耀天環」", static (w, t) => w.Pattern_Sun(t)),
				new(600f, 35, false, "", static (w, t) => w.Pattern_NonSpell2(t)),
				new(850f, 50, true, "月蝕「月面反照」", static (w, t) => w.Pattern_Moon(t)),
				new(850f, 50, true, "星雨「天河崩落」", static (w, t) => w.Pattern_Star(t)),
				new(1300f, 60, true, "日月星奧義「終夜天象」", static (w, t) => w.Pattern_Final(t)),
			},
			Dialogue = new List<FDialogueLine>[]
			{
				new()
				{   // 蓮
					new(1, "神社派來的，竟是個追著星星跑的少年。"),
					new(0, "村子上空的星座全都熄滅了。是你做的吧？"),
					new(1, "我只是借來占卜。星象說，七夜之後「天門」將開……我必須看清楚。"),
					new(0, "管你什麼天門，先把星星還回來！"),
					new(1, "好大的口氣。就讓我看看，你的命運能否熬過這片星圖！"),
				},
				new()
				{   // 蒼
					new(1, "又一個來找星星的？這次是個帶弓的。"),
					new(0, "星座熄了，村裡的占卜全亂了。還回來。"),
					new(1, "夜裡有聲音要我讀星。天門將開，我得把整片夜空攤開來看。"),
					new(0, "話真多。我只需要一箭。"),
					new(1, "那就看看你的箭，能不能射穿這片星圖！"),
				},
				new()
				{   // 晴
					new(1, "山伏？深夜爬到參道上，不怕迷路嗎？"),
					new(0, "星星都不見了，我連回家的方向都看不到啦～"),
					new(1, "我借來占卜罷了。夢裡有個聲音說，天門將開，要我讀盡星象。"),
					new(0, "夢裡的聲音？那你有沒有夢到晚飯？"),
					new(1, "……真是奇怪的傢伙。也罷，讓星圖決定你的命運！"),
				},
				new()
				{   // 雪
					new(1, "提著人偶的少年……你也是神社派來的？"),
					new(0, "星光消失之後，孩子們一直在哭。……對吧，小紙？"),
					new(1, "有聲音在夢裡告訴我：天門將開，要我看清所有星辰。"),
					new(0, "那麼，請先把星星還給天空。孩子們……準備好了。"),
					new(1, "好啊，讓我看看人偶的命運，熬不熬得過這片星圖！"),
				},
			},
		};

		// ------------------------------------------------------------------ stage 2
		public static readonly FBossDef Stage2 = new FBossDef
		{
			Name = "千秋", Title = "四季流轉的畫師", StageTitle = "第二面", StageSub = "四時繪卷",
			Phases = new()
			{
				new(450f, 30, false, "", static (w, t) => w.S2_NonSpell1(t)),
				new(750f, 45, true, "春符「春風催生」", static (w, t) => w.S2_Spring(t)),
				new(500f, 30, false, "", static (w, t) => w.S2_NonSpell2(t)),
				new(800f, 45, true, "夏符「夏日鋒芒」", static (w, t) => w.S2_Summer(t)),
				new(800f, 45, true, "秋符「秋風落葉」", static (w, t) => w.S2_Autumn(t)),
				new(1000f, 55, true, "冬符「冬日暴雪」", static (w, t) => w.S2_Winter(t)),
			},
			Dialogue = new List<FDialogueLine>[]
			{
				new()
				{   // 蓮
					new(1, "別動。你正站在我還沒乾的顏料上。"),
					new(0, "這片山谷的季節亂成一團，是你畫的？"),
					new(1, "夢裡有人對我說：「四季將散，畫下它們。」所以我把春夏秋冬都收進了這一幅畫。"),
					new(0, "又是夢裡的聲音？前面那個占星師也這麼說！"),
					new(1, "咦，他也聽見了？……不管了，畫還沒完成，我不能停筆！"),
					new(0, "那我就把你的畫筆打下來！"),
				},
				new()
				{   // 蒼
					new(1, "啊，弓手！你拉弓的姿勢真美，能讓我畫一張嗎？"),
					new(0, "不能。把季節還原。"),
					new(1, "不行喔。有個聲音說四季就要散了，我得趁現在把它們全畫下來。"),
					new(0, "跟占星師一樣的說詞。誰在背後說話？"),
					new(1, "我也不知道。可是那聲音……很溫柔，也很疲憊。"),
					new(0, "疲憊的是我。開始吧。"),
				},
				new()
				{   // 晴
					new(1, "你踩到我的顏料了……啊，算了，顏色挺好看。"),
					new(0, "這裡又開櫻花又落楓葉，山上的柿子都不知道該不該熟了～"),
					new(1, "夢裡有人拜託我，在四季散掉之前把它們畫下來。我只是聽話而已。"),
					new(0, "你也做夢？最近大家都好愛做夢喔。"),
					new(1, "大家？……難道不只我一個？"),
					new(0, "先不管啦，我想吃秋天的柿子，不想吃冰的！"),
				},
				new()
				{   // 雪
					new(1, "好安靜的孩子。你的人偶……也想入畫嗎？"),
					new(0, "它們說，這裡的季節亂得讓人頭暈。"),
					new(1, "我是照著夢裡的聲音畫的。它說：「四季將散，請留住它們。」"),
					new(0, "天狼先生也說，他聽見了聲音……是同一個人嗎？"),
					new(1, "也許吧。那聲音聽起來，像是快要消失了。"),
					new(0, "……那麼，就讓孩子們先把季節還回去吧。"),
				},
			},
		};

		// ------------------------------------------------------------------ stage 3
		public static readonly FBossDef Stage3 = new FBossDef
		{
			Name = "嵐", Title = "呼風喚雨的天氣師", StageTitle = "第三面", StageSub = "雲上之路",
			Phases = new()
			{
				new(500f, 30, false, "", static (w, t) => w.S3_NonSpell1(t)),
				new(800f, 45, true, "晴符「晴空萬里」", static (w, t) => w.S3_Sunny(t)),
				new(550f, 30, false, "", static (w, t) => w.S3_NonSpell2(t)),
				new(850f, 45, true, "雨符「雨幕延綿」", static (w, t) => w.S3_Rain(t)),
				new(850f, 45, true, "雷符「雷暴中心」", static (w, t) => w.S3_Storm(t)),
				new(1100f, 60, true, "霧符「雲裡霧裡」", static (w, t) => w.S3_Fog(t)),
			},
			Dialogue = new List<FDialogueLine>[]
			{
				new()
				{   // 蓮
					new(1, "誰准你飛上我的雲海的？給我滾下去！"),
					new(0, "山下連下了七天大雨，你就是罪魁禍首吧！"),
					new(1, "哼，是天上的聲音叫我聚雲的！不過我嵐可不是聽命行事——我是剛好想下雨！"),
					new(0, "天上的聲音……占星師和畫師也聽到了。到底是誰？"),
					new(1, "我怎麼知道！只知道我聚起的雲，全被吸進更高的天上去了。"),
					new(0, "有人在收集力量……？"),
					new(1, "少囉嗦！先撐過我的雷再說吧！"),
				},
				new()
				{   // 蒼
					new(1, "又來一個不怕雷劈的小鬼。"),
					new(0, "你的雷劈壞了三座神社。賠吧。"),
					new(1, "要賠找天上那傢伙賠！是他在我耳邊嘀咕，要我把雲聚起來的！"),
					new(0, "你聚起來的雲，往上飄走了。"),
					new(1, "……你也注意到了？我的雲，一直被什麼東西往天頂吸。真不爽。"),
					new(0, "那就先打完，再一起上去看。"),
				},
				new()
				{   // 晴
					new(1, "喂！雲是給你坐的嗎！快起來！"),
					new(0, "好軟喔～在這裡睡午覺一定很舒服。"),
					new(1, "我是天氣之主嵐！你給我認真一點！"),
					new(0, "好啦好啦。那天氣之主，為什麼一直下雨呢？"),
					new(1, "……天上有個聲音叫我聚雲。可是聚越多，雲就被吸走越多，我只好拼命再聚！"),
					new(0, "喔～所以你是被拿走雲才生氣的啊。"),
					new(1, "閉嘴！想要晴天？先淋一場暴雨吧！"),
				},
				new()
				{   // 雪
					new(1, "小鬼，帶著一堆布娃娃跑到雲上幹什麼？"),
					new(0, "雨下太久了……孩子們的衣服一直都乾不了。"),
					new(1, "哼，那你去跟天上的聲音抱怨！是它要我聚雲的。"),
					new(0, "占星師、畫師、還有您……大家都聽見同一個聲音。"),
					new(1, "是又怎樣？那聲音一直說「時間不多了」，聽了就煩！"),
					new(0, "……時間不多了。孩子們，我們得快一點。"),
				},
			},
		};

		// ------------------------------------------------------------------ stage 4
		public static readonly FBossDef Stage4 = new FBossDef
		{
			Name = "墨竹", Title = "四君子的書生", StageTitle = "第四面", StageSub = "四君子庭園",
			Phases = new()
			{
				new(550f, 30, false, "", static (w, t) => w.S4_NonSpell1(t)),
				new(850f, 45, true, "梅符「梅花盛開」", static (w, t) => w.S4_Plum(t)),
				new(600f, 30, false, "", static (w, t) => w.S4_NonSpell2(t)),
				new(900f, 45, true, "蘭符「蘭花路徑」", static (w, t) => w.S4_Orchid(t)),
				new(900f, 45, true, "竹符「竹林之勢」", static (w, t) => w.S4_Bamboo(t)),
				new(1000f, 50, true, "菊符「菊花之哀」", static (w, t) => w.S4_Chrys(t)),
				new(1f, 60, true, "四君子「君子之約」", static (w, t) => w.S4_Final(t), true, 1),
			},
			Dialogue = new List<FDialogueLine>[]
			{
				new()
				{   // 蓮
					new(1, "擅闖庭園，實屬無禮。閣下所為何事？"),
					new(0, "山裡的花草全都朝這裡長，是你搞的鬼吧？"),
					new(1, "在下奉「天命」聚集草木靈氣。天門開啟之時，山之靈氣將會散盡。"),
					new(0, "又是天上的聲音！占星師、畫師、天氣師……大家都被它使喚！"),
					new(1, "星辰、四時、天候、草木——閣下可曾想過，這四者合起來是什麼？"),
					new(0, "……什麼？"),
					new(1, "是撐起「天地結界」之氣。有人在收集它們。五行仙壇的玄，或許知道得更多。"),
					new(0, "那我先打贏你，再去問他！"),
				},
				new()
				{   // 蒼
					new(1, "持弓而來，殺氣太重，有失風雅。"),
					new(0, "你把山裡的靈氣都吸走了。"),
					new(1, "在下奉天命行事。只是近日越想越覺不妥。"),
					new(0, "說。"),
					new(1, "星、季、天候、靈氣，皆被引往天頂。有人在補一道破損的結界。"),
					new(0, "誰？"),
					new(1, "不知。但五行仙壇的方士玄，定然知情。——在此之前，請先賜教。"),
				},
				new()
				{   // 晴
					new(1, "這位修行者，入園請先脫鞋。"),
					new(0, "啊，抱歉抱歉～你家竹子長得真好，筍子能吃嗎？"),
					new(1, "……竹乃君子，豈可食之。"),
					new(0, "可是很好吃耶。對了，你也是聽了夢裡的聲音才這樣的吧？"),
					new(1, "……閣下怎會知道？那聲音說，天門將開，山靈將散，要在下守住它們。"),
					new(0, "大家都這麼說耶。占星的、畫畫的、下雨的……好像有人在湊齊什麼東西。"),
					new(1, "湊齊……原來如此。若想知道答案，去問五行仙壇的玄吧。不過，得先過在下這一關！"),
				},
				new()
				{   // 雪
					new(1, "有禮的客人。是來賞花的嗎？"),
					new(0, "是的……也想請您，把山的靈氣還回去。"),
					new(1, "在下是奉天命而為。天門開啟時，靈氣將散，必須有人守住。"),
					new(0, "孩子們說，星星、四季、雲……還有這裡的靈氣，都流向同一個地方。"),
					new(1, "好敏銳的人偶。沒錯，全都流向天頂，像在修補什麼。"),
					new(0, "修補……結界嗎？"),
					new(1, "或許。五行仙壇的方士玄，比誰都清楚天上的事。"),
					new(0, "那麼，請讓孩子們先過去。"),
				},
			},
			Midboss = null, ArtIndex = -1,
			Ending = new List<FDialogueLine>[]
			{   // after the fight
				new()
				{   // 蓮
					new(1, "……梅蘭竹菊，竟都敗在你手上。在下服了。"),
					new(0, "你也聽到那個聲音了吧？到底是誰？"),
					new(1, "在下只知，那聲音來自天上。它說靈氣齊聚之時，「門」便會開。"),
					new(1, "往五行仙壇去吧。玄……那個人或許知道得更多。"),
					new(0, "好！謝啦，書呆子！"),
					new(1, "……書、書呆子？"),
				},
				new()
				{   // 蒼
					new(1, "好箭法。君子不爭，在下卻爭輸了。"),
					new(0, "那聲音，你知道多少。"),
					new(1, "不多。只知它對每個人說的話都不一樣，卻都指向同一個夜晚——天門之夜。"),
					new(0, "下一個是誰？"),
					new(1, "五行仙壇的方士，玄。他說話如謎，閣下……請耐著性子聽。"),
					new(0, "我盡量。"),
				},
				new()
				{   // 晴
					new(1, "輸了。在下的庭園，被你踩得一塌糊塗。"),
					new(0, "對不起嘛～不過這裡的梅子好像很好吃耶！"),
					new(1, "……那要等結果才能吃。言歸正傳，你身上有不尋常的靈氣。"),
					new(0, "咦？是剛剛吃的饅頭嗎？"),
					new(1, "不是。是星辰、四季、天候……還有剛從在下身上流走的靈氣。你正一路收集它們。"),
					new(0, "收集到最後會怎樣？"),
					new(1, "在下不知道。去問五行仙壇的玄吧。"),
				},
				new()
				{   // 雪
					new(1, "人偶師，你贏了。在下的四君子，都向你低頭。"),
					new(0, "孩子們說……您的花，其實很溫柔。"),
					new(1, "……多謝。那聲音要在下守住靈氣，等一個人來取。現在想來，等的或許就是你。"),
					new(0, "等我……？"),
					new(1, "往五行仙壇吧。玄看得比在下遠。"),
					new(0, "孩子們，我們走吧。"),
				},
			},
		};

		// ------------------------------------------------------------------ stage 5
		public static readonly FBossDef Stage5 = new FBossDef
		{
			Name = "玄", Title = "五行的方士", StageTitle = "第五面", StageSub = "五行仙壇",
			Phases = new()
			{
				new(600f, 30, false, "", static (w, t) => w.S5_NonSpell1(t)),
				new(900f, 45, true, "木符「萬木生長」", static (w, t) => w.S5_Wood(t)),
				new(900f, 45, true, "火符「烈火燎原」", static (w, t) => w.S5_Fire(t)),
				new(650f, 30, false, "", static (w, t) => w.S5_NonSpell2(t)),
				new(950f, 45, true, "土符「山崩地裂」", static (w, t) => w.S5_Earth(t)),
				new(950f, 45, true, "金符「金戈鐵刃」", static (w, t) => w.S5_Metal(t)),
				new(700f, 30, false, "", static (w, t) => w.S5_NonSpell3(t)),
				new(950f, 45, true, "水符「流水無形」", static (w, t) => w.S5_Water(t)),
				new(1400f, 60, true, "五行「相生相剋」", static (w, t) => w.S5_Cycle(t)),
			},
			Dialogue = new List<FDialogueLine>[]
			{
				new()
				{   // 蓮
					new(1, "你來了。比卦象晚了一刻。"),
					new(0, "你一定知道吧！那個在夢裡下命令的聲音是誰？"),
					new(1, "天宮的守護者，燁。守護這片土地的「四神結界」正在崩壞，而他的力量也快耗盡了。"),
					new(0, "所以他讓大家收集星辰、四季、天候、靈氣……"),
					new(1, "再加上我的五行。可是他從沒說原因——因為真正的目的，不是讓我們收集。"),
					new(0, "那是什麼？"),
					new(1, "是讓「打倒我們的人」收集。你每贏一場，那份力量就流進你的身體。你，就是他在等的容器。"),
					new(0, "……我才不管什麼容器！我要親口問他！"),
					new(1, "呵……那就證明你裝得下第五份力量吧。"),
				},
				new()
				{   // 蒼
					new(1, "你的箭，屬金。"),
					new(0, "我沒空聽卦。在背後下令的是誰？"),
					new(1, "天宮的燁。四神結界的守護者。他快撐不住了，所以向五個人發了天啟。"),
					new(0, "要大家收集力量？"),
					new(1, "不。要大家「被打倒」。每一位守門人倒下，力量就會移到勝者身上。"),
					new(0, "……所以我一路射下來的，全是他的棋。"),
					new(1, "正是。而我是最後一顆棋子。來吧，把五行也拿走。"),
					new(0, "棋也好，靶也好。我照射不誤。"),
				},
				new()
				{   // 晴
					new(1, "山伏也懂五行？"),
					new(0, "懂一點點～木頭可以生火烤地瓜，火可以煮水泡茶！"),
					new(1, "……倒也沒錯。萬物相生，循環不息。那你可知道，你身上已經背了四份力量？"),
					new(0, "咦？難怪我最近一直很餓！"),
					new(1, "呵。那是星辰、四時、天候、靈氣。全是天宮的燁安排的——他要找一個能扛起四神結界的人。"),
					new(0, "結界？壞掉了嗎？"),
					new(1, "快碎了。燁也快消失了。所以他用天啟讓我們引起異變，好讓你一路收集。"),
					new(0, "好過分喔，那等一下要罵他一頓！"),
					new(1, "先過了我這關吧。來，把五行也背上。"),
				},
				new()
				{   // 雪
					new(1, "人偶師。你的人偶，屬木還是屬土？"),
					new(0, "它們……屬於我。請告訴我，那個聲音的主人是誰。"),
					new(1, "天宮的守護者，燁。四神結界將碎，他也將盡。"),
					new(0, "所以大家才……"),
					new(1, "是他讓我們引起異變，讓你一路取走我們的力量。你已經是他選中的容器。"),
					new(0, "孩子們說，那聲音一直都很寂寞。"),
					new(1, "……人偶看得比人清楚。那麼，接下第五份吧——五行之中沒有例外。"),
					new(0, "孩子們，我們會好好帶上去的。"),
				},
			},
			Midboss = null, ArtIndex = -1,
			Ending = new List<FDialogueLine>[]
			{   // after the fight
				new()
				{   // 蓮
					new(1, "五行已盡……卦象說得沒錯，你裝得下。"),
					new(0, "身體好熱……這就是五份力量？"),
					new(1, "是。天宮就在雲上。燁等的，就是現在的你。"),
					new(0, "你們被他利用，不生氣嗎？"),
					new(1, "生氣？呵……我們都夢到了同一個將要崩塌的世界。若能撐住它，當一顆棋子也無妨。"),
					new(0, "……我會好好問他的。"),
					new(1, "去吧。天門之夜，時辰將至。"),
				},
				new()
				{   // 蒼
					new(1, "金克木，箭穿五行。……卦象誠不我欺。"),
					new(0, "天宮怎麼走。"),
					new(1, "往上。雲會為你讓路。"),
					new(0, "你們甘心當棋？"),
					new(1, "棋盤若毀，便無所謂棋子。我們只是選了能守住棋盤的那一步。"),
					new(0, "……我不喜歡被人下。但這一步，我走。"),
				},
				new()
				{   // 晴
					new(1, "五行皆盡。你現在，比剛才更餓了吧？"),
					new(0, "對！肚子在叫！"),
					new(1, "那是五份力量在你體內打轉。上天宮去吧，燁……大概會請你吃飯。"),
					new(0, "真的嗎！？"),
					new(1, "卦象沒說。但若結界補好，大家都能好好吃飯。"),
					new(0, "那就更要去了！"),
				},
				new()
				{   // 雪
					new(1, "五行相生，最後都流向了你。"),
					new(0, "孩子們說，這些力量……好重。"),
					new(1, "那是結界的重量。燁一個人扛了好幾百年。"),
					new(0, "一個人……"),
					new(1, "上天宮吧，人偶師。你和你的孩子們，或許能替他分擔。"),
					new(0, "……嗯。我們一起去。"),
				},
			},
		};

		// ------------------------------------------------------------------ stage 6 (final)
		public static readonly FBossDef Stage6 = new FBossDef
		{
			Name = "燁", Title = "四神的守護者", StageTitle = "第六面", StageSub = "天宮",
			Phases = new()
			{
				new(650f, 30, false, "", static (w, t) => w.S6_NonSpell1(t)),
				new(1000f, 45, true, "東方「青龍天昇」", static (w, t) => w.S6_Dragon(t)),
				new(700f, 30, false, "", static (w, t) => w.S6_NonSpell2(t)),
				new(1000f, 45, true, "西方「白虎嘯風」", static (w, t) => w.S6_Tiger(t)),
				new(750f, 30, false, "", static (w, t) => w.S6_NonSpell3(t)),
				new(1050f, 45, true, "南方「朱雀焚天」", static (w, t) => w.S6_Phoenix(t)),
				new(1050f, 45, true, "北方「玄武鎮海」", static (w, t) => w.S6_Tortoise(t)),
				new(800f, 30, false, "", static (w, t) => w.S6_NonSpell4(t)),
				new(2600f, 99, true, "四神奧義「四象歸一」", static (w, t) => w.S6_Final(t)),
			},
			Dialogue = new List<FDialogueLine>[]
			{
				new()
				{   // 蓮
					new(1, "走到這裡的人，你是第一個。"),
					new(0, "你就是燁吧！大家的異變，全是你在背後安排的！"),
					new(1, "是的。四神結界撐不過今晚的天門之夜，而我已經沒有力量補上它。"),
					new(0, "那為什麼不直接說！為什麼要讓天狼、千秋、嵐、墨竹、玄去鬧事？"),
					new(1, "結界之力只承認「勝者」。我離不開天宮，只能讓五位守門人各執一份力量，等待有人一一勝過他們。"),
					new(0, "……那些力量，現在全在我身上。"),
					new(1, "沒錯。我對他們、對村子都感到抱歉。但若結界崩毀，失去的將不只是星星和季節。"),
					new(0, "我才不要什麼繼承！我只要大家能好好過日子！"),
					new(1, "正是這份心，才配得上這些力量。最後一次——以四神之力試你。"),
					new(0, "好！打完以後，你得親口跟大家道歉！"),
				},
				new()
				{   // 蒼
					new(1, "弓手，你一路射穿了所有考驗。"),
					new(0, "考驗？害那麼多人困擾，就為了這個？"),
					new(1, "天門之夜就在今晚。四神結界已經碎了一半，而我快撐不住了。"),
					new(0, "所以你讓五個人鬧事，讓我把他們的力量一一搶過來。"),
					new(1, "結界之力只追隨勝者。我無法下凡，只能這樣做。"),
					new(0, "自私。"),
					new(1, "我承認。這份罪，我會在結界補好之後承擔。"),
					new(0, "……那就快點結束。"),
					new(1, "好。四神為證——讓我看看你的箭，能否抵達天頂！"),
					new(0, "我說過了。我只需要一箭。"),
				},
				new()
				{   // 晴
					new(1, "修行者，歡迎來到天宮。"),
					new(0, "哇～好漂亮！這裡有供品可以吃嗎？……不對，我是來罵你的！"),
					new(1, "罵我？"),
					new(0, "你讓大家做奇怪的夢、亂搞星星跟季節，害村子好慘！"),
					new(1, "……是的。四神結界今晚就會碎裂，而我已無力修補。結界之力只追隨勝者，我只能讓守門人們各執一份，等人來取。"),
					new(0, "所以我一路吃苦，都是在幫你搬東西喔？"),
					new(1, "呵……可以這麼說。你背上的五份力量，足以補上結界。但我必須確認，你承受得住。"),
					new(0, "那打完以後，你要請大家吃飯喔，五個守門人都要！"),
					new(1, "一言為定。來吧，接受四神的最後試煉！"),
					new(0, "說好了喔～開飯前的運動！"),
				},
				new()
				{   // 雪
					new(1, "安靜的孩子。你的人偶，陪你走過了每一關。"),
					new(0, "是的……它們說，您看起來很寂寞。"),
					new(1, "守護四方的人，本就孤身一人。如今，我連守護也快做不到了。"),
					new(0, "所以您發出了天啟，讓大家引起異變。"),
					new(1, "結界之力只追隨勝者。我讓五人各守一份，等待有人一一贏過他們——那就是你。"),
					new(0, "天狼先生、千秋先生、嵐先生、墨竹先生、玄先生……大家其實都不是壞人。"),
					new(1, "是我讓他們背了罪名。等結界修好，我會親自向他們謝罪。"),
					new(0, "那麼……請讓孩子們，也陪陪您吧。"),
					new(1, "……溫柔的回答。那就以四神之力，最後一次試你！"),
					new(0, "孩子們，最後一場了。"),
				},
			},
			Midboss = null, ArtIndex = -1,
			Ending = new List<FDialogueLine>[]
			{   // after the fight
				new()
				{   // 蓮
					new(1, "……勝負已分。四神的力量，現在都屬於你了。"),
					new(0, "那結界呢？補得回來嗎？"),
					new(1, "已經在補了。你每贏一場，結界就穩一分。天門之夜，會平安過去。"),
					new(0, "那你呢？你會消失嗎？"),
					new(1, "力量交出去了，我也終於能休息。不必擔心，守護者不會死，只是睡去。"),
					new(0, "……下次有事，直接說！不要再讓大家打架了！"),
					new(1, "哈……是。我答應你。"),
				},
				new()
				{   // 蒼
					new(1, "好箭。……結界已經穩住了。"),
					new(0, "你利用了五個人。"),
					new(1, "是。這份罪，我會記著。"),
					new(0, "……不必。他們都知道，還是選了這條路。"),
					new(1, "你比看起來溫柔。"),
					new(0, "少囉嗦。快去睡吧，守護者。"),
				},
				new()
				{   // 晴
					new(1, "……結束了。天門之夜，平安無事。"),
					new(0, "太好了！那可以吃飯了嗎？"),
					new(1, "……你一路打上天宮，就為了這個？"),
					new(0, "還有大家！天狼、千秋、嵐、墨竹、玄，大家都很擔心你耶。還有我！"),
					new(1, "……是嗎。原來，我不是一個人在守。"),
					new(0, "所以啦，下次一起吃飯！"),
					new(1, "好。下次，一起。"),
				},
				new()
				{   // 雪
					new(1, "人偶師……謝謝你。結界穩住了。"),
					new(0, "孩子們說，你的聲音不再寂寞了。"),
					new(1, "五百年來，我一直以為只能靠自己。原來，還能把重量分出去。"),
					new(0, "分給大家吧。我們都在。"),
					new(1, "……嗯。那就拜託了，新的守護者。"),
					new(0, "孩子們，我們有新的工作了。"),
				},
			},
		};

		// ------------------------------------------------------------------ Extra midboss: 玄 warns the player
		public static readonly FBossDef StageExMid = new FBossDef
		{
			Name = "玄", Title = "五行的方士", StageTitle = "", StageSub = "",
			Phases = new()
			{
				new(700f, 30, true, "逆符「五行逆轉」", static (w, t) => w.MB_Reverse(t)),
				new(750f, 35, true, "數符「河圖洛書」", static (w, t) => w.MB_LoShu(t)),
				new(800f, 35, true, "陣符「八門金鎖」", static (w, t) => w.MB_EightGates(t)),
			},
			Dialogue = new List<FDialogueLine>[]
			{
				new()
				{   // 蓮
					new(1, "又見面了。結界補好了，你卻還往上走。"),
					new(0, "天宮後面還有路！那裡有什麼？"),
					new(1, "「天和閣」。主人叫一索——五百年前，從燁手上贏走了十三枚天印的男人。"),
					new(0, "十三枚天印？"),
					new(1, "結界最初的裂痕，就是從那時開始的。他從不輸牌，你的力量再強，也得照他的規矩來。"),
					new(0, "我才不怕規矩！"),
					new(1, "勸你在此撤退，這是最好的卦。……不聽的話，就先過我這三卦吧。"),
				},
				new()
				{   // 蒼
					new(1, "又見面了。你的箭，還想往哪裡射？"),
					new(0, "天宮後面。那裡有人。"),
					new(1, "天和閣的一索。他手上握著十三枚天印——結界缺的最後一塊。"),
					new(0, "那就拿回來。"),
					new(1, "他不是靠力量能打倒的人。他的規矩，連燁都贏不了。撤退吧，這是忠告。"),
					new(0, "不。"),
					new(1, "……果然。那就讓我用三卦，看你有沒有資格。"),
				},
				new()
				{   // 晴
					new(1, "山伏，你怎麼又來了？"),
					new(0, "燁說要請客，我在找廚房～結果走到這裡來了。"),
					new(1, "這裡往前是天和閣。主人一索，愛賭成性，五百年前從燁手上贏走十三枚天印。"),
					new(0, "天印？可以吃嗎？"),
					new(1, "……不能。結界的裂痕就是從那時開始的。他從不輸，連燁都敗給他。勸你撤退。"),
					new(0, "可是燁說他請客，那個一索搞不好也有好吃的！"),
					new(1, "唉……那就讓我用三卦，試試你的胃口。"),
				},
				new()
				{   // 雪
					new(1, "人偶師，結界已經補好，你為何還往前走？"),
					new(0, "孩子們說，前面還有一個發光的地方，在叫我們。"),
					new(1, "天和閣。主人一索，五百年前與燁對賭，贏走了十三枚天印。"),
					new(0, "十三枚……那就是結界最初的裂痕嗎？"),
					new(1, "不錯。但他從不輸牌。以你的溫柔，恐怕會被他的規矩吞掉。撤退吧。"),
					new(0, "……孩子們想見他。"),
					new(1, "那就先讓我用三卦，看看人偶的覺悟。"),
				},
			},
			Midboss = null, ArtIndex = 4,   // uses stage 5's art
		};

		// ------------------------------------------------------------------ Extra
		public static readonly FBossDef StageEx = new FBossDef
		{
			Name = "一索", Title = "役滿的雀聖", StageTitle = "Extra", StageSub = "雀莊・天和閣",
			Phases = new()
			{
				new(700f, 30, false, "", static (w, t) => w.EX_NonSpell1(t)),
				new(900f, 45, true, "立直「立直一發」", static (w, t) => w.EX_Riichi(t)),
				new(850f, 40, true, "平和「平和斷么」", static (w, t) => w.EX_Pinfu(t)),
				new(750f, 30, false, "", static (w, t) => w.EX_NonSpell2(t)),
				new(950f, 45, true, "一色「清一色」", static (w, t) => w.EX_Chinitsu(t)),
				new(950f, 45, true, "對子「七對子」", static (w, t) => w.EX_Chiitoi(t)),
				new(800f, 30, false, "", static (w, t) => w.EX_NonSpell3(t)),
				new(1000f, 45, true, "開花「嶺上開花」", static (w, t) => w.EX_Rinshan(t)),
				new(1100f, 50, true, "役滿一「大三元」", static (w, t) => w.EX_Daisangen(t)),
				new(1100f, 50, true, "役滿二「大四喜」", static (w, t) => w.EX_Daisuushi(t)),
				new(1f, 45, true, "役滿三「九蓮寶燈」", static (w, t) => w.EX_Chuuren(t), true),
				new(1300f, 70, true, "役滿奧義「國士無雙」", static (w, t) => w.EX_Kokushi(t)),
				new(6000f, 99, true, "終局「天和」", static (w, t) => w.EX_Tenhou(t)) { bTimerAtThird = true },   // the 99 s only start at 1/3 HP
			},
			Dialogue = new List<FDialogueLine>[]
			{
				new()
				{   // 蓮
					new(1, "喲，通關的小哥！玄那傢伙沒把你攔住啊。"),
					new(0, "你就是一索！把十三枚天印還給燁！"),
					new(1, "還？我可是贏來的。五百年前，那個認真過頭的守護者，想用天印賭我的「不敗之運」。"),
					new(0, "他為什麼要賭那種東西？"),
					new(1, "他想要一份永遠不輸的運氣，去守永遠不能輸的結界。結果他輸了——所以結界才開始裂。"),
					new(0, "那你明明知道，還一直留著！"),
					new(1, "我留著，是因為他一直不肯好好休息。沒有天印，他才會去找繼承人。……你看，他不就找到你了？"),
					new(0, "……這是什麼歪理！"),
					new(1, "規矩很簡單：贏我一局，十三枚全給你。來，開局！"),
				},
				new()
				{   // 蒼
					new(1, "冷著一張臉，不錯，很適合打牌。"),
					new(0, "我不賭。天印還來。"),
					new(1, "人生就是一場賭局。燁當年賭輸了天印，結界才開始裂。"),
					new(0, "你明知道，還不還。"),
					new(1, "因為那傢伙會把自己燒乾。沒了天印，他才終於肯找人接手——找到的就是你。"),
					new(0, "所以你也算計了。"),
					new(1, "我只是坐在牌桌前等。來吧，你射出每一箭時，不也在賭？"),
					new(0, "……我從不射偏。"),
					new(1, "哈哈！那就讓我看看，你能不能聽牌到最後！"),
				},
				new()
				{   // 晴
					new(1, "山伏也來雀莊？輸了可是要請客的喔。"),
					new(0, "好啊～贏了你請我吃一桌！還有，天印要還給燁喔。"),
					new(1, "喔？玄都告訴你了。那是五百年前燁輸給我的。他想用天印換我的「不敗之運」。"),
					new(0, "為什麼？"),
					new(1, "那個死腦筋想永遠不輸地守著結界。可是一個人怎麼可能永遠不輸？輸了，才會找人幫忙。"),
					new(0, "嗯～所以你是故意不還，好讓他找幫手？"),
					new(1, "誰知道呢。總之規矩是：贏我，十三枚全給你。"),
					new(0, "那就說好了，十三枚天印，加十三道菜！"),
					new(1, "十三？巧了，我也有十三張牌要給你看——國士無雙！"),
				},
				new()
				{   // 雪
					new(1, "小朋友，帶著人偶來打牌？"),
					new(0, "孩子們想看您打牌……它們說，您的牌在發光。"),
					new(1, "好眼光！這十三張，是五百年前從燁手上贏來的天印。"),
					new(0, "結界的裂痕……是從那時開始的吧。"),
					new(1, "是啊。那傢伙想用天印換我的運氣，好一個人守到永遠。他輸了，我就把天印留下了。"),
					new(0, "您是怕他……一直一個人嗎？"),
					new(1, "……人偶師，你看得太清楚了。沒錯，沒了天印，他才肯找人來分擔。"),
					new(0, "那麼，請讓孩子們把天印帶回去吧。"),
					new(1, "哈！有意思。那就開局吧，贏了我，九蓮寶燈都給你看！"),
				},
			},
			Midboss = StageExMid, ArtIndex = -1,
			Ending = new List<FDialogueLine>[]
			{   // after the fight
				new()
				{   // 蓮
					new(1, "哈哈哈！和了！不對，是你和了。十三枚天印，拿去吧。"),
					new(0, "……這麼乾脆？"),
					new(1, "賭品要好。而且，我等的就是這一天。"),
					new(0, "等什麼？"),
					new(1, "等燁找到可以信任的人。天印回到結界，最初的裂痕也補上了——五百年的牌局，終於結束。"),
					new(0, "你其實一直在幫他吧。"),
					new(1, "誰知道呢？下次記得帶燁來打牌，三缺一啊。"),
				},
				new()
				{   // 蒼
					new(1, "漂亮。每一箭都聽牌，每一箭都和了。"),
					new(0, "天印。"),
					new(1, "拿去拿去。十三枚，一枚不少。"),
					new(0, "你早就打算還了。"),
					new(1, "我只是在等一個值得還的人。燁那傢伙，終於學會依靠別人了。"),
					new(0, "……你們兩個，都很麻煩。"),
					new(1, "哈哈！這句我收下了。改天來打牌吧！"),
				},
				new()
				{   // 晴
					new(1, "輸了輸了！說好的，十三枚天印……還有十三道菜！"),
					new(0, "耶～！真的有菜嗎？"),
					new(1, "當然，天和閣的廚房可是天界第一。"),
					new(0, "那燁也一起來吧！他一定很久沒好好吃飯了。"),
					new(1, "……你這山伏，總是說中要害。好，把他也拖來。"),
					new(0, "說好了喔！十三道菜，一道都不能少！"),
				},
				new()
				{   // 雪
					new(1, "你贏了，人偶師。十三枚天印，請帶回去吧。"),
					new(0, "謝謝您……孩子們說，您其實很捨不得。"),
					new(1, "捨不得？哈……是啊。這些天印陪了我五百年。"),
					new(0, "那，您可以來天宮看它們。和燁一起。"),
					new(1, "……你真是溫柔得可怕。好，就這麼說定了。"),
					new(0, "孩子們，回家吧。天印也一起。"),
				},
			},
		};

		// ------------------------------------------------------------------ Extra-IF: the four perils
		// midbosses: 晴 meets 饕餮, 蓮 meets 窮奇, 蒼 meets 檮杌, 雪 meets 混沌 (speaker 1 = that beast)
		public static readonly FBossDef TaotieMid = new FBossDef
		{
			Name = "饕餮", Title = "貪食的凶獸", StageTitle = "", StageSub = "",
			Phases = new()
			{
				new(650f, 35, true, "饕符「貪食之口」", static (w, t) => w.IF_TaotieMaw(t)),
				new(700f, 35, true, "饕符「鐘鼎饌宴」", static (w, t) => w.IF_TaotieFeast(t)),
				new(750f, 35, true, "饕符「饕餮紋」", static (w, t) => w.IF_TaotieMotif(t)),
			},
			Dialogue = new List<FDialogueLine>[]
			{
				new()
				{   // 蓮
				},
				new()
				{   // 蒼
				},
				new()
				{   // 晴
					new(1, "嗯？好香的味道……是吃了五份力量的小鬼啊。"),
					new(0, "你是誰？天宮怎麼變成這樣了……燁呢？"),
					new(1, "我是饕餮。結界碎了，那個守護者也睡著了。被壓在四神底下幾千年，終於能出來吃頓飽飯。"),
					new(0, "我也好餓！可是你吃掉的是天宮耶！"),
					new(1, "天宮、雲、星星，什麼都能吃。上面還有我的兄弟們——檮杌、混沌、窮奇。勸你回頭，他們可沒我這麼好說話。"),
					new(0, "才不要！說好要讓燁請大家吃飯的！"),
					new(1, "哈哈！那就比比看，誰的胃口比較大！"),
				},
				new()
				{   // 雪
				},
			},
			Midboss = null, ArtIndex = 7,   // 饕餮's art
		};
		public static readonly FBossDef TaowuMid = new FBossDef
		{
			Name = "檮杌", Title = "不馴的凶獸", StageTitle = "", StageSub = "",
			Phases = new()
			{
				new(650f, 35, true, "杌符「頑石不化」", static (w, t) => w.IF_TaowuStone(t)),
				new(700f, 35, true, "杌符「難訓之獸」", static (w, t) => w.IF_TaowuUntamed(t)),
				new(750f, 35, true, "杌符「丈八之尾」", static (w, t) => w.IF_TaowuTail(t)),
			},
			Dialogue = new List<FDialogueLine>[]
			{
				new()
				{   // 蓮
				},
				new()
				{   // 蒼
					new(1, "……射手。你也是來把我們壓回去的？"),
					new(0, "結界碎了。你是什麼。"),
					new(1, "檮杌。四神鎮壓的四凶之一。不聽教誨，不肯回頭——你們是這麼說我的吧。"),
					new(0, "我只問一件事：燁在哪。"),
					new(1, "在上面睡著。和我的兄弟們在一起——饕餮、混沌、窮奇。去了，你會死。"),
					new(0, "那就先過你。"),
					new(1, "好。我也從不退。"),
				},
				new()
				{   // 晴
				},
				new()
				{   // 雪
				},
			},
			Midboss = null, ArtIndex = 8,   // 檮杌's art
		};
		public static readonly FBossDef HundunMid = new FBossDef
		{
			Name = "混沌", Title = "無面的凶獸", StageTitle = "", StageSub = "",
			Phases = new()
			{
				new(650f, 35, true, "沌符「七竅未開」", static (w, t) => w.IF_HundunSeven(t)),
				new(700f, 35, true, "沌符「無面之舞」", static (w, t) => w.IF_HundunDance(t)),
				new(750f, 35, true, "沌符「帝江歌舞」", static (w, t) => w.IF_HundunSong(t)),
			},
			Dialogue = new List<FDialogueLine>[]
			{
				new()
				{   // 蓮
				},
				new()
				{   // 蒼
				},
				new()
				{   // 晴
				},
				new()
				{   // 雪
					new(1, "…………"),
					new(0, "孩子們說，您沒有臉。可是您在笑。"),
					new(1, "我是混沌。沒有眼睛，也看得見你。沒有耳朵，也聽得見結界碎掉的聲音。"),
					new(0, "燁先生……在哪裡呢？"),
					new(1, "他睡著了。四神倒下，我們四個便出來了。上面的兄弟們等著呢——饕餮、檮杌、窮奇。"),
					new(0, "孩子們想帶燁先生回家。"),
					new(1, "那就跳一支舞吧，人偶師。跳完了，若你還站著，就往上走。"),
				},
			},
			Midboss = null, ArtIndex = 9,   // 混沌's art
		};
		public static readonly FBossDef QiongqiMid = new FBossDef
		{
			Name = "窮奇", Title = "顛倒是非的凶獸", StageTitle = "", StageSub = "",
			Phases = new()
			{
				new(650f, 35, true, "奇符「懲善揚惡」", static (w, t) => w.IF_QiongqiLie(t)),
				new(700f, 35, true, "奇符「翼虎狂風」", static (w, t) => w.IF_QiongqiGale(t)),
				new(750f, 35, true, "奇符「食人從首」", static (w, t) => w.IF_QiongqiHead(t)),
			},
			Dialogue = new List<FDialogueLine>[]
			{
				new()
				{   // 蓮
					new(1, "喲，好正直的眼神。我最喜歡吃你這種人了。"),
					new(0, "你就是把天宮搞成這樣的傢伙？"),
					new(1, "我是窮奇。結界碎了，四神睡了，我們四凶自由了。有人爭吵的時候，我專吃講理的那一邊。"),
					new(0, "歪理！燁在哪裡！"),
					new(1, "在上面，和我的兄弟們一起——饕餮、檮杌、混沌。好心勸你一句：回頭吧，正直的人在上面活不久。"),
					new(0, "我才不回頭！"),
					new(1, "哈！那就從頭開始，一口一口吃掉你！"),
				},
				new()
				{   // 蒼
				},
				new()
				{   // 晴
				},
				new()
				{   // 雪
				},
			},
			Midboss = null, ArtIndex = 10,   // 窮奇's art
		};
		// the relay: two cards each, then the four together (Actor: 0 饕餮, 1 檮杌, 2 混沌, 3 窮奇, 4 all four)
		// speakers in these conversations: 2 饕餮, 3 檮杌, 4 混沌, 5 窮奇
		public static readonly FBossDef StageIf = new FBossDef
		{
			Name = "四凶", Title = "結界下的上古凶獸", StageTitle = "Extra-IF", StageSub = "崩落天宮・四凶之淵",
			Phases = new()
			{
				new(800f, 30, false, "", static (w, t) => w.IF_NsTaotie(t), false, 0, 0),
				new(1000f, 45, true, "凶符「吞天噬地」", static (w, t) => w.IF_Devour(t), false, 0, 0),
				new(800f, 30, false, "", static (w, t) => w.IF_NsTaotie(t), false, 0, 0),
				new(1000f, 45, true, "凶符「無底之胃」", static (w, t) => w.IF_Stomach(t), false, 0, 0),
				new(800f, 30, false, "", static (w, t) => w.IF_NsTaowu(t), false, 0, 1),
				new(1000f, 90, true, "凶符「傲狠明德」", static (w, t) => w.IF_Arrogance(t), false, 0, 1),
				new(800f, 30, false, "", static (w, t) => w.IF_NsTaowu(t), false, 0, 1),
				new(1000f, 45, true, "凶符「不退之陣」", static (w, t) => w.IF_NoRetreat(t), false, 0, 1),
				new(800f, 30, false, "", static (w, t) => w.IF_NsHundun(t), false, 0, 2),
				new(1000f, 45, true, "凶符「混元一氣」", static (w, t) => w.IF_PrimalMist(t), false, 0, 2),
				new(800f, 30, false, "", static (w, t) => w.IF_NsHundun(t), false, 0, 2),
				new(1000f, 45, true, "凶符「渾沌初開」", static (w, t) => w.IF_ChaosSplit(t), false, 0, 2),
				new(800f, 30, false, "", static (w, t) => w.IF_NsQiongqi(t), false, 0, 3),
				new(1000f, 45, true, "凶符「顛倒是非」", static (w, t) => w.IF_Inversion(t), false, 0, 3),
				new(800f, 30, false, "", static (w, t) => w.IF_NsQiongqi(t), false, 0, 3),
				new(1000f, 90, true, "凶符「窮奇之翼」", static (w, t) => w.IF_Wings(t), false, 0, 3),
				new(1f, 60, true, "耐久「四凶降世」", static (w, t) => w.IF_QuadSurvive(t), true, 0, 4),
				new(4000f, 99, true, "終符「四凶・天地失序」", static (w, t) => w.IF_QuadFinal(t), false, 0, 4) { bTimerAtThird = true },   // the 99 s only start at 1/3 HP
			},
			Dialogue = new List<FDialogueLine>[]
			{
				new()
				{   // 蓮
					new(2, "來了來了！窮奇說的那個正直小鬼！"),
					new(3, "……打倒了窮奇，還敢往上走。"),
					new(0, "你們就是四凶！把燁還來！"),
					new(4, "他就睡在我們身後。四神結界碎了，守護者也撐不住了。"),
					new(5, "我說過了吧？正直的人在上面活不久。"),
					new(0, "為什麼要出來搗亂！結界壓著你們，大家才能好好過日子！"),
					new(3, "我們被壓了幾千年。沒有人問過我們，想不想好好過日子。"),
					new(2, "所以啊——你身上那五份力量，正好讓我們吃個飽！"),
					new(0, "休想！我一個一個打回去！"),
					new(5, "好啊，一個一個來。最後，四個一起！"),
				},
				new()
				{   // 蒼
					new(3, "……你果然來了。"),
					new(0, "燁在哪。"),
					new(2, "哇，好冷的人。檮杌，這就是打敗你的射手？"),
					new(5, "眼神真直，我喜歡。"),
					new(4, "守護者在後面睡著。四神倒了，結界碎了。你要做什麼呢？"),
					new(0, "把你們射回原位。用我身上這五份力量。"),
					new(3, "我們不會退。你也不會。那就只剩一條路。"),
					new(0, "我只需要四箭。"),
					new(5, "哈哈哈！四箭？那就讓你一箭一箭試試！"),
				},
				new()
				{   // 晴
					new(2, "又是你！剛剛還沒吃夠嗎？"),
					new(0, "還沒！而且燁欠我一頓飯！"),
					new(5, "這山伏是怎麼回事？一點都不怕。"),
					new(4, "他心裡沒有恐懼。只有……肚子餓。"),
					new(3, "哼。結界碎了，守護者睡了，我們出來了。你一個人能做什麼？"),
					new(0, "把你們打回去，然後叫醒燁，大家一起吃飯！你們也可以來喔！"),
					new(2, "……這、這是在邀請我們？"),
					new(5, "先打贏再說吧，小鬼！"),
				},
				new()
				{   // 雪
					new(4, "人偶師。你跳完舞了，還站著。"),
					new(0, "是的……孩子們都在。"),
					new(2, "哦？混沌居然放他上來？"),
					new(5, "好安靜的小鬼。你不怕嗎？"),
					new(0, "怕。可是孩子們說，你們……其實也很寂寞。"),
					new(3, "……寂寞？被壓在結界下幾千年，只剩下恨。"),
					new(0, "那麼，等一切結束，請和燁先生一起，把話說清楚吧。"),
					new(4, "說話……我們沒有嘴，也沒有人聽。……好。若你贏了，我們就聽你說。"),
				},
			},
			Midboss = null, ArtIndex = -1,
			Ending = new List<FDialogueLine>[]
			{
				new()
				{   // 蓮
					new(5, "……輸了。正直的人，原來也能活下來啊。"),
					new(0, "我會把你們封回去。可是不是用恨，是用約定！"),
					new(3, "約定？"),
					new(0, "下一次天門之夜，我會來看你們。不讓你們再被忘記！"),
					new(2, "……那、那順便帶點吃的！"),
					new(4, "……結界，重新亮起來了。守護者也快醒了。"),
				},
				new()
				{   // 蒼
					new(3, "……我退了。第一次。"),
					new(0, "你沒有退。你只是輸了。"),
					new(3, "……哼。這兩個不一樣嗎？"),
					new(0, "不一樣。下次再來比。"),
					new(5, "哈！這射手原來會說笑話。"),
					new(4, "結界重新合上了。我們回去睡吧……這一次，不是被壓著，是自己躺下。"),
				},
				new()
				{   // 晴
					new(2, "吃、吃不下了……我認輸……"),
					new(0, "好耶！那等燁醒了，大家一起吃飯喔！"),
					new(5, "被封印的傢伙要怎麼吃飯啦！"),
					new(0, "天門之夜我來接你們！一年一次，請你們吃大餐！"),
					new(3, "……真是奇怪的人類。"),
					new(4, "呵……那就約好了。結界合上之前，讓我們記住這個味道。"),
				},
				new()
				{   // 雪
					new(4, "你的舞，跳得比我好。"),
					new(0, "是孩子們在跳。……現在，可以說話了嗎？"),
					new(3, "……我們只是想被看見。"),
					new(2, "還想吃飽一點。"),
					new(5, "還想有人聽我們吵架。"),
					new(0, "那麼，孩子們會記得你們。燁先生醒來後，也會聽的。"),
					new(4, "……謝謝你，人偶師。結界合上了。我們……晚安。"),
				},
			},
			CharMidboss = new FBossDef[] { QiongqiMid, TaowuMid, TaotieMid, HundunMid },   // midboss per character: 蓮, 蒼, 晴, 雪
		};

	}
}
