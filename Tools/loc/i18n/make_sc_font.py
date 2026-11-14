"""Godot/Fonts/NotoSerifSC-SemiBold-subset.ttf: the Simplified Chinese glyphs the game's Noto Serif TC lacks.

Source: Noto Serif SC (SIL Open Font License 1.1), the variable font Windows 11 installs as
C:/Windows/Fonts/NotoSerifSC-VF.ttf, fixed at weight 600 to match NotoSerifTC-SemiBold. The subset holds the
3,755 common GB2312 characters plus every character of the current Simplified Chinese text, minus what the TC
font already has, so later dialogue edits rarely need a new subset. Hud.cs loads it as the UI font's fallback.
"""
import io, json, os, sys
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont
from fontTools import subset
from opencc import OpenCC

HERE = os.path.dirname(os.path.abspath(__file__))
FONTS = os.path.join(HERE, "..", "..", "..", "Godot", "Fonts")
SRC = os.environ.get("NOTO_SERIF_SC", "C:/Windows/Fonts/NotoSerifSC-VF.ttf")

cc = OpenCC("tw2s")
text = "".join(cc.convert(x["zh"]) for x in json.load(io.open(os.path.join(HERE, "source.json"), encoding="utf-8")))
gb = []
for hi in range(0xB0, 0xD8):               # GB2312 level 1 (the 3,755 most common hanzi)
    for lo in range(0xA1, 0xFF):
        try: gb.append(bytes([hi, lo]).decode("gb2312"))
        except UnicodeDecodeError: pass
tc = TTFont(os.path.join(FONTS, "NotoSerifTC-SemiBold.ttf")).getBestCmap()
want = sorted({c for c in text + "".join(gb) + "简体中文语言" if ord(c) > 0x2E7F and ord(c) not in tc})

font = instantiateVariableFont(TTFont(SRC), {"wght": 600})
opts = subset.Options(); opts.name_IDs = ["*"]; opts.name_languages = ["*"]; opts.notdef_outline = True
sub = subset.Subsetter(opts); sub.populate(unicodes=[ord(c) for c in want]); sub.subset(font)
out = os.path.join(FONTS, "NotoSerifSC-SemiBold-subset.ttf")
font.save(out)
sys.stdout.reconfigure(encoding="utf-8")
print(len(want), "glyphs,", os.path.getsize(out) // 1024, "KB ->", out)
