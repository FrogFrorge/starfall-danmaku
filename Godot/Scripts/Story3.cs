// Story, Extra and Extra-IF (see Story.cs).
namespace DanmakuSTG;

public static partial class Story
{
	// =====================================================================================
	// Extra midboss — 玄 again. Beyond the palace lies 天和閣; 一索 only gives up the seals to someone who beats him
	// at mahjong, and he has never lost. 玄 tests whether you're ready.
	// =====================================================================================
	static Talk ExMid => new(
		new L[] {
			new(1, "結界補好了，你卻還往上走。", "The barrier is mended, and yet you keep climbing."),
			new(0, "燁說，最舊的那道裂痕是他五百年前犯的錯。我想知道是什麼錯。", "Yoh said the oldest crack was a mistake he made five hundred years ago. I want to know what it was."),
			new(1, "他連這個都跟你說了。前面是天和閣，主人叫一索。五百年前，燁跟他賭了一局，十三枚天印就留在了那裡。最早的裂痕，就是從那時開始的。", "He even told you that. Ahead is the Tenhou Pavilion. Its master is called Isso. Five hundred years ago Yoh played a game with him, and the thirteen Heaven Seals stayed there. That's when the first crack appeared."),
			new(0, "那我去把天印拿回來！", "Then I'll go get the seals back!"),
			new(1, "一索不會白白交出來。他只認牌桌上的輸贏，而他從來沒輸過。", "Isso won't just hand them over. He only trusts what's decided at the table, and he has never lost."),
			new(0, "那贏就好啦！", "Then I'll just win!"),
			new(1, "說得容易。你連我這三卦都過不了，就別去讓他看笑話。", "Easy to say. If you can't get past my three divinations, don't go and make a fool of yourself in front of him."),
		},
		new L[] {
			new(1, "你的箭，還想往哪裡射？", "Where else do you mean to shoot?"),
			new(0, "燁說還有一道裂沒補。我順路。", "Yoh said there's a crack left. It's on my way."),
			new(1, "順路……前面是天和閣。一索手上有十三枚天印，是五百年前從燁那裡贏來的，結界缺的就是它們。", "On your way... Ahead is the Tenhou Pavilion. Isso holds thirteen Heaven Seals he won from Yoh five hundred years ago. They're what the barrier is missing."),
			new(0, "那就拿回來。", "Then I'll take them back."),
			new(1, "他不收箭，只收牌。要拿回天印，得在他的牌桌上贏他。", "He doesn't take arrows, only tiles. To get the seals back, you have to beat him at his table."),
			new(0, "我不輸。", "I won't lose."),
			new(1, "每個坐上他牌桌的人都這麼說。讓我用三卦，看看你的運。", "Everyone who sits at his table says that. Let me read your luck with three divinations."),
		},
		new L[] {
			new(1, "山伏，你又來做什麼？", "Yamabushi, what are you doing here again?"),
			new(0, "燁睡著了，沒人請我吃飯～我聞到前面有很香的味道，就一路走過來了！", "Yoh fell asleep and nobody's feeding me~ I smelled something delicious up ahead, so I followed it!"),
			new(1, "那是天和閣的廚房。主人一索是個愛賭的雀聖，五百年前，燁的十三枚天印輸給了他。", "That's the kitchen of the Tenhou Pavilion. Its master, Isso, is a mahjong saint who loves a gamble. Five hundred years ago Yoh's thirteen Heaven Seals went to him."),
			new(0, "天印是什麼？好吃嗎？", "What's a Heaven Seal? Is it tasty?"),
			new(1, "不能吃。是補結界最早那道裂痕要用的東西。你一進去，一索就會要你上牌桌，不贏他，他是不會交出來的。", "You can't eat it. It's what the barrier's oldest crack needs. The moment you walk in, Isso will have you at his table, and he won't hand them over unless you win."),
			new(0, "那我不要賭，我只要吃飯！", "Then I won't gamble. I just want to eat!"),
			new(1, "他不會只請你吃飯。先讓我看看，你有沒有本事坐上那張桌子。", "He won't just feed you. First, let me see if you're good enough to sit at that table."),
		},
		new L[] {
			new(1, "人偶師，結界已經補好，你為何還往前走？", "Puppeteer, the barrier is mended. Why do you keep going?"),
			new(0, "燁先生說的那點光，孩子們找到了。在前面。", "The children found the faint light Yoh spoke of. It's up ahead."),
			new(1, "天和閣。那光是十三枚天印。五百年前燁跟主人一索對賭，天印就留在那裡了。", "The Tenhou Pavilion. That light is thirteen Heaven Seals. Five hundred years ago Yoh gambled against its master, Isso, and the seals stayed there."),
			new(0, "孩子們想把它們帶回去，讓燁先生不用再背這個債。", "The children want to bring them back, so Yoh won't have to carry that debt anymore."),
			new(1, "一索只用牌局說話。贏不了他，天印一枚也帶不走。", "Isso only speaks through games. If you can't beat him, you won't take a single seal."),
			new(0, "我會怕。可是孩子們想去。", "I'm scared. But the children want to go."),
			new(1, "那就先過我這三卦吧。", "Then get through my three divinations first."),
		});

	// =====================================================================================
	// Extra — 一索, a good man. He won the seals so Yoh couldn't trade them for never-losing luck and wear himself out
	// guarding alone, and has kept them safe since. His game only tests whether you can carry them; losing costs nothing.
	// =====================================================================================
	static Talk StageEx => new(
		new L[] {
			new(1, "喲，玄放你上來了。坐吧，桌子都擺好了。", "Well, Gen let you through. Have a seat, the table's all set."),
			new(0, "我不是來打牌的！十三枚天印，請你還給燁！", "I'm not here to play! Please give the thirteen Heaven Seals back to Yoh!"),
			new(1, "還，當然要還。我替他保管了五百年，就等這一天。", "Of course they go back. I've kept them for him for five hundred years, waiting for this day."),
			new(0, "保管？玄說是你贏走的。", "Kept them? Gen said you won them."),
			new(1, "是贏來的。那天燁想拿天印換一份永遠不輸的運氣，好一個人守著結界到永遠。我不能讓他換。", "I did win them. That day Yoh wanted to trade the seals for luck that never loses, so he could guard the barrier alone forever. I couldn't let him make that trade."),
			new(0, "為什麼？", "Why not?"),
			new(1, "一個人不可能永遠不輸，他會把自己撐壞的。所以我贏下天印，替他收著，等他願意找人幫忙。你看，他不就找了你？", "No one alone can never lose. He'd have broken himself. So I won the seals and kept them for him until he was willing to ask for help. And look, he found you."),
			new(0, "那現在就還給我吧！", "Then give them to me now!"),
			new(1, "天印很重，拿不穩的人會被壓垮。陪我打一局吧。你贏了，十三枚全帶走；輸了也沒關係，吃頓飯，下次再來。", "The seals are heavy. Anyone who can't hold them steady gets crushed. Play one game with me. Win, and all thirteen go with you. Lose, and that's fine too: have dinner and come back another time."),
		},
		new L[] {
			new(1, "臉這麼冷，很適合打牌。", "A face that cold. Made for cards."),
			new(0, "我不賭。天印。", "I don't gamble. The seals."),
			new(1, "天印會還。可是得先確定，你拿得動。", "The seals will go back. But first I need to know you can carry them."),
			new(0, "玄說你從沒輸過。", "Gen says you've never lost."),
			new(1, "沒錯。燁那天也是不肯認輸，想一個人扛下所有事。", "That's right. Yoh wouldn't admit defeat that day either. He wanted to carry everything alone."),
			new(0, "你留著天印，是在等他找人幫忙。", "You kept the seals, waiting for him to find help."),
			new(1, "你這人眼睛真利。規矩很簡單：贏我，天印你帶回去。", "Sharp eyes. The rule is simple: beat me, and you take the seals home."),
			new(0, "射箭也是賭。我沒射偏過。", "Every shot is a gamble. I've never missed."),
			new(1, "哈哈！那就看你能不能聽牌到最後！", "Hahaha! Then let's see if you can stay one tile from winning to the very end!"),
		},
		new L[] {
			new(1, "山伏也來雀莊？先說好，輸了要洗碗喔。", "A yamabushi in a mahjong parlour? Fair warning: the loser does the dishes."),
			new(0, "我是聞到味道來的！還有，玄說燁的天印在你這裡。", "I followed the smell! Also, Gen says Yoh's seals are here."),
			new(1, "在，一枚也沒少。五百年前，他想用天印換我的不敗之運。", "They are, every one of them. Five hundred years ago he wanted to trade them for my unbeatable luck."),
			new(0, "為什麼要換那個？", "Why would he want that?"),
			new(1, "他想一個人永遠不輸地守著結界。一個人哪可能永遠不輸？所以我贏了天印，替他好好收著。", "He wanted to guard the barrier alone and never lose. How can anyone alone never lose? So I won the seals and kept them safe for him."),
			new(0, "所以你一直在等他找人幫忙？", "So you've been waiting for him to ask for help?"),
			new(1, "誰知道呢。規矩是：贏我，十三枚全給你。", "Who knows. The rule: beat me and all thirteen are yours."),
			new(0, "那再加一桌菜！", "Then add a full dinner on top!"),
			new(1, "行。贏了，就是十三枚天印加十三道菜。輸了，碗歸你洗。", "Done. Win, and it's thirteen Heaven Seals and thirteen dishes. Lose, and the dishes are yours to wash."),
		},
		new L[] {
			new(1, "小朋友，帶著人偶來打牌？", "Little one, you brought puppets to a card game?"),
			new(0, "孩子們說，您的牌在發光。", "The children say your tiles are glowing."),
			new(1, "好眼光。這十三張，是燁的天印。我替他收了五百年。", "Good eye. These thirteen are Yoh's Heaven Seals. I've kept them for him for five hundred years."),
			new(0, "燁先生那時想用它們換什麼呢？", "What did Yoh want to trade them for back then?"),
			new(1, "換我的運氣。他想一個人守到永遠，永遠不輸。我不想看他那樣，就贏下了天印。", "My luck. He wanted to guard it alone forever and never lose. I didn't want to see him like that, so I won the seals."),
			new(0, "您是怕他……一直一個人嗎？", "Were you afraid he'd... always be alone?"),
			new(1, "人偶師，你看得太清楚了。天印很重，我得確定你們拿得動。陪我打一局吧。", "You see too clearly, puppeteer. The seals are heavy; I need to be sure you can carry them. Play a game with me."),
			new(0, "孩子們，我們坐下吧。", "Children, let's sit down."),
		});

	static Talk StageExAfter => new(
		new L[] {
			new(1, "和了！……不對，是你和了。十三枚天印，拿去。", "Mahjong! ...No, wait, you won. The thirteen seals are yours."),
			new(0, "就這樣？你不賴帳？", "Just like that? No welching?"),
			new(1, "賭品要好。而且這局，我等了五百年。", "A gambler keeps his word. Besides, I've waited five hundred years for this game."),
			new(0, "等燁找到可以幫他的人？", "Waiting for Yoh to find someone to help him?"),
			new(1, "你要這麼想也行。天印拿回去，最舊的裂痕也會合上。", "Think of it that way if you like. Take the seals back, and the oldest crack will close."),
			new(0, "你其實一直在幫他吧。", "You've been helping him all along, haven't you."),
			new(1, "下次帶他來打牌。三缺一。", "Bring him to play next time. We're one short for a table."),
		},
		new L[] {
			new(1, "漂亮。每一箭都聽牌。", "Beautiful. Every shot one tile from winning."),
			new(0, "天印。", "The seals."),
			new(1, "拿去，十三枚，一枚不少。", "Here. Thirteen, not one missing."),
			new(0, "你早就想還了。", "You wanted to give them back all along."),
			new(1, "我在等值得還的人。那傢伙終於肯讓別人幫忙了。", "I was waiting for someone worth giving them to. That fool finally let someone help him."),
			new(0, "你們兩個，都很麻煩。", "You two are both a pain."),
			new(1, "哈哈！這句我收下。改天來打牌。", "Hahaha! I'll take that. Come play sometime."),
		},
		new L[] {
			new(1, "輸了輸了！說好的，十三枚天印，還有十三道菜！", "I lose, I lose! As promised: thirteen Heaven Seals and thirteen dishes!"),
			new(0, "耶！真的有菜！", "Yay! There really is food!"),
			new(1, "天和閣的廚房可是天界第一。", "The Tenhou Pavilion's kitchen is the best in the heavens."),
			new(0, "那等燁醒來，也帶他來吃！", "Then once Yoh wakes up, I'll bring him here to eat too!"),
			new(1, "他？他五百年沒踏進這裡了。", "Him? He hasn't set foot here in five hundred years."),
			new(0, "那就更要來啦，五百年沒吃，一定很餓。", "All the more reason. Five hundred years without eating, he must be starving."),
			new(1, "你這山伏……好，我把位子留著。", "You yamabushi... Fine. I'll keep a seat for him."),
		},
		new L[] {
			new(1, "你贏了，人偶師。天印請帶回去吧。", "You win, puppeteer. Please take the seals back."),
			new(0, "孩子們說，您其實很捨不得。", "The children say you don't really want to let them go."),
			new(1, "陪了我五百年的東西，哪有捨得的。", "They've kept me company for five hundred years. Of course I don't."),
			new(0, "那您可以去天宮看它們。和燁先生一起。", "Then you can visit them at the palace. With Yoh."),
			new(1, "你真是溫柔得可怕。好，就這麼說定了。", "You're frighteningly kind. All right, it's a deal."),
			new(0, "孩子們，回家吧。天印也一起。", "Children, let's go home. The seals are coming with us."),
		});

	// =====================================================================================
	// Extra-IF — what if the hero was too late: the barrier broke last night and the four perils are loose.
	// Each character meets one beast on the road (蓮 窮奇, 蒼 檮杌, 晴 饕餮, 雪 混沌), then all four in relay.
	// =====================================================================================
	static Talk IfMidQiongqi => new(
		new L[] {
			new(1, "喲，好正直的眼神。我最喜歡吃你這種人了。", "Well, what honest eyes. People like you are my favourite meal."),
			new(0, "天宮是你們弄塌的？", "Did you lot bring the palace down?"),
			new(1, "是結界自己碎的，我們只是出來透透氣。我是窮奇——吵架的時候，我專吃講理的那一邊。", "The barrier broke on its own. We just came out for some air. I'm Qiongqi. When people argue, I eat whoever's in the right."),
			new(0, "燁在哪裡！", "Where's Yoh!"),
			new(1, "上面，和我的兄弟們一起。饕餮、檮杌、混沌。好心勸你：回頭吧，正直的人在上面活不久。", "Up there, with my brothers. Taotie, Taowu, Hundun. A friendly warning: turn back. Honest people don't last long up there."),
			new(0, "我才不回頭！", "I'm not turning back!"),
			new(1, "哈！那就從頭開始，一口一口吃掉你！", "Ha! Then I'll start with your head and eat you one bite at a time!"),
		},
		None, None, None);

	static Talk IfMidTaowu => new(
		None,
		new L[] {
			new(1, "射手。你也是來把我們壓回去的？", "Archer. Have you come to push us back under too?"),
			new(0, "結界碎了。你是什麼。", "The barrier's broken. What are you?"),
			new(1, "檮杌。四神鎮著的四凶之一。不聽教、不回頭——你們人是這麼說我的。", "Taowu. One of the Four Perils the Four Gods held down. Won't be taught, won't turn back. That's what you humans say about me."),
			new(0, "燁在哪。", "Where's Yoh?"),
			new(1, "在上面，倒在我兄弟們中間。饕餮、混沌、窮奇。你去了也沒用。", "Up there, fallen among my brothers. Taotie, Hundun, Qiongqi. Going won't help you."),
			new(0, "那就先過你。", "Then I'll get past you first."),
			new(1, "好。我也從不退。", "Good. I never back down either."),
		},
		None, None);

	static Talk IfMidTaotie => new(
		None, None,
		new L[] {
			new(1, "嗯？好香……你身上有五份力量的味道。", "Hm? Something smells good... You smell of five shares of power."),
			new(0, "你是誰？天宮怎麼塌成這樣……燁呢？", "Who are you? Why has the palace collapsed like this... Where's Yoh?"),
			new(1, "我是饕餮。結界昨晚碎了，那個守護者也倒了。壓在四神底下幾千年，總算能出來吃頓飽的。", "I'm Taotie. The barrier broke last night and the guardian fell with it. Thousands of years under the Four Gods, and finally I can eat my fill."),
			new(0, "你在吃什麼？那是天宮的柱子耶！", "What are you eating? That's one of the palace's pillars!"),
			new(1, "柱子、雲、星星，什麼都能吃。上面還有我的兄弟們——檮杌、混沌、窮奇。回頭吧，他們沒我這麼好說話。", "Pillars, clouds, stars, it's all food. My brothers are further up: Taowu, Hundun, Qiongqi. Turn back. They're not as easygoing as me."),
			new(0, "才不要！我要把燁找出來！", "No way! I'm going to find Yoh!"),
			new(1, "那就比比看，誰的胃口比較大！", "Then let's see whose appetite is bigger!"),
		},
		None);

	static Talk IfMidHundun => new(
		None, None, None,
		new L[] {
			new(1, "人偶。你帶著好多人偶。", "Puppets. You've brought so many puppets."),
			new(0, "孩子們說，您沒有臉，可是您在笑。", "The children say you have no face, but you're smiling."),
			new(1, "我是混沌。沒有眼睛，也看得見你；沒有耳朵，也聽見了結界碎掉的聲音。", "I am Hundun. With no eyes, I still see you; with no ears, I still heard the barrier break."),
			new(0, "燁先生在哪裡？", "Where is Yoh?"),
			new(1, "他倒下了。四神一倒，我們四個就出來了。兄弟們在上面——饕餮、檮杌、窮奇。", "He fell. When the Four Gods fell, the four of us came out. My brothers are above: Taotie, Taowu, Qiongqi."),
			new(0, "孩子們想帶燁先生回家。", "The children want to take Yoh home."),
			new(1, "那就陪我跳一支舞，人偶師。跳完還站得住，就往上走。", "Then dance with me, puppeteer. If you're still standing when it ends, go on up."),
		});

	// the relay: speakers 2 饕餮, 3 檮杌, 4 混沌, 5 窮奇
	static Talk StageIf => new(
		new L[] {
			new(2, "來了來了！窮奇說的那個正直小鬼！", "Here he comes! The honest brat Qiongqi was talking about!"),
			new(3, "打倒了窮奇，還敢往上走。", "Beat Qiongqi and still came up."),
			new(0, "你們就是四凶？燁在哪裡！", "So you're the Four Perils? Where's Yoh!"),
			new(4, "在我們身後。結界碎了，守護者倒在碎片裡。", "Behind us. The barrier broke, and the guardian lies among the pieces."),
			new(5, "我說過了吧？正直的人在上面活不久。", "Didn't I tell you? Honest people don't last long up here."),
			new(0, "為什麼要出來搗亂！結界壓著你們，山下的人才能好好過日子！", "Why come out and wreck everything! The barrier held you down so people below could live in peace!"),
			new(3, "我們被壓了幾千年。沒有人問過我們，想不想好好過日子。", "We were held down for thousands of years. No one ever asked whether we wanted to live in peace."),
			new(0, "那也不能拿山下的人出氣。", "That still doesn't mean you can take it out on the people below."),
			new(2, "所以啊，你身上那五份力量，正好讓我們吃個飽！", "So anyway, those five shares of yours will make a nice full meal!"),
			new(0, "休想！我一個一個打回去！", "Not a chance! I'll beat you back, one at a time!"),
			new(5, "好啊，一個一個來。最後，四個一起！", "Sure, one at a time. And at the end, all four together!"),
		},
		new L[] {
			new(3, "你果然上來了。", "So you did come up."),
			new(0, "燁在哪。", "Where's Yoh?"),
			new(2, "哇，好冷的人。檮杌，就是他打贏你？", "Wow, what a cold one. Taowu, this is the guy who beat you?"),
			new(5, "眼神真直，我喜歡。", "Such straight eyes. I like him."),
			new(4, "守護者在後面。四神倒了，結界碎了。你要怎麼做？", "The guardian is behind us. The Four Gods have fallen, the barrier is broken. What will you do?"),
			new(0, "把你們射回原位。用我身上的五份力量。", "Shoot you all back where you belong. With the five shares I carry."),
			new(3, "我們不會退。你也不會。那就只剩一條路。", "We won't back down. Neither will you. That leaves only one way."),
			new(0, "我只需要四箭。", "I only need four arrows."),
			new(5, "哈哈哈！四箭？那就一箭一箭試試！", "Hahaha! Four arrows? Then let's try them one at a time!"),
		},
		new L[] {
			new(2, "又是你！剛剛還沒吃夠嗎？", "You again! Weren't you full yet?"),
			new(0, "還沒！我還要找燁！", "Not yet! I still have to find Yoh!"),
			new(5, "這山伏怎麼回事，一點都不怕。", "What's with this yamabushi? Not scared in the slightest."),
			new(4, "他心裡沒有怕，只有餓。", "There's no fear in him. Only hunger."),
			new(3, "結界碎了，守護者倒了，我們出來了。你一個人能做什麼？", "The barrier's broken, the guardian has fallen, and we're out. What can you do alone?"),
			new(0, "把你們打回去，把燁叫起來，然後大家一起吃飯！你們也可以來！", "Beat you back, wake Yoh up, and then we all eat together! You can come too!"),
			new(2, "這、這是在請我們吃飯？", "Is- is he inviting us to dinner?"),
			new(5, "先打贏再說吧，小鬼！", "Win first, then talk, brat!"),
		},
		new L[] {
			new(4, "人偶師。你跳完舞了，還站著。", "Puppeteer. The dance is over, and you're still standing."),
			new(0, "孩子們都在。", "The children are all here."),
			new(2, "哦？混沌居然放他上來？", "Oh? Hundun actually let him up here?"),
			new(5, "好安靜的小鬼。你不怕嗎？", "Such a quiet kid. Aren't you scared?"),
			new(0, "怕。可是孩子們說，你們也很寂寞。", "I am. But the children say you're lonely too."),
			new(3, "寂寞？被壓在結界底下幾千年，只剩下恨。", "Lonely? After thousands of years under the barrier, all that's left is hate."),
			new(0, "那麼，等一切結束，請和燁先生把話說清楚。", "Then when this is over, please talk it out with Yoh."),
			new(4, "說話……我們說的話，從來沒有人聽。若你贏了，我們就聽你說。", "Talk... No one has ever listened to what we say. If you win, we'll listen to you."),
		});

	static Talk StageIfAfter => new(
		new L[] {
			new(5, "輸了。正直的人，原來也能活著上來啊。", "We lost. So honest people can make it up here alive after all."),
			new(0, "我會把你們封回去。可是這次不是用恨，是用約定。", "I'm going to seal you back. But this time not with hate. With a promise."),
			new(3, "約定？", "A promise?"),
			new(0, "下一次天門之夜，我會上來看你們。不會再讓你們被忘掉。", "On the next Night of the Heaven's Gate, I'll come up to see you. I won't let you be forgotten again."),
			new(2, "那、那順便帶點吃的！", "Th- then bring some food while you're at it!"),
			new(4, "結界重新亮起來了。守護者也快醒了。", "The barrier is lighting up again. The guardian will wake soon."),
		},
		new L[] {
			new(3, "我退了。第一次。", "I backed down. For the first time."),
			new(0, "你沒退。你只是輸了。", "You didn't back down. You just lost."),
			new(3, "這兩個有什麼不一樣？", "What's the difference?"),
			new(0, "退了就不會再來。輸了，下次還能再比。", "Back down, and you never come again. Lose, and you can have another go next time."),
			new(5, "哈！這射手原來會說人話。", "Ha! So the archer can talk like a person."),
			new(4, "結界合上了。我們回去睡吧。這一次，不是被壓著，是自己躺下。", "The barrier has closed. Let's go back to sleep. This time not held down, but lying down ourselves."),
		},
		new L[] {
			new(2, "吃、吃不下了……我認輸。", "I- I can't eat another bite... I give up."),
			new(0, "好耶！那等燁醒了，大家一起吃飯！", "Yay! Then once Yoh wakes up, we'll all eat together!"),
			new(5, "被封住的傢伙要怎麼吃飯啦！", "How are sealed-up monsters supposed to eat dinner!"),
			new(0, "天門之夜我來接你們！一年一次，請你們吃大餐！", "I'll come get you on the Night of the Heaven's Gate! Once a year, a big feast on me!"),
			new(3, "奇怪的人類。", "Strange human."),
			new(4, "那就約好了。結界合上之前，讓我們記住這個味道。", "Then it's a promise. Before the barrier closes, let us remember this taste."),
		},
		new L[] {
			new(4, "你的舞，跳得比我好。", "You danced better than I did."),
			new(0, "是孩子們在跳。現在，可以說話了嗎？", "It was the children dancing. Can we talk now?"),
			new(3, "我們只是想被看見。", "We only wanted to be seen."),
			new(2, "還想吃飽一點。", "And to eat a little more."),
			new(5, "還想有人聽我們吵架。", "And for someone to listen to us argue."),
			new(0, "孩子們會記得你們。燁先生醒來，也會聽的。", "The children will remember you. When Yoh wakes, he'll listen too."),
			new(4, "謝謝你，人偶師。結界合上了。晚安。", "Thank you, puppeteer. The barrier is closing. Good night."),
		});
}
