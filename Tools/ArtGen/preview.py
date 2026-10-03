"""Preview page of every demon animation frame (and the UI art), scaled up with nearest-neighbour.

Usage (from the project root):  py Tools/ArtGen/preview.py
Output: Tools/ArtGen/preview/index.html and contact sheets — local only, not in the repo (.gitignore).
"""
import json
import os
import sys

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import pixel_demons  # noqa: E402
import pixel_menu  # noqa: E402
import pixel_salons  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
ART = os.path.join(ROOT, "Assets", "Resources", "Art")
OUT = os.path.join(HERE, "preview")
SCALE = 3
FPS = {"idle": 5, "talk": 8, "gloat": 8, "angry": 8, "reraise": 8, "final": 8, "soul": 8}


def scaled(img, scale=SCALE):
    return img.resize((img.width * scale, img.height * scale), Image.NEAREST)


def demon_sheet(demon):
    """All states of one demon, one row per state, frames left to right."""
    rows = []
    for state in pixel_demons.STATES:
        path = os.path.join(ART, "Demons", demon, state + ".png")
        if os.path.exists(path):
            rows.append((state, Image.open(path).convert("RGBA")))
    width = max(img.width for _, img in rows) + 70
    height = sum(img.height + 4 for _, img in rows)
    sheet = Image.new("RGBA", (width, height), (11, 6, 16, 255))
    draw = ImageDraw.Draw(sheet)
    y = 0
    for state, img in rows:
        draw.text((2, y + 2), state, fill=(242, 232, 208, 255))
        sheet.alpha_composite(img, (70, y))
        y += img.height + 4
    return sheet


BACKDROP_SCALE = 2

# Plays the backdrops: frames swapped per layer at its FPS (with its offset), drifting layers moved in whole pixels and
# wrapped, particles stepping whole pixels inside their areas (rising embers and motes, wandering wisps).
PLAYER_JS = r"""
const S = 2, colors = {embers: ['#ffe08a','#ffb02e','#ff7a1c','#e0401c','#b8261c'], motes: ['#ffd860','#e0a828','#ffd860'],
  wisps: ['#c8a8d4','#9a7aa8','#4a2856']};
BACKDROPS.forEach((b, i) => {
  const root = document.getElementById('bd' + i);
  b.layers.forEach(l => { l.els = []; for (let r = 0; r < l.repeat; r++) { const d = document.createElement('div');
    d.style.width = l.w * S + 'px'; d.style.height = l.h * S + 'px'; d.style.backgroundImage = 'url(' + l.src + ')';
    d.style.top = l.y * S + 'px'; root.appendChild(d); l.els.push(d); } });
  b.zones.forEach(z => { const d = document.createElement('div'); d.className = 'zone';
    Object.assign(d.style, {left: z[0] * S + 'px', top: z[1] * S + 'px', width: (z[2] - z[0] + 1) * S + 'px', height: (z[3] - z[1] + 1) * S + 'px'});
    root.appendChild(d); });
  const c = document.createElement('canvas'); c.width = 480; c.height = 270; c.style.width = '960px'; c.style.height = '540px';
  root.appendChild(c); b.ctx = c.getContext('2d'); b.parts = [];
  b.particles.forEach(p => { for (let k = 0; k < p.count; k++) b.parts.push(spawn(p, true)); });
});
function spawn(p, anywhere) {
  const q = {p, x: p.x + Math.random() * p.w, y: anywhere ? p.y + Math.random() * p.h : p.y + p.h - 1, age: 0};
  if (p.kind === 'embers') { q.vx = (Math.random() - .5) * 4; q.vy = -12 - Math.random() * 14; q.life = p.h / -q.vy; }
  else if (p.kind === 'motes') { q.vx = (Math.random() - .5) * 2; q.vy = -3 - Math.random() * 4; q.life = 4 + Math.random() * 6; }
  else { q.vx = (Math.random() - .5) * 6; q.vy = -1 - Math.random() * 2; q.life = 5 + Math.random() * 5; }
  if (anywhere) q.age = Math.random() * q.life;
  return q;
}
let last = performance.now(), t = 0;
function tick(now) {
  const dt = (now - last) / 1000; last = now; t += dt;
  BACKDROPS.forEach(b => {
    b.layers.forEach(l => { const f = (Math.floor(t * l.fps) + l.offset) % l.frames;
      let x = l.x; if (l.drift && l.wrap) { x = ((l.x + Math.floor(t * l.drift) + l.w) % l.wrap + l.wrap) % l.wrap - l.w; }
      l.els.forEach((d, r) => { d.style.backgroundPosition = (-f * l.w * S) + 'px 0'; d.style.left = (x + r * l.w) * S + 'px'; }); });
    b.ctx.clearRect(0, 0, 480, 270);
    b.parts.forEach((q, k) => { q.age += dt; q.x += q.vx * dt; q.y += q.vy * dt; const p = q.p;
      if (q.age >= q.life || q.y < p.y || q.x < p.x - 2 || q.x > p.x + p.w + 2) b.parts[k] = q = spawn(p, false);
      const ramp = colors[p.kind]; b.ctx.fillStyle = ramp[Math.min(ramp.length - 1, Math.floor(q.age / q.life * ramp.length))];
      b.ctx.fillRect(Math.floor(q.x), Math.floor(q.y), 1, 1); });
  });
  requestAnimationFrame(tick);
}
requestAnimationFrame(tick);
"""


def backdrop(folder, base_file, manifest_file, variant, title, zones):
    """One layered backdrop for the page: the still picture and its layers scaled up, the manifest read; None if missing."""
    base = os.path.join(folder, base_file)
    manifest = os.path.join(folder, manifest_file)
    if not os.path.exists(base):
        return None
    key = "bd_" + title.replace(" ", "").replace("—", "_")
    scaled(Image.open(base).convert("RGBA"), BACKDROP_SCALE).save(os.path.join(OUT, key + ".png"))
    layers, particles = [], []
    if os.path.exists(manifest):
        for line in open(manifest, encoding="utf-8"):
            parts = line.split()
            if not parts or parts[0].startswith("#"):
                continue
            if parts[0] == "layer":
                name = parts[1]
                src = os.path.join(folder, name + ("" if variant == "normal" else "_" + variant) + ".png")
                if not os.path.exists(src):
                    src = os.path.join(folder, name + ".png")
                strip = Image.open(src).convert("RGBA")
                frames = int(parts[4])
                out = f"{key}_{name}.png"
                scaled(strip, BACKDROP_SCALE).save(os.path.join(OUT, out))
                layers.append({"src": out, "x": int(parts[2]), "y": int(parts[3]), "frames": frames, "fps": float(parts[5]),
                               "repeat": int(parts[6]), "drift": float(parts[7]), "wrap": int(parts[8]), "offset": int(parts[9]),
                               "w": strip.width // frames, "h": strip.height})
            elif parts[0] == "particles":
                particles.append({"kind": parts[1], "x": int(parts[2]), "y": int(parts[3]), "w": int(parts[4]), "h": int(parts[5]),
                                  "count": int(parts[6])})
    return {"base": key + ".png", "title": title, "layers": layers, "particles": particles, "zones": [list(z) for z in zones]}


def ui_sheet():
    folder = os.path.join(ART, "Ui")
    # The full-screen backdrops (the menu and its layers, the fade) get their own sections.
    images = [(name, Image.open(os.path.join(folder, name)).convert("RGBA")) for name in sorted(os.listdir(folder))
              if name.endswith(".png") and not name.startswith("menu") and name != "fade.png"]
    width = 500
    x = y = row_h = 0
    placed = []
    for name, img in images:
        if x + img.width > width:
            x, y, row_h = 0, y + row_h + 12, 0
        placed.append((name, img, x, y))
        x += img.width + 8
        row_h = max(row_h, img.height)
    sheet = Image.new("RGBA", (width, y + row_h + 12), (40, 40, 48, 255))
    draw = ImageDraw.Draw(sheet)
    for name, img, px, py in placed:
        sheet.alpha_composite(img, (px, py + 10))
        draw.text((px, py), name[:-4][:14], fill=(200, 200, 200, 255))
    return sheet


def main():
    os.makedirs(OUT, exist_ok=True)
    html = ["<!doctype html><meta charset=utf-8><title>Hell Poker art preview</title>",
            "<style>body{background:#0b0610;color:#f2e8d0;font:14px monospace}img{image-rendering:pixelated;margin:4px}"
            ".strip{display:inline-block;width:%dpx;height:%dpx;background-repeat:no-repeat;image-rendering:pixelated;margin:4px}</style>"
            % (96 * SCALE, 96 * SCALE)]
    css = []
    for demon in pixel_demons.DEMONS:
        sheet = demon_sheet(demon)
        name = demon + "_sheet.png"
        scaled(sheet).save(os.path.join(OUT, name))
        html.append(f"<h2>{demon}</h2><div>")
        for state in pixel_demons.STATES:
            src = os.path.join(ART, "Demons", demon, state + ".png")
            if not os.path.exists(src):
                continue
            strip = Image.open(src)
            frames = strip.width // strip.height
            big = scaled(strip)
            file = f"{demon}_{state}.png"
            big.save(os.path.join(OUT, file))
            cls = f"{demon}-{state}"
            css.append(f".{cls}{{background-image:url({file});animation:{cls} {frames / FPS[state]:.3f}s steps({frames}) infinite}}"
                       f"@keyframes {cls}{{to{{background-position:-{big.width}px 0}}}}")
            html.append(f"<figure style=display:inline-block><div class='strip {cls}'></div><figcaption>{state} ({frames})</figcaption></figure>")
        html.append(f"</div><img src={name}>")
    # The layered backdrops, played at their real speed: the still picture, each layer at its own rate (offsets and drift
    # as in the game), and the particles (approximated by the page's script). Dashed boxes: where words sit.
    backdrops = []
    salons = os.path.join(ART, "Backgrounds")
    if os.path.isdir(salons):
        for demon in sorted(os.listdir(salons)):
            for variant in ("normal", "hell", "soul"):
                backdrops.append(backdrop(os.path.join(salons, demon), variant + ".png", "motion.txt", variant,
                                          f"{demon} — {variant}", pixel_salons.TABLE_TEXT))
    backdrops.append(backdrop(os.path.join(ART, "Ui"), "menu.png", "menu_motion.txt", "normal", "title screen", pixel_menu.MENU_TEXT))
    backdrops = [b for b in backdrops if b]
    if backdrops:
        html.append("<h2>backdrops (live: layers at their own FPS, particles, dashed = words)</h2>")
        for i, b in enumerate(backdrops):
            html.append(f"<figure><div class=backdrop id=bd{i} style='background-image:url({b['base']})'></div>"
                        f"<figcaption>{b['title']} — {len(b['layers'])} layers, {sum(p['count'] for p in b['particles'])} particles"
                        f"</figcaption></figure>")
        css.append(".backdrop{position:relative;width:960px;height:540px;background-repeat:no-repeat;image-rendering:pixelated;"
                   "overflow:hidden}.backdrop>div{position:absolute;background-repeat:no-repeat;image-rendering:pixelated}"
                   ".backdrop>.zone{outline:1px dashed #ffd860}.backdrop>canvas{position:absolute;left:0;top:0;image-rendering:pixelated}")
        html.append("<script>const BACKDROPS=" + json.dumps(backdrops) + ";\n" + PLAYER_JS + "</script>")
    if os.path.isdir(os.path.join(ART, "Ui")):
        scaled(ui_sheet(), 2).save(os.path.join(OUT, "ui_sheet.png"))
        html.append("<h2>ui</h2><img src=ui_sheet.png>")
    html.insert(1, "<style>" + "".join(css) + "</style>")
    with open(os.path.join(OUT, "index.html"), "w", encoding="utf-8") as f:
        f.write("\n".join(html))
    print("wrote", os.path.relpath(os.path.join(OUT, "index.html"), ROOT))


if __name__ == "__main__":
    main()
