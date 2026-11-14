"""Build Tools/loc/i18n/source.json: every string the game shows (from Saved/AllText.tsv, written by
-DanmakuDumpText), minus the old Unreal-era dialogue in Bosses.cs that Story*.cs replaces."""
import io, json, os, re, sys

GODOT = os.path.join(os.path.dirname(__file__), "..", "..", "..", "Godot")
B = io.open(os.path.join(GODOT, "Scripts", "Bosses.cs"), encoding="utf-8-sig").read()
old = set(re.findall(r'new\(\d, "((?:[^"\\]|\\.)*)"\)', B))
live = set(l.split("\t")[3] for l in io.open(os.path.join(GODOT, "Saved", "Dialogue.tsv"), encoding="utf-8") if l.strip())
dead = old - live
rows = [l.rstrip("\n").split("\t") for l in io.open(os.path.join(GODOT, "Saved", "AllText.tsv"), encoding="utf-8") if l.strip()]
keep = [r for r in rows if r[0] not in dead]
items = [{"id": i, "zh": r[0], "en": r[1] if len(r) > 1 else ""} for i, r in enumerate(keep)]
json.dump(items, io.open(os.path.join(os.path.dirname(__file__), "source.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=0)
sys.stdout.reconfigure(encoding="utf-8")
print("old dialogue", len(old), "dead", len(dead), "all", len(rows), "keep", len(keep), "english chars", sum(len(x["en"]) for x in items))
