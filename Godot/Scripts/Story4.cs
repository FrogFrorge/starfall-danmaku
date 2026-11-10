// Story: the endings shown after clearing the main game (Touhou style: a few pages of narration over the stage art).
namespace DanmakuSTG;

/** One ending page. Bg: stage background index (0..7), -1 the title art. Portrait: -1 none, 0..3 a player character,
 *  10 + i boss portrait i (0 天狼, 1 千秋, 2 嵐, 3 墨竹, 4 玄, 5 燁, 6 一索, 7..10 the beasts). */
public sealed record FEndingPage(int Bg, int Portrait, L[] Lines);
public sealed record FEnding(string Title, string TitleEn, FEndingPage[] Pages);

public static partial class Story
{
	/** Indexed by ECharacter. */
	public static readonly FEnding[] Endings =
	{
		// ---- 蓮 ----
		new("Ending No.01　星光回到參道", "Ending No.01  The Stars Return to the Shrine Path", new FEndingPage[] {
			new(0, -1, new L[] {
				new(0, "天門之夜過去了。", "The Night of the Heaven's Gate came and went."),
				new(0, "隔天傍晚，村子上空的星星一顆一顆亮了回來。", "The next evening, the stars over the village lit up again, one by one."),
			}),
			new(0, 0, new L[] {
				new(0, "神社的婆婆對著星空算出了祭典的日子，然後回頭把蓮罵了一頓。", "The old lady at the shrine worked out the festival day from the sky, then turned around and gave Ren an earful."),
				new(0, "「一個人跑上天宮？你知道大家找了你多久！」", "“Running off to the Heavenly Palace alone? Do you know how long everyone spent looking for you!”"),
			}),
			new(0, 0, new L[] {
				new(0, "蓮被罵得抬不起頭，嘴角卻怎麼也壓不下來。", "Ren couldn't lift his head under the scolding, but he couldn't stop smiling either."),
				new(0, "那天晚上，他把天上發生的事，一件一件說給婆婆聽。", "That night, he told her everything that had happened up there, one thing at a time."),
			}),
			new(1, 11, new L[] {
				new(0, "幾天後，一個背著畫具的男人來到村口，說是來替凍壞的稻田道歉。", "A few days later, a man carrying painting gear came to the village gate, saying he'd come to apologise for the frozen rice fields."),
				new(0, "他在神社的牆上，畫了一整面的秋收。", "He painted an entire autumn harvest across the shrine wall."),
			}),
			new(2, 12, new L[] {
				new(0, "雨在那天就停了。", "The rain had stopped that very day."),
				new(0, "據說有人在雲上聽見誰在抱怨：「早說嘛。」", "They say someone up in the clouds was heard grumbling: “Should've just said so.”"),
			}),
			new(5, -1, new L[] {
				new(0, "天宮的守護者還在睡。", "The guardian of the Heavenly Palace is still asleep."),
				new(0, "蓮在神社的日曆上，把明年的天門之夜圈了起來。", "On the shrine calendar, Ren circled next year's Night of the Heaven's Gate."),
				new(0, "這一次，他要自己走上去，把話說清楚。", "This time he'll walk up there himself, and they'll talk it through properly."),
			}),
		}),
		// ---- 蒼 ----
		new("Ending No.02　收回陷阱的早晨", "Ending No.02  The Morning He Collected His Traps", new FEndingPage[] {
			new(0, -1, new L[] {
				new(0, "天門之夜過後的早晨，雨停了。", "The morning after the Night of the Heaven's Gate, the rain had stopped."),
				new(0, "蒼沿著獸道，把放了好幾天的陷阱一個一個收回來。", "Sou followed the animal trails and brought in the traps he'd left out for days, one after another."),
			}),
			new(0, 1, new L[] {
				new(0, "大多數都空著。只有最後一個，困著一隻濕透的兔子。", "Most were empty. Only the last one held a rabbit, soaked to the skin."),
				new(0, "他看了一會兒，把牠放了。", "He looked at it for a while, then let it go."),
			}),
			new(2, 12, new L[] {
				new(0, "回到村子時，三間被雷劈壞的屋頂上，站著一個人正罵罵咧咧地補瓦。", "Back in the village, someone was standing on the three lightning-struck roofs, swearing as he patched the tiles."),
				new(0, "「看什麼看！我只是剛好路過！」", "“What are you looking at! I just happened to be passing by!”"),
				new(0, "蒼什麼也沒說，替他把梯子扶穩了。", "Sou said nothing and held the ladder steady for him."),
			}),
			new(5, -1, new L[] {
				new(0, "後來，有人在天宮的門口發現一支插在石縫裡的箭。", "Later, someone found an arrow lodged in a crack in the stone at the palace gate."),
				new(0, "箭上綁著一張紙條，只寫了兩個字：「醒了？」", "Tied to it was a note with just two words: “Awake yet?”"),
			}),
		}),
		// ---- 晴 ----
		new("Ending No.03　留一半的便當", "Ending No.03  Half a Lunch Box", new FEndingPage[] {
			new(0, 2, new L[] {
				new(0, "晴終於下了山，在看到的第一間飯館裡吃了三大碗飯。", "Haru finally made it down the mountain and ate three huge bowls of rice at the first eatery he saw."),
				new(0, "老闆問他從哪裡來。他想了想，說：「天上。」", "The owner asked where he'd come from. He thought about it and said, “The sky.”"),
			}),
			new(3, 13, new L[] {
				new(0, "回程經過四君子庭園，墨竹板著臉，塞給他一籃筍子。", "On his way back past the Garden of the Four Gentlemen, Mochiku, straight-faced, pushed a basket of bamboo shoots into his hands."),
				new(0, "「不是給你吃的。是拿去給天宮那位的。」", "“These are not for you. Take them to the one in the palace.”"),
			}),
			new(5, -1, new L[] {
				new(0, "於是晴背著便當和筍子，又爬了一次天宮。", "So Haru shouldered a lunch box and the shoots and climbed to the Heavenly Palace once more."),
				new(0, "守護者還在睡。晴在他身邊坐下，打開了便當。", "The guardian was still asleep. Haru sat down beside him and opened the lunch box."),
			}),
			new(5, 2, new L[] {
				new(0, "「一個人吃不好吃啦。等你醒來，我們再一起吃。」", "“Food doesn't taste good eaten alone. When you wake up, we'll eat together.”"),
				new(0, "他留下一半，自己吃掉另外一半。", "He left half, and ate the other half himself."),
				new(0, "下山的時候，晴發現自己第一次不覺得餓。", "On the way down, Haru noticed that, for the first time, he wasn't hungry."),
			}),
		}),
		// ---- 雪 ----
		new("Ending No.04　借出去的孩子", "Ending No.04  The Child He Lent Out", new FEndingPage[] {
			new(0, 3, new L[] {
				new(0, "回到家，雪把孩子們一個一個放回架上。", "Back home, Yuki set the children back on their shelf, one by one."),
				new(0, "有一個的線磨斷了，是在天宮最後那一場弄斷的。", "One of them had a worn-through string, snapped in that last fight at the palace."),
			}),
			new(0, 3, new L[] {
				new(0, "他花了一整晚重新穿線。", "He spent the whole night restringing it."),
				new(0, "天亮時，他手邊多了一個新的人偶，穿著白衣，看起來有點像某個人。", "By dawn there was a new puppet beside him, dressed in white, looking a little like someone he knew."),
			}),
			new(5, -1, new L[] {
				new(0, "雪帶著那個人偶，再一次走上天宮。", "Yuki carried that puppet up to the Heavenly Palace once more."),
				new(0, "守護者睡得很沉，身邊空空的。", "The guardian was sleeping deeply, with no one beside him."),
				new(0, "他把人偶放在守護者手邊，替它理好衣角。", "He placed the puppet by the guardian's hand and straightened its sleeves."),
				new(0, "「孩子們說，這樣您就不是一個人了。」", "“The children say this way, you won't be alone.”"),
			}),
			new(4, 14, new L[] {
				new(0, "下山的路上，五行仙壇的方士對他點了點頭。", "On the way down, the sorcerer of the Altar of the Five Elements gave him a nod."),
				new(0, "「人偶師，你又多了一個孩子。」", "“Puppeteer, you've gained another child.”"),
				new(0, "「不。那是借給燁先生的。」", "“No. That one is on loan to Yoh.”"),
			}),
		}),
	};

	/** Screen text used by the ending scene. */
	static readonly (string Zh, string En)[] EndingUi =
	{
		("全劇終", "The End"),
		("感謝遊玩", "Thank you for playing"),
		("Z　繼續", "Z  Continue"),
	};
}
