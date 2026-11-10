// Story, stages 4-6 (see Story.cs).
namespace DanmakuSTG;

public static partial class Story
{
	// =====================================================================================
	// Stage 4 — 四君子庭園. 墨竹 draws the mountain's plant spirit into his garden "by heaven's order".
	// =====================================================================================
	static Talk Stage4 => new(
		new L[] {
			new(1, "擅自闖入，有失禮數。閣下所為何來？", "Entering uninvited is ill-mannered. What brings you here?"),
			new(0, "山裡的樹全都往你這裡長，連路都被樹根擋住了。是你吧？", "Every tree on the mountain is growing toward this place. The roots have blocked the paths. That's you, right?"),
			new(1, "在下奉天命，替山守住草木之氣。", "I am keeping the mountain's plant spirit safe, by heaven's order."),
			new(0, "天命？是不是夢裡的聲音？", "Heaven's order? A voice in a dream, by any chance?"),
			new(1, "閣下怎麼知道？", "How would you know that?"),
			new(0, "看星星的、畫畫的、叫雨的那個，全都聽到了。", "The stargazer, the painter and the rain guy all heard it."),
			new(1, "三人皆然……星辰、四時、天候，再加上在下守的草木之氣，全都被引往天頂。看來不是天命，是有人在收東西。", "All three of them... The stars, the seasons, the weather, and the plant spirit I keep, all drawn toward the sky. That isn't heaven's order. Someone is collecting."),
			new(0, "收去幹嘛？", "Collecting it for what?"),
			new(1, "在下不知。五行仙壇的玄精通天象，或許清楚。", "I do not know. Gen of the Altar of the Five Elements knows the heavens well. He may."),
			new(0, "那你讓開，我去問他。", "Then let me through and I'll go ask him."),
			new(1, "天命未解之前，在下仍得守住此地。要過去，請先勝過在下。", "Until this order is understood, I must still hold this place. If you wish to pass, you must first defeat me."),
		},
		new L[] {
			new(1, "持弓入園，殺氣太重。", "You enter a garden carrying a bow. Far too much killing intent."),
			new(0, "林子裡的獸道全被樹根封了。我要過去。", "Every animal trail in the woods is sealed off by roots. I'm going through."),
			new(1, "那是在下聚的草木之氣。天命所託，不能讓。", "That is the plant spirit I have gathered. Heaven entrusted it to me. I cannot give way."),
			new(0, "天命。夢裡的？", "Heaven. From a dream?"),
			new(1, "閣下也聽過？", "You have heard it too?"),
			new(0, "我沒有。是我一路打倒的人聽過。", "No. The people I beat on the way here did."),
			new(1, "一路打倒……難怪閣下身上的氣這麼雜。星、季、雲，全纏在閣下身上。", "Beat on the way... No wonder your spirit is so mixed. Stars, seasons, clouds, all tangled around you."),
			new(0, "我不在乎。讓路。", "I don't care. Move."),
			new(1, "若天命要閣下來取，在下更該確認閣下取得起。請。", "If heaven means for you to take it, all the more reason I make sure you can carry it. Please."),
		},
		new L[] {
			new(1, "這位修行者，入園之前，請先把鞋上的泥擦掉。", "Traveller, please wipe the mud off your shoes before entering the garden."),
			new(0, "啊，抱歉抱歉～你家竹子長得好好喔，筍子可以挖嗎？", "Ah, sorry, sorry~ Your bamboo's growing so well. Can I dig up some shoots?"),
			new(1, "竹乃君子，豈可食之。", "Bamboo is a gentleman. One does not eat a gentleman."),
			new(0, "可是很好吃耶。對了，山上的樹全朝這裡長，害我繞了好遠的路。你也是被夢叫來做這個的嗎？", "But they're tasty. Oh, right, every tree on the mountain grows this way, so I had to take a huge detour. Did a dream tell you to do this too?"),
			new(1, "閣下何以得知？", "How do you know that?"),
			new(0, "前面三個人都是這樣啊。星星、季節、雲，現在是樹。好像有人在湊東西。", "The three people before you were all like that. Stars, seasons, clouds, and now trees. It's like someone is collecting a set."),
			new(1, "湊……確實。在下的草木之氣，也日夜往天頂流去。", "Collecting... Indeed. My plant spirit has been flowing toward the sky day and night."),
			new(0, "往天上？上面是不是有人在等著吃？", "Toward the sky? Is someone up there waiting to eat it?"),
			new(1, "閣下說得粗俗，倒也未必錯。要知道上面是誰，可去五行仙壇問玄。只是，在下的職責還沒卸下。", "Crudely put, but perhaps not wrong. To learn who it is, ask Gen at the Altar of the Five Elements. But my duty here is not yet done."),
			new(0, "那我先陪你練一下囉！", "Then I'll spar with you first!"),
		},
		new L[] {
			new(1, "客人有禮。是來賞花的嗎？", "A polite guest. Have you come to view the flowers?"),
			new(0, "花很漂亮。可是孩子們說，山裡的草木都很累。", "They're beautiful. But the children say the plants on the mountain are very tired."),
			new(1, "人偶也懂得草木的累嗎？", "Puppets can tell when plants are tired?"),
			new(0, "它們的身體是木頭做的。", "Their bodies are made of wood."),
			new(1, "原來如此。在下奉天命，聚集山中靈氣，守到天門之夜。", "I see. By heaven's order, I am gathering the mountain's spirit and keeping it until the Night of the Heaven's Gate."),
			new(0, "星星、季節、雲，也都被人收著。孩子們說，最後全往同一個地方去。", "The stars, the seasons and the clouds are being kept too. The children say they all end up going to the same place."),
			new(1, "天頂。在下也感覺到了。五行仙壇的玄比誰都清楚天上的事，閣下可以去問他。", "The top of the sky. I have felt it as well. Gen of the Altar of the Five Elements knows the heavens better than anyone. You may ask him."),
			new(0, "那，可以讓我們過去嗎？", "Then may we pass?"),
			new(1, "天命還在在下肩上。要過去，得先讓在下看見，閣下擔得起。", "Heaven's order still rests on my shoulders. To pass, you must first show me you can bear it."),
		});

	static Talk Stage4After => new(
		new L[] {
			new(1, "梅、蘭、竹、菊，竟一一敗在閣下手上。", "Plum, orchid, bamboo, chrysanthemum... each one beaten by you."),
			new(1, "奇怪。在下的靈氣沒有回到山裡，反而流到閣下身上了。", "Strange. My spirit did not return to the mountain. It flowed into you instead."),
			new(0, "我身上？難怪從剛才開始一直在發燙。", "Into me? So that's why I've felt hot since a moment ago."),
			new(1, "閣下一路打倒的人，他們的力量恐怕都在閣下這裡。往五行仙壇去吧，玄會告訴你為什麼。", "The power of everyone you defeated on your way is probably with you now. Go to the Altar of the Five Elements. Gen will tell you why."),
			new(0, "好。謝了，書生。", "Got it. Thanks, scholar."),
			new(1, "墨竹。在下名叫墨竹。", "Mochiku. My name is Mochiku."),
		},
		new L[] {
			new(1, "好箭。在下輸了。", "Fine archery. I have lost."),
			new(0, "你的氣，往我這邊來了。", "Your spirit is coming my way."),
			new(1, "閣下也感覺到了？星、季、雲，加上在下的草木。閣下一路收集的，恐怕不是巧合。", "You feel it too? Stars, seasons, clouds, and now my plants. What you have been gathering is no coincidence."),
			new(0, "玄在哪。", "Where's Gen?"),
			new(1, "往上，五行仙壇。他說話喜歡繞圈子，請耐著性子。", "Up the mountain, at the Altar of the Five Elements. He likes to talk in circles. Be patient with him."),
			new(0, "我盡量。", "I'll try."),
		},
		new L[] {
			new(1, "在下的庭園，被閣下踩得一塌糊塗。", "You have trampled my garden into a ruin."),
			new(0, "對不起嘛～咦，我怎麼又餓了，剛剛才吃過饅頭。", "Sorry~ Huh, why am I hungry again? I just ate a bun."),
			new(1, "那不是餓。在下的草木之氣流進了閣下體內，還有星辰、四時、天候。", "That is not hunger. My plant spirit has flowed into you, along with the stars, the seasons and the weather."),
			new(0, "我吃了那麼多東西喔？", "I ate all that?"),
			new(1, "算是吧。五行仙壇的玄知道這是怎麼回事，去問他。", "In a sense. Gen of the Altar of the Five Elements knows what this means. Go and ask him."),
			new(0, "好！順便問他有沒有吃的。", "Okay! I'll ask if he has any food while I'm at it."),
		},
		new L[] {
			new(1, "人偶師，在下的四君子都向你低頭了。", "Puppeteer, all four of my gentlemen bow to you."),
			new(0, "孩子們說，你的花其實很溫柔。", "The children say your flowers are actually gentle."),
			new(1, "多謝。那聲音說，要守到有人來取。在下一直以為，取的是天。", "Thank you. The voice told me to keep it until someone came to take it. I always assumed that someone was heaven."),
			new(0, "不是嗎？", "It isn't?"),
			new(1, "靈氣現在在閣下身上。也許，等的就是你。去五行仙壇問玄吧。", "The spirit is with you now. Perhaps it was you it was waiting for. Go ask Gen at the Altar of the Five Elements."),
			new(0, "孩子們，我們走。", "Children, let's go."),
		});

	// =====================================================================================
	// Stage 5 — 五行仙壇. 玄 knows the plan: 燁's barrier, power that only follows a victor.
	// =====================================================================================
	static Talk Stage5 => new(
		new L[] {
			new(1, "你來了。比卦象晚了一刻。", "You've come. A quarter-hour later than the divination said."),
			new(0, "你早就知道我會來？那你一定也知道，夢裡那個聲音是誰！", "You knew I'd come? Then you must know whose voice is in the dreams!"),
			new(1, "天宮的守護者，燁。", "Yoh, guardian of the Heavenly Palace."),
			new(0, "燁……他為什麼要大家去偷星星、搶季節？", "Yoh... Why make everyone steal stars and snatch seasons?"),
			new(1, "四神結界快碎了，他也快撐不住了。補結界需要力量，而那份力量只認勝者。", "The Barrier of the Four Gods is about to break, and he can barely hold on. Mending it takes power, and that power only answers to a victor."),
			new(0, "勝者？", "A victor?"),
			new(1, "打倒守門人的人。我們五個各守一份，等人來一一贏過。你身上已經有四份了。", "Someone who defeats the gatekeepers. The five of us each hold one share, waiting for someone to beat us one by one. You already carry four."),
			new(0, "所以我一路打過來，全照著他的意思？", "So everything I did on the way here went exactly the way he wanted?"),
			new(1, "是。", "Yes."),
			new(0, "那你們呢？明知道被安排了，還照做？", "And you? You knew you were being used, and you went along with it?"),
			new(1, "我知道，其他人不知道。", "I knew. The others didn't."),
			new(0, "這種事應該好好說清楚！", "Something like this should be said plainly!"),
			new(1, "說清楚了，你就會來嗎？第五份在我這裡。想上天宮問他，就拿走它。", "Would you have come if it had been? The fifth share is with me. If you want to go up and ask him, take it."),
		},
		new L[] {
			new(1, "你的箭，屬金。", "Your arrows belong to Metal."),
			new(0, "我不聽卦。夢裡的聲音是誰。", "I'm not here for fortunes. Whose voice is in the dreams?"),
			new(1, "天宮的燁。守著四神結界的人。", "Yoh of the Heavenly Palace. The one who guards the Barrier of the Four Gods."),
			new(0, "他要什麼。", "What does he want?"),
			new(1, "要一個人，一路贏過五個守門人，把五份力量帶上天宮。結界只認勝者的力量。", "Someone who beats five gatekeepers in a row and carries their five shares up to the palace. The barrier only accepts a victor's power."),
			new(0, "我只是來收陷阱的。", "I only came to collect my traps."),
			new(1, "卦象不管你為什麼來。你已經走到這裡，身上背著四份。", "The divination doesn't care why you came. You're here, carrying four shares."),
			new(0, "所以我是被拿來搬東西的。", "So I'm being used to haul things."),
			new(1, "你可以在這裡轉身。", "You can turn back here."),
			new(0, "轉身，雨會停嗎？星星會回來嗎？", "If I turn back, does the rain stop? Do the stars come back?"),
			new(1, "不會。", "No."),
			new(0, "那就打。", "Then we fight."),
		},
		new L[] {
			new(1, "山伏也懂五行？", "A yamabushi who knows the five elements?"),
			new(0, "一點點～木頭生火烤地瓜，火燒水泡茶！", "A little~ Wood makes fire for roasting sweet potatoes, fire boils water for tea!"),
			new(1, "倒也沒錯。那你知道，你身上已經背了四份力量嗎？", "Not wrong. Then do you know you're already carrying four shares of power?"),
			new(0, "難怪一直很餓！這也是你們搞的喔？", "No wonder I'm always hungry! Is this your doing too?"),
			new(1, "是天宮的燁安排的。四神結界快碎了，他要找一個人一路贏過我們，把力量帶上去補結界。", "Yoh of the Heavenly Palace arranged it. The Barrier of the Four Gods is about to break. He needs someone to beat all of us and carry the power up to mend it."),
			new(0, "他為什麼不自己來拿？", "Why doesn't he come and get it himself?"),
			new(1, "他離不開天宮，也已經沒力氣贏過任何人了。", "He can't leave the palace, and he no longer has the strength to beat anyone."),
			new(0, "好麻煩喔……那他有沒有好好吃飯啊？撐那麼久，一定很累。", "What a hassle... Is he eating properly? Holding on that long must be exhausting."),
			new(1, "這倒沒人想過。先把第五份拿走吧，上去以後你自己問他。", "No one ever thought to ask that. Take the fifth share first. You can ask him yourself up there."),
			new(0, "好，那我收下囉！", "Okay, I'll take it then!"),
		},
		new L[] {
			new(1, "人偶師。你的人偶，屬木，還是屬土？", "Puppeteer. Do your puppets belong to Wood, or to Earth?"),
			new(0, "它們屬於我。請告訴我們，那根線的另一頭是誰。", "They belong to me. Please tell us who's at the other end of the string."),
			new(1, "線？說得好。天宮的守護者，燁。他用夢牽著我們五個，又讓你一路收走我們的力量。", "A string? Well put. Yoh, the guardian of the Heavenly Palace. He holds the five of us with dreams, and has you gather up our power one by one."),
			new(0, "為什麼要這樣做？", "Why would he do that?"),
			new(1, "四神結界快碎了。補它的力量只認勝者，而他已經沒力氣自己去贏。", "The Barrier of the Four Gods is about to break. The power to mend it only answers to a victor, and he no longer has the strength to win on his own."),
			new(0, "孩子們說，那個聲音一直很寂寞。", "The children say that voice has always been lonely."),
			new(1, "人偶看得比人清楚。最後一份在我這裡。你們想替他分擔，就拿去。", "Puppets see more clearly than people. The last share is with me. If you want to help carry his load, take it."),
			new(0, "孩子們，準備好了嗎？", "Children, are you ready?"),
		});

	static Talk Stage5After => new(
		new L[] {
			new(1, "五行已盡。卦象沒錯，你裝得下。", "The five elements are spent. The divination was right. You can hold it."),
			new(0, "身體好燙……這就是五份力量？", "I'm burning up... Is this the five shares?"),
			new(1, "是。往上走，雲會讓路。燁就在天宮等。", "It is. Go up. The clouds will part for you. Yoh is waiting in the palace."),
			new(0, "你們被他這樣用，不生氣嗎？", "Doesn't it make you angry, being used like that?"),
			new(1, "我們都夢到了同一個快要崩掉的世界。能撐住它，當一次棋子也沒什麼。", "We all dreamed of the same world about to collapse. If it holds, being a game piece once is nothing."),
			new(0, "我會氣。我會替你們問他。", "It makes me angry. I'll ask him for all of you."),
			new(1, "去吧。天門之夜就快到了。", "Go. The Night of the Heaven's Gate is almost here."),
		},
		new L[] {
			new(1, "金克木，箭穿五行。卦象沒騙我。", "Metal cuts Wood; an arrow pierced the five elements. The divination didn't lie."),
			new(0, "天宮怎麼走。", "How do I get to the palace?"),
			new(1, "往上。雲會讓路。", "Up. The clouds will part."),
			new(0, "你們甘心被他用？", "You're fine with being used by him?"),
			new(1, "棋盤要是毀了，就沒有棋子可言。我們只是選了守住棋盤的那一步。", "If the board is destroyed, there are no pieces left. We just chose the move that keeps the board."),
			new(0, "我不喜歡被人下。但這一步，我自己走。", "I don't like being played. But this move, I'll make myself."),
		},
		new L[] {
			new(1, "五行皆盡。你現在，比剛才更餓了吧？", "The five elements are spent. You're even hungrier than before, aren't you?"),
			new(0, "你怎麼知道！肚子一直叫！", "How did you know! My stomach won't stop growling!"),
			new(1, "那是五份力量在你體內打轉。上天宮吧。", "That's five shares of power swirling inside you. Go up to the palace."),
			new(0, "燁會請我吃飯嗎？", "Will Yoh feed me?"),
			new(1, "卦象沒說。", "The divination didn't say."),
			new(0, "那我自己跟他講！", "Then I'll ask him myself!"),
		},
		new L[] {
			new(1, "五行相生，最後都流到了你身上。", "The five elements gave rise to one another, and in the end they all flowed into you."),
			new(0, "孩子們說，好重。", "The children say it's heavy."),
			new(1, "那是結界的重量。燁一個人扛了好幾百年。", "That's the weight of the barrier. Yoh has carried it alone for centuries."),
			new(0, "一個人扛……孩子們說，那一定很冷。", "Carrying it alone... The children say that must be cold."),
			new(1, "上天宮吧。你和你的孩子們，也許能替他分一些。", "Go up to the palace. You and your children might take some of it off him."),
			new(0, "我們一起去。", "We'll go together."),
		});

	// =====================================================================================
	// Stage 6 — 天宮. 燁, who arranged everything, tests the one who gathered the power.
	// =====================================================================================
	static Talk Stage6 => new(
		new L[] {
			new(1, "你來了。走到這裡的人，你是第一個。", "You've come. You're the first to make it this far."),
			new(0, "你就是燁吧？看星星的、畫畫的、叫雨的、墨竹，還有玄——大家會鬧事，全是你安排的！", "You're Yoh, right? The stargazer, the painter, the rain guy, Mochiku and Gen. Everything they did, you set it up!"),
			new(1, "是。四神結界撐不過今晚的天門之夜，而我已經沒有力氣補它。", "Yes. The Barrier of the Four Gods won't last through tonight's Night of the Heaven's Gate, and I no longer have the strength to mend it."),
			new(0, "那為什麼不直接說？找人來幫忙不就好了！", "Then why not just say so? Why not ask for help!"),
			new(1, "結界的力量只跟隨勝者。我離不開天宮，只好讓五個人各守一份，等有人一一勝過他們。", "The barrier's power only follows a victor. I can't leave the palace, so I had five people each hold a share and waited for someone to beat them."),
			new(0, "你知道山下變成什麼樣子嗎？星星沒了，稻子凍死，河都快淹出來了！", "Do you know what it's like down there? No stars, frozen rice, a river about to flood!"),
			new(1, "知道。每一件我都看著。", "I know. I watched every bit of it."),
			new(0, "看著，卻什麼都不做？", "You watched and did nothing?"),
			new(1, "我做了。我讓你來到這裡。", "I did something. I brought you here."),
			new(0, "這不是同一件事！", "That's not the same thing!"),
			new(1, "我知道。所以最後，我要用四神之力親自試你。你若撐不住，這些力量也撐不住結界。", "I know. So, at the end, I'll test you myself with the power of the Four Gods. If you can't hold up, neither can the barrier."),
			new(0, "好！打完以後，你給我下山，跟每個人道歉！", "Fine! When this is over, you're coming down the mountain to apologise to every one of them!"),
		},
		new L[] {
			new(1, "獵人。你一路射穿了五道門。", "Hunter. You've shot your way through five gates."),
			new(0, "我只是想收陷阱。", "I only wanted my traps back."),
			new(1, "我知道。對你而言，這全是多出來的事。", "I know. For you, all of this was beside the point."),
			new(0, "為了補結界，讓五個人去鬧，讓我去打。", "To mend the barrier, you set five people loose and sent me to fight them."),
			new(1, "結界只認勝者的力量。我離不開這裡，沒有別的辦法。", "The barrier only accepts a victor's power. I can't leave this place. There was no other way."),
			new(0, "有。你可以說。", "There was. You could have said so."),
			new(1, "說了，誰會信？一個夢裡的聲音說天要塌了。", "And who would believe it? A voice in a dream saying the sky is falling."),
			new(0, "下了七天雨，誰都會信。", "After seven days of rain, anyone would."),
			new(1, "也許你說得對。是我太習慣一個人做決定了。", "Perhaps you're right. I've grown too used to deciding everything alone."),
			new(0, "那就快點結束。", "Then let's finish this."),
			new(1, "好。四神為證，讓我看看你的箭能不能抵達天頂。", "Very well. With the Four Gods as witness, show me whether your arrows can reach the top of the sky."),
		},
		new L[] {
			new(1, "修行者，歡迎來到天宮。", "Welcome to the Heavenly Palace, traveller."),
			new(0, "哇——好漂亮！……不對不對，我是來念你的！", "Wow, it's so pretty! ...No, wait, I'm here to scold you!"),
			new(1, "念我？", "Scold me?"),
			new(0, "你讓大家做奇怪的夢，害星星不見、雨下不停，山下的人都沒辦法好好吃飯！", "You gave everyone weird dreams, the stars vanished, the rain won't stop, and nobody down there can eat properly!"),
			new(1, "是我。四神結界今晚就會碎，我已經補不動了。結界的力量只跟隨勝者，所以我讓五個人各守一份，等人來拿。", "That was me. The Barrier of the Four Gods breaks tonight, and I can't mend it anymore. Its power only follows a victor, so I had five people each keep a share until someone came to take them."),
			new(0, "所以我一路餓肚子，都是在幫你搬東西？", "So I've been starving the whole way just to carry your stuff?"),
			new(1, "可以這麼說。", "You could put it that way."),
			new(0, "那你自己呢？你多久沒吃飯了？", "And you? When did you last eat?"),
			new(1, "守護者不需要吃飯。", "A guardian doesn't need to eat."),
			new(0, "騙人，看你的臉色就知道。", "Liar. One look at your face says otherwise."),
			new(1, "先把試煉做完吧。你背上的五份力量，我得確定你承受得住。", "Let's finish the trial first. I need to be sure you can bear the five shares you carry."),
			new(0, "好喔，那打完你要吃飯！", "Okay, but after this, you're eating!"),
		},
		new L[] {
			new(1, "安靜的孩子。你的人偶陪你走過了每一關。", "A quiet child. Your puppets have walked every step with you."),
			new(0, "是孩子們帶我走過來的。它們說，您的線一直牽到這裡。", "It was the children who brought me. They say your string leads all the way here."),
			new(1, "線……是啊。我用夢牽著五個人，也牽著你。", "A string... Yes. I held five people with dreams, and you as well."),
			new(0, "為什麼不放開呢？", "Why not let go?"),
			new(1, "四神結界快碎了。補它的力量只跟隨勝者，我已經沒力氣自己去贏，只好讓守門人各執一份，等一個人來。", "The Barrier of the Four Gods is about to break. The power to mend it only follows a victor, and I can no longer win on my own. So I had the gatekeepers each hold a share and waited for someone to come."),
			new(0, "大家都不是壞人，卻被當成壞人打倒了。", "None of them were bad people, but they were beaten like villains."),
			new(1, "是我讓他們背了這個名。等結界補好，我會親自去向他們賠罪。", "I'm the one who put that name on them. Once the barrier is mended, I'll go and make amends to each of them myself."),
			new(0, "孩子們說，您也該有人陪。", "The children say you should have someone with you too."),
			new(1, "那就讓我最後一次，用四神之力試你們。", "Then let me test you one last time, with the power of the Four Gods."),
		});

	static Talk Stage6After => new(
		new L[] {
			new(1, "勝負已分。四神之力，現在都在你身上了。", "It's decided. The power of the Four Gods is all yours now."),
			new(0, "那結界呢？補得起來嗎？", "And the barrier? Can it be mended?"),
			new(1, "已經在補了。天門會在今晚安靜地開，再安靜地關上。", "It's already mending. The gate will open quietly tonight and close just as quietly."),
			new(0, "那你呢？力量都給我了，你會不見嗎？", "What about you? You gave me all your power. Will you disappear?"),
			new(1, "不會，只是要睡一陣子。還有一件事——結界最舊的那道裂痕，不是今晚才有的。", "No. I'll just sleep for a while. One more thing: the oldest crack in the barrier wasn't made tonight."),
			new(0, "什麼意思？", "What do you mean?"),
			new(1, "那是我五百年前犯的錯。改天再說吧。", "It's a mistake I made five hundred years ago. Another time."),
			new(0, "下次有事，直接說！別再讓大家打架了！", "Next time, just tell us! Don't make everyone fight again!"),
			new(1, "我答應你。", "I promise."),
		},
		new L[] {
			new(1, "好箭。結界穩住了。", "Fine shooting. The barrier is holding."),
			new(0, "你利用了五個人。", "You used five people."),
			new(1, "我會記著。", "I'll remember that."),
			new(0, "不用記。玄說，他們是自己選的。", "No need. Gen said they chose it themselves."),
			new(1, "你比看起來溫柔。", "You're kinder than you look."),
			new(0, "少囉嗦。陷阱還在山下，我要走了。", "Spare me. My traps are still down there. I'm leaving."),
			new(1, "結界最舊的那道裂，我還補不了。若你哪天又路過天宮——", "There's an old crack in the barrier I still can't mend. If you ever pass by the palace again—"),
			new(0, "看情況。", "We'll see."),
		},
		new L[] {
			new(1, "結束了。天門之夜會平安過去。", "It's over. The Night of the Heaven's Gate will pass safely."),
			new(0, "太好了！那可以吃飯了嗎？", "Great! So can we eat now?"),
			new(1, "你一路打上來，就為了這個？", "You fought all the way up here for that?"),
			new(0, "還有大家！他們都是因為你才那麼累的，你要好好跟他們說謝謝。", "And for everyone! They got worn out because of you. You have to thank them properly."),
			new(1, "是啊。我欠他們一句話。", "Yes. I owe them that."),
			new(0, "那就一起吃飯的時候說！", "Then say it over dinner, all together!"),
			new(1, "好。等我醒來。", "All right. Once I wake up."),
		},
		new L[] {
			new(1, "人偶師，謝謝你。結界穩住了。", "Thank you, puppeteer. The barrier is holding."),
			new(0, "孩子們說，您的聲音不那麼寂寞了。", "The children say your voice doesn't sound so lonely anymore."),
			new(1, "五百年來，我一直以為只能靠自己。", "For five hundred years, I thought I could only rely on myself."),
			new(0, "那以後，讓孩子們也來看您。", "Then from now on, let the children visit you too."),
			new(1, "好。人偶師，你的孩子們有沒有注意到，更遠的地方還有一點光？", "I'd like that. Puppeteer, have your children noticed a faint light further off?"),
			new(0, "有。它們一直在看。", "They have. They keep looking at it."),
			new(1, "那是我還沒還清的債。等我醒來，再告訴你。", "That's a debt I haven't repaid. I'll tell you about it when I wake."),
		});
}
