// The game's story: every conversation (before and after each fight) and the ending scenes, in Chinese with English.
// Story.Apply() writes these into the boss tables from Bosses.cs (replacing the older lines there); the English goes
// into Tr()'s table. Speakers: 0 the player, 1 the boss, 2 饕餮, 3 檮杌, 4 混沌, 5 窮奇.
// Characters: 蓮 (Ren) the village shrine's young onmyoji; 蒼 (Sou) a hunter, few words; 晴 (Haru) a wandering
// yamabushi, always hungry; 雪 (Yuki) a quiet puppeteer whose dolls ("孩子們") sense what people can't.
using Godot;

namespace DanmakuSTG;

public readonly record struct L(int S, string Zh, string En);

public static partial class Story
{
	/** One conversation per player character, in ECharacter order (蓮, 蒼, 晴, 雪). An empty array keeps no lines. */
	public sealed record Talk(L[] Ren, L[] Sou, L[] Haru, L[] Yuki)
	{
		public L[] For(int C) => C switch { 0 => Ren, 1 => Sou, 2 => Haru, _ => Yuki };
	}

	static readonly L[] None = System.Array.Empty<L>();
	static bool bApplied;

	static void Fill(List<FDialogueLine>[] Target, Talk T)
	{
		if (T == null) return;
		for (int c = 0; c < 4; ++c)
		{
			Target[c].Clear();
			foreach (var Line in T.For(c)) Target[c].Add(new FDialogueLine(Line.S, Line.Zh));
		}
	}

	/** Writes the story into the boss tables (called once from FindBossDef). */
	public static void Apply(Func<int, FBossDef> Find)
	{
		if (bApplied) return;
		bApplied = true;
		for (int s = 0; s <= DanmakuWorld.IfStage; ++s)
		{
			FBossDef D = Find(s);
			if (D == null) continue;
			Fill(D.Dialogue, Before[s]);
			Fill(D.Ending, After[s]);
		}
		FBossDef Ex = Find(DanmakuWorld.ExtraStage);
		if (Ex?.Midboss != null) Fill(Ex.Midboss.Dialogue, ExMid);
		FBossDef If = Find(DanmakuWorld.IfStage);
		if (If != null)
		{
			// the road beast each character meets: 蓮 窮奇, 蒼 檮杌, 晴 饕餮, 雪 混沌
			Talk[] Mids = { IfMidQiongqi, IfMidTaowu, IfMidTaotie, IfMidHundun };
			for (int c = 0; c < 4; ++c) if (If.CharMidboss[c] != null) Fill(If.CharMidboss[c].Dialogue, Mids[c]);
		}
	}

	/** Every Chinese line with its English (for Tr). */
	public static IEnumerable<(string Zh, string En)> AllPairs()
	{
		IEnumerable<Talk> Talks()
		{
			foreach (var T in Before) if (T != null) yield return T;
			foreach (var T in After) if (T != null) yield return T;
			yield return ExMid;
			yield return IfMidQiongqi; yield return IfMidTaowu; yield return IfMidTaotie; yield return IfMidHundun;
		}
		foreach (var T in Talks())
			for (int c = 0; c < 4; ++c)
				foreach (var Line in T.For(c)) yield return (Line.Zh, Line.En);
		foreach (var E in Endings)
		{
			yield return (E.Title, E.TitleEn);
			foreach (var P in E.Pages) foreach (var Line in P.Lines) yield return (Line.Zh, Line.En);
		}
		foreach (var P in EndingUi) yield return P;
	}

	// Before[s] / After[s]: the conversation before / after the fight of stage s (0..5 main, 6 Extra, 7 Extra-IF relay)
	static readonly Talk[] Before = { Stage1, Stage2, Stage3, Stage4, Stage5, Stage6, StageEx, StageIf };
	static readonly Talk[] After = { null, null, null, Stage4After, Stage5After, Stage6After, StageExAfter, StageIfAfter };

	// =====================================================================================
	// Stage 1 — 星光參道. 天狼 has pulled the stars over the village down onto the shrine steps to read them.
	// =====================================================================================
	static Talk Stage1 => new(
		new L[] {
			new(1, "別踩到那條線。整片星圖都畫在石階上。", "Don't step on that line. The whole star chart is drawn across these steps."),
			new(0, "星圖？村子上面的星星一顆都看不到，原來是被你搬到這裡來了！", "A star chart? Not one star is left over the village. So you dragged them all down here!"),
			new(1, "借而已。天門之夜以前，我得把它們讀完。", "Only borrowed. I have to finish reading them before the Night of the Heaven's Gate."),
			new(0, "神社的婆婆說，沒有星星就定不了祭典的日子。你要借到什麼時候？", "The old lady at the shrine says we can't set the festival day without stars. How long are you keeping them?"),
			new(1, "讀完為止。", "Until I'm done."),
			new(0, "那是多久！", "And how long is that?"),
			new(1, "不知道。夢裡有個聲音要我讀，沒說要讀多久。我總不能為了一個小孩收起星圖。", "No idea. A voice in a dream told me to read them. It didn't say for how long. I'm not rolling up the chart for a kid."),
			new(0, "那我就打到你收為止！", "Then I'll fight you until you do!"),
		},
		new L[] {
			new(1, "這麼晚還上山。獵人？", "Climbing the mountain this late. A hunter?"),
			new(0, "沒有星星，夜裡認不了路。陷阱放了三天，一個都找不回來。", "No stars, no way to find the paths at night. I set traps three days ago and can't find a single one."),
			new(1, "那真是抱歉了。星星在我這裡。", "My apologies, then. The stars are with me."),
			new(0, "還來。", "Give them back."),
			new(1, "天門之夜以前不行。有個聲音要我在那之前把星象讀完。", "Not before the Night of the Heaven's Gate. A voice told me to finish reading them by then."),
			new(0, "我不管什麼聲音。今晚就還。", "I don't care about voices. Tonight."),
			new(1, "不行。", "No."),
			new(0, "那就打。", "Then we fight."),
		},
		new L[] {
			new(1, "山伏？這條參道晚上不好走。", "A yamabushi? This path is no place to be at night."),
			new(0, "我本來是看著北斗星走的～走到一半星星全不見了，現在連自己從哪裡來都不知道。", "I was following the Big Dipper~ Halfway up, every star vanished. Now I don't even know where I came from."),
			new(1, "那是我借走的。", "I borrowed them."),
			new(0, "星星可以借喔？那先借我北斗星就好，我想下山吃宵夜。", "You can borrow stars? Then just lend me the Big Dipper. I want to go down for a late-night snack."),
			new(1, "一顆也不能少。少一顆，星象就讀不準。", "Not a single one. With one missing, the reading goes wrong."),
			new(0, "可是我的晚飯在山下耶。", "But my dinner is down the mountain."),
			new(1, "你真想要，就自己從星圖裡拿。拿得到再說。", "If you want it that badly, take it out of the chart yourself. Then we'll talk."),
			new(0, "好喔，那我拿囉！", "Okay, I'm taking it!"),
		},
		new L[] {
			new(1, "帶著人偶上參道……這麼晚了，你不怕嗎？", "Climbing the shrine path with puppets... this late? Aren't you scared?"),
			new(0, "怕。可是孩子們說，星星的聲音是從這裡傳來的。", "I am. But the children say the stars' voices are coming from here."),
			new(1, "星星的聲音？", "The stars' voices?"),
			new(0, "它們說，天上空了一大塊，星星全擠在這片石階上，很吵。", "They say there's a big hole in the sky, and all the stars are crammed onto these steps, being noisy."),
			new(1, "你的人偶耳朵真好。沒錯，是我把星星請下來的。夢裡有人告訴我，天門之夜以前要讀完它們。", "Your puppets have good ears. Yes, I brought the stars down. Someone in a dream told me to read them all before the Night of the Heaven's Gate."),
			new(0, "孩子們想把星星送回去。", "The children want to send the stars back."),
			new(1, "我還沒讀完。想送回去，就先讓我看看它們有多少本事。", "I'm not done reading. If they want to send them back, let me see what they can do first."),
		});

	// =====================================================================================
	// Stage 2 — 四時繪卷. 千秋 is painting all four seasons into one valley.
	// =====================================================================================
	static Talk Stage2 => new(
		new L[] {
			new(1, "站住，你腳下那一塊還沒乾。", "Stop. The paint under your feet isn't dry yet."),
			new(0, "又是畫在地上的東西……這山谷怎麼同時在下雪又開櫻花？", "Another thing painted on the ground... Why is this valley snowing and blooming at the same time?"),
			new(1, "我在畫四季。春夏秋冬放進同一幅畫裡，畫完以前會有點亂。", "I'm painting the four seasons. Spring, summer, autumn and winter in one picture. It's a bit of a mess until I finish."),
			new(0, "有點？山下的稻子凍死了一半！", "A bit? Half the rice down the mountain froze to death!"),
			new(1, "……那我很抱歉。可是有人在夢裡跟我說，四季就要散了，要我趁現在把它們畫下來。", "...I'm sorry about that. But someone told me in a dream that the seasons are about to scatter, and to paint them while I still can."),
			new(0, "夢裡？參道上那個看星星的，也說他聽到聲音！", "In a dream? The stargazer on the shrine path said he heard a voice too!"),
			new(1, "他也是？那我更不能停了。不只我一個人聽見，就表示那是真的。", "Him too? Then I really can't stop. If I'm not the only one who heard it, it's real."),
			new(0, "真的假的我不管，先把季節放回去！", "Real or not, put the seasons back first!"),
		},
		new L[] {
			new(1, "你的弓，可以讓我畫一張嗎？一下就好。", "Your bow. May I paint it? Just for a moment."),
			new(0, "不行。山谷的雪是你下的？", "No. Is the snow in this valley your doing?"),
			new(1, "雪、櫻花、楓葉、蟬聲，都是我畫的。四季就要散了，我得把它們留在紙上。", "The snow, the blossoms, the maple leaves, the cicadas. All mine. The seasons are about to scatter, so I'm keeping them on paper."),
			new(0, "誰說的。", "Who says so?"),
			new(1, "夢裡的聲音。", "A voice in a dream."),
			new(0, "上面那個看星星的，也這麼說。", "The stargazer up there said the same."),
			new(1, "原來不只我……那我更要畫完。抱歉了，獵人，畫到一半的東西，我不給人碰。", "So it isn't just me... Then I have to finish. Sorry, hunter. Nobody touches a painting of mine halfway through."),
			new(0, "那就別怪我把紙射穿。", "Then don't blame me when I shoot through the paper."),
		},
		new L[] {
			new(1, "啊，別踩——算了，你踩的那塊，顏色反而好看。", "Ah, don't step on— never mind. The colour looks better where you stepped."),
			new(0, "這裡好奇怪喔～我剛剛在雪地裡撿到一顆熟透的柿子。", "This place is so weird~ I just found a ripe persimmon in the snow."),
			new(1, "那是秋天那一段的。你吃了？", "That's from the autumn part. Did you eat it?"),
			new(0, "吃了！冰冰的，很好吃。", "I did! Nice and cold. Delicious."),
			new(1, "那顆我還沒畫完。", "I hadn't finished painting that one."),
			new(0, "啊，對不起。所以這裡的季節，全是你畫出來的喔？", "Oh, sorry. So all the seasons here are things you painted?"),
			new(1, "夢裡有人拜託我，在四季散掉以前把它們畫下來。", "Someone in a dream asked me to paint the seasons before they scatter."),
			new(0, "又是夢喔？星星那個人也是做夢耶。", "A dream again? The star guy had a dream too."),
			new(1, "他在哪裡？", "Where is he?"),
			new(0, "被我打倒了，在參道上睡覺～", "I beat him. He's napping on the shrine path~"),
			new(1, "那我得小心了。畫完以前，誰都別想讓我放下筆。", "Then I'd better be careful. Until I finish, no one makes me put down my brush."),
		},
		new L[] {
			new(1, "好安靜的客人。你身邊那幾個孩子，也是人偶吧？可以借我畫嗎？", "What a quiet guest. Those little ones with you are puppets too, aren't they? May I paint them?"),
			new(0, "它們不喜歡被盯著看。這個山谷冷熱一直在換，孩子們頭很暈。", "They don't like being stared at. This valley keeps turning hot and cold. It makes the children dizzy."),
			new(1, "那是我在畫四季。抱歉，再一下就好。", "That's me painting the seasons. Sorry. Just a little longer."),
			new(0, "參道上的人也說「再一下」。他說，有聲音要他讀星星。", "The man on the shrine path said 'a little longer' too. He said a voice told him to read the stars."),
			new(1, "我也是聽了聲音。它說四季要散了……說話的樣子很累。", "I heard a voice too. It said the seasons would scatter... It sounded so tired."),
			new(0, "孩子們說，那個聲音是從很高的地方來的。", "The children say that voice comes from somewhere very high up."),
			new(1, "那我更得把畫畫完，好拿給它看。請別攔我。", "Then I have to finish all the more, so I can show it. Please don't stop me."),
			new(0, "孩子們會小心，不弄髒你的畫。", "The children will be careful not to smudge your painting."),
		});

	// =====================================================================================
	// Stage 3 — 雲上之路. 嵐 keeps gathering rain clouds; something above keeps drawing them away.
	// =====================================================================================
	static Talk Stage3 => new(
		new L[] {
			new(1, "哪來的小鬼，敢踩上我的雲！", "Where did this brat come from, walking on my clouds!"),
			new(0, "山下連下了七天雨，河都快滿出來了，是你吧！", "It's rained for seven days down there. The river's about to flood. That's you, isn't it!"),
			new(1, "是我又怎樣？雲是我聚的，雨愛下哪就下哪。", "So what if it is? I gather the clouds, the rain falls where it likes."),
			new(0, "你是不是也聽到什麼聲音？看星星的跟畫畫的，都說有人在夢裡叫他們做事。", "Do you hear a voice too? The stargazer and the painter both said someone in a dream told them what to do."),
			new(1, "那兩個傢伙也是？哼，我才不是聽誰的話。我是自己想聚雲。", "Those two as well? Hmph. I don't take orders. I gather clouds because I want to."),
			new(0, "那雲呢？你一聚好，就全往上面飄走了。", "Then where are the clouds going? The moment you gather them, they drift upward."),
			new(1, "你也看到了？我聚一團，上面就吸走一團。不知道是哪個混帳在偷我的雲！", "You saw that too? Every cloud I gather gets sucked up. Some bastard up there is stealing my clouds!"),
			new(0, "那你停手不就好了？", "Then why not just stop?"),
			new(1, "停手就輸了！在搞清楚之前，誰都別想叫我停——你也一樣！", "Stopping means losing! Until I figure this out, nobody tells me to stop. That includes you!"),
		},
		new L[] {
			new(1, "又一個不怕被雷劈的。", "Another one who isn't afraid of lightning."),
			new(0, "你的雷，劈壞了山下三間屋子。", "Your lightning broke three houses down the mountain."),
			new(1, "要賠找天上那傢伙賠！雲是我聚的，可是一聚好就被吸上去，害我只能一直聚！", "Take it up with whoever's in the sky! I gather the clouds, but they get sucked up the moment I'm done, so I keep gathering more!"),
			new(0, "被吸上去？", "Sucked up?"),
			new(1, "你自己看。", "See for yourself."),
			new(0, "看星星的跟畫畫的，也說夢裡有人叫他們做事。", "The stargazer and the painter said someone in a dream gave them orders too."),
			new(1, "我沒那麼好使喚。不過那聲音確實很吵。", "I'm not that easy to order around. Though that voice is noisy, I'll give you that."),
			new(0, "讓雨停。", "Stop the rain."),
			new(1, "想讓我停，就拿出本事來！", "If you want me to stop, show me what you've got!"),
		},
		new L[] {
			new(1, "喂！那是雲，不是床！給我起來！", "Hey! That's a cloud, not a bed! Get up!"),
			new(0, "好軟喔……我爬了好久的山，休息一下嘛。", "It's so soft... I've been climbing forever. Let me rest a bit."),
			new(1, "我是天氣之主嵐！在我的雲上睡午覺，你膽子不小！", "I am Arashi, lord of the weather! Napping on my clouds takes some nerve!"),
			new(0, "天氣之主？那為什麼一直下雨呀？我的衣服濕了三天了。", "Lord of the weather? Then why won't the rain stop? My clothes have been wet for three days."),
			new(1, "我在聚雲！可是不管聚多少都被往上吸走，只好一直聚！", "I'm gathering clouds! But however many I gather, they get sucked upward, so I have to keep at it!"),
			new(0, "喔～所以你是雲被拿走在生氣。跟星星那個人、畫畫那個人一樣，大家都被什麼東西搞得團團轉耶。", "Oh~ so you're mad because someone took your clouds. Like the star guy and the painter. Everyone's being spun around by something."),
			new(1, "誰團團轉了！我才沒有！", "Who's spinning around! Not me!"),
			new(0, "好好好，那你下完這場就休息一下嘛。", "Okay, okay. Then take a break after this rain."),
			new(1, "少囉嗦！想要晴天，先撐過這場暴雨！", "Quiet! You want clear skies? Survive this storm first!"),
		},
		new L[] {
			new(1, "小鬼，抱著一堆娃娃跑上雲來幹什麼？", "Kid, what are you doing up on the clouds with an armful of dolls?"),
			new(0, "孩子們的衣服一直乾不了。雨下太久，它們都快發霉了。", "The children's clothes never dry. It's rained so long they're starting to mould."),
			new(1, "發霉？那是你家的事。", "Mould? That's your problem."),
			new(0, "孩子們說，你的雲一直被拉到更高的地方。", "The children say your clouds keep being pulled somewhere higher."),
			new(1, "它們看得到？", "They can see that?"),
			new(0, "看得到。像有一根線，在上面拉著。", "They can. Like a string pulling from above."),
			new(1, "線？哼，要是讓我知道是誰在拉，我非劈了他不可。", "A string? Hmph. If I ever find out who's pulling it, I'll strike him down."),
			new(0, "參道的人和畫畫的人，也被同一根線拉著。", "The man on the shrine path and the painter are on the same string."),
			new(1, "我才不是什麼人偶！既然你這麼會看，就看看我的雷吧！", "I'm nobody's puppet! If you're so good at seeing things, take a look at my lightning!"),
		});
}
