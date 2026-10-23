# 把白底 PNG 的白色背景去掉（從四邊往內淹，不會動到圖中間的白色，例如眼睛反光、肚子）。
# 純 Python，不需要 Pillow。用法：python tools/knockout-white.py 圖1.png 圖2.png ...（原檔覆蓋）
import struct, sys, zlib
from collections import deque

def read_png(path):
    data = open(path, "rb").read()
    assert data[:8] == b"\x89PNG\r\n\x1a\n", "not a PNG"
    pos, idat, info = 8, [], None
    while pos < len(data):
        length, ctype = struct.unpack(">I4s", data[pos:pos+8]); body = data[pos+8:pos+8+length]; pos += 12 + length
        if ctype == b"IHDR": info = struct.unpack(">IIBBBBB", body)
        elif ctype == b"IDAT": idat.append(body)
        elif ctype == b"IEND": break
    w, h, depth, color, _, _, interlace = info
    assert depth == 8 and interlace == 0 and color in (2, 6), f"unsupported PNG (depth {depth}, color {color})"
    ch = 4 if color == 6 else 3
    raw = zlib.decompress(b"".join(idat)); stride = w * ch
    out = bytearray(h * stride); prev = bytearray(stride); p = 0
    for y in range(h):
        f = raw[p]; line = bytearray(raw[p+1:p+1+stride]); p += 1 + stride
        if f == 1:
            for i in range(ch, stride): line[i] = (line[i] + line[i-ch]) & 255
        elif f == 2:
            for i in range(stride): line[i] = (line[i] + prev[i]) & 255
        elif f == 3:
            for i in range(stride): line[i] = (line[i] + ((line[i-ch] if i >= ch else 0) + prev[i]) // 2) & 255
        elif f == 4:
            for i in range(stride):
                a = line[i-ch] if i >= ch else 0; b = prev[i]; c = prev[i-ch] if i >= ch else 0
                pa, pb, pc = abs(b - c), abs(a - c), abs(a + b - 2*c)
                line[i] = (line[i] + (a if pa <= pb and pa <= pc else b if pb <= pc else c)) & 255
        out[y*stride:(y+1)*stride] = line; prev = line
    if ch == 3:  # 補上 alpha
        rgba = bytearray(w*h*4)
        for i in range(w*h): rgba[i*4:i*4+3] = out[i*3:i*3+3]; rgba[i*4+3] = 255
        out = rgba
    return w, h, out

def write_png(path, w, h, rgba):
    stride = w * 4
    raw = b"".join(b"\x00" + bytes(rgba[y*stride:(y+1)*stride]) for y in range(h))
    def chunk(t, b): return struct.pack(">I", len(b)) + t + b + struct.pack(">I", zlib.crc32(t + b) & 0xffffffff)
    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)) + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")
    open(path, "wb").write(png)

def knockout(path, white=238, soft=170):
    w, h, px = read_png(path)
    def mn(i): return min(px[i*4], px[i*4+1], px[i*4+2])
    gone = bytearray(w*h); q = deque()
    for x in range(w): q.append(x); q.append((h-1)*w + x)
    for y in range(h): q.append(y*w); q.append(y*w + w - 1)
    while q:
        i = q.popleft()
        if gone[i] or mn(i) < white: continue
        gone[i] = 1
        x, y = i % w, i // w
        if x > 0: q.append(i-1)
        if x < w-1: q.append(i+1)
        if y > 0: q.append(i-w)
        if y < h-1: q.append(i+w)
    removed = 0
    for i in range(w*h):
        if gone[i]:
            px[i*4+3] = 0; removed += 1
        else:
            # 貼著背景的邊緣像素：越接近白色越透明，描邊才不會有白邊
            x, y = i % w, i // w
            near = (x > 0 and gone[i-1]) or (x < w-1 and gone[i+1]) or (y > 0 and gone[i-w]) or (y < h-1 and gone[i+w])
            if near:
                m = mn(i)
                if m > soft: px[i*4+3] = max(0, min(255, int(255 * (white - m) / (white - soft))))
    write_png(path, w, h, px)
    return removed, w*h

if __name__ == "__main__":
    for p in sys.argv[1:]:
        removed, total = knockout(p)
        print(f"{p}: 去掉 {removed*100//total}% 的像素當背景")
