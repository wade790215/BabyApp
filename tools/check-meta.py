# 找出 Unity 資產與 .meta 在版控裡不同步的地方。
# .meta 裡的 GUID 是 Unity 唯一的資產參照：圖 commit 了但 .meta 沒 commit，別台機器 clone 下來
# Unity 會重新產生新 GUID，所有指向它的參照（Deck 資產、場景）就全斷。2026-11-05 就踩過一次。
# 純 Python，只靠 git 指令。用法：python tools/check-meta.py（發現 ERROR 回傳 1，可掛 pre-commit）
import os, subprocess, sys

ROOT = "Assets"

# Windows 的 console 預設 cp950，印繁中會變亂碼
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")


def unity_ignores(name):
    # Unity 匯入時會跳過這些，所以它們本來就不該有 .meta（.gitkeep、備份檔、暫存檔）
    return name.startswith(".") or name.endswith("~") or name.endswith(".tmp") or name.lower() == "cvs"


def git(*args):
    out = subprocess.run(("git",) + args, capture_output=True, check=True).stdout
    return [p for p in out.decode("utf-8").split("\0") if p]


def scan():
    # git 一律用正斜線的相對路徑，os.walk 在 Windows 會給反斜線，統一掉才能比對
    files, dirs = set(), set()
    for cur, subdirs, names in os.walk(ROOT):
        subdirs[:] = [d for d in subdirs if not unity_ignores(d)]
        rel = cur.replace(os.sep, "/")
        if rel != ROOT:
            dirs.add(rel)
        for n in names:
            if not unity_ignores(n) or n.endswith(".meta"):
                files.add(rel + "/" + n)
    return files, dirs


def main():
    os.chdir(subprocess.run(("git", "rev-parse", "--show-toplevel"),
                            capture_output=True, check=True).stdout.decode("utf-8").strip())
    if not os.path.isdir(ROOT):
        sys.exit("找不到 %s/，請在專案根目錄跑" % ROOT)

    tracked = set(git("ls-files", "-z", ROOT))
    ignored = set(git("ls-files", "--others", "--ignored", "--exclude-standard", "-z", ROOT))
    on_disk, dirs = scan()

    # 資料夾本身也要有 .meta，但只有「裡面有被追蹤的檔案」的資料夾才算進專案
    tracked_dirs = set()
    for p in tracked:
        while "/" in p:
            p = p.rsplit("/", 1)[0]
            if p == ROOT:
                break
            tracked_dirs.add(p)

    untracked_meta, missing_meta, pending, orphan = [], [], [], []

    for asset in sorted([f for f in on_disk if not f.endswith(".meta")] + list(dirs)):
        if asset in ignored:
            continue
        meta = asset + ".meta"
        asset_tracked = asset in tracked or asset in tracked_dirs
        if meta in tracked:
            continue
        if asset_tracked:
            # 這是危險的那一種：資產進了版控，GUID 卻沒有
            (untracked_meta if meta in on_disk else missing_meta).append(asset)
        elif meta not in ignored:
            pending.append(asset)

    for meta in sorted(m for m in tracked if m.endswith(".meta")):
        asset = meta[:-5]
        if asset not in on_disk and asset not in dirs:
            orphan.append(meta)

    def report(title, items, detail):
        if not items:
            return
        print("\n%s（%d）" % (title, len(items)))
        print("  " + detail)
        for p in items[:40]:
            print("    " + p)
        if len(items) > 40:
            print("    ...另外 %d 個" % (len(items) - 40))

    report("ERROR 資產已追蹤但 .meta 沒追蹤", untracked_meta,
           ".meta 檔在磁碟上但不在 git 裡 → 換機器 GUID 會重生，參照全斷。下面列的是資產路徑，要 add 的是 <路徑>.meta")
    report("ERROR 資產已追蹤但完全沒有 .meta", missing_meta,
           "磁碟上也沒有 .meta。開一次 Unity 讓它產生，然後連同資產一起 commit")
    report("WARN 資產與 .meta 都還沒 commit", pending,
           "正常的工作中狀態；要 commit 的話記得兩個一起進去")
    report("WARN .meta 追蹤著但資產不見了", orphan,
           "殘留的 .meta，資產被刪掉時忘了一起刪")

    errors = len(untracked_meta) + len(missing_meta)
    print("\n" + "-" * 60)
    if errors:
        print("有 %d 個資產的 GUID 沒進版控。要全部補上就跑：" % errors)
        print("  python tools/check-meta.py --fix-cmd | sh")
        return 1
    print("GUID 都進版控了（檢查 %d 個資產）。" % (len(on_disk) + len(dirs)))
    if pending or orphan:
        print("另有 %d 個待 commit、%d 個殘留 .meta，見上面的 WARN。" % (len(pending), len(orphan)))
    return 0


def fix_cmd():
    """印出補齊用的 git add 指令，不直接執行——要加什麼進版控由人決定。"""
    os.chdir(subprocess.run(("git", "rev-parse", "--show-toplevel"),
                            capture_output=True, check=True).stdout.decode("utf-8").strip())
    tracked = set(git("ls-files", "-z", ROOT))
    ignored = set(git("ls-files", "--others", "--ignored", "--exclude-standard", "-z", ROOT))
    on_disk, dirs = scan()
    tracked_dirs = set()
    for p in tracked:
        while "/" in p:
            p = p.rsplit("/", 1)[0]
            if p == ROOT:
                break
            tracked_dirs.add(p)
    todo = []
    for asset in sorted([f for f in on_disk if not f.endswith(".meta")] + list(dirs)):
        meta = asset + ".meta"
        if asset in ignored or meta in tracked or meta not in on_disk:
            continue
        if asset in tracked or asset in tracked_dirs:
            todo.append(meta)
    for m in todo:
        print("git add '%s'" % m)
    return 0


if __name__ == "__main__":
    sys.exit(fix_cmd() if "--fix-cmd" in sys.argv else main())
