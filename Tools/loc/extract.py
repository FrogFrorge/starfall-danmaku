"""Lists every Chinese TEXT("...") literal in the game source (unique, in order of appearance).

    python Tools/loc/extract.py            -> writes Tools/loc/zh.txt (one string per line)
    python Tools/loc/extract.py --check    -> reports strings that have no line in Tools/loc/en.txt yet

Translations live in Tools/loc/en.txt as "zh<TAB>en" lines; make_table.py turns them into DanmakuText_Table.inl.
"""
import glob, os, re, sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Source", "DanmakuSTG")
HERE = os.path.dirname(os.path.abspath(__file__))
LIT = re.compile(r'TEXT\("((?:[^"\\]|\\.)*)"\)')
CJK = re.compile("[　-鿿＀-￯]")


def strings():
    seen, out = set(), []
    files = sorted(glob.glob(os.path.join(ROOT, "*.cpp")) + glob.glob(os.path.join(ROOT, "*.h")))
    for f in files:
        if os.path.basename(f).startswith("DanmakuText"):
            continue
        src = open(f, encoding="utf-8").read()
        src = re.sub(r"//[^\n]*", "", src)
        for s in LIT.findall(src):
            if CJK.search(s) and s not in seen:
                seen.add(s)
                out.append(s)
    return out


def load_en():
    p = os.path.join(HERE, "en.txt")
    d = {}
    if os.path.exists(p):
        for line in open(p, encoding="utf-8"):
            line = line.rstrip("\n")
            if "\t" in line:
                zh, en = line.split("\t", 1)
                d[zh] = en
    return d


if __name__ == "__main__":
    s = strings()
    if "--check" in sys.argv:
        en = load_en()
        missing = [x for x in s if x not in en]
        open(os.path.join(HERE, "missing.txt"), "w", encoding="utf-8").write("\n".join(missing) + "\n")
        print(len(s), "strings,", len(missing), "missing (Tools/loc/missing.txt)")
    else:
        open(os.path.join(HERE, "zh.txt"), "w", encoding="utf-8").write("\n".join(s) + "\n")
        print(len(s), "strings")
