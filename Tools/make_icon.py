from PIL import Image, ImageDraw
OUT="Assets/Art/Icon/"
import os; os.makedirs(OUT,exist_ok=True)
K=(20,0,4)
def bgcol(y,n):
    g=[(24,6,14),(34,8,16),(50,10,16),(80,14,14),(120,26,10),(180,60,12)]
    t=y/(n-1); return g[min(len(g)-1,int(t**1.8*len(g)))]
def base(n,r):
    im=Image.new("RGBA",(n,n),(0,0,0,0)); p=im.load()
    for y in range(n):
        for x in range(n):
            # rounded square mask
            cx=min(x,n-1-x); cy=min(y,n-1-y)
            if cx<r and cy<r and (r-cx-0.5)**2+(r-cy-0.5)**2>r*r: continue
            p[x,y]=bgcol(y,n)+(255,)
    return im
# ---- 32x32 ----
im=base(32,4); p=im.load(); d=ImageDraw.Draw(im); BG=im.copy().load()
DS=(120,22,26,255); DM=(160,32,32,255); DL=(210,70,44,255); DK=(60,8,14,255)
HN=(96,68,52,255); HT=(236,214,172,255)
# horns
import math
for s in (-1,1):
    for i in range(12):
        t=i/11; x=16+s*(6+t*9-t*t*3); y=13-t*11+t*t*2; r=2.0*(1-t)+0.3
        d.ellipse([x-r,y-r,x+r,y+r],fill=HN if i<10 else HT)
# head
d.ellipse([7,7,24,27],fill=DS)
d.polygon([(9,21),(16,30),(22,21)],fill=DS)
d.ellipse([8,8,19,20],fill=DM)
d.rectangle([11,10,13,11],fill=DL)
# brows + eyes
d.line([(9,14),(14,16)],fill=DK); d.line([(22,14),(17,16)],fill=DK)
for ex in (10,18):
    d.rectangle([ex,17,ex+3,18],fill=(255,226,70,255)); p[ex+1,17]=(255,255,220,255)
# nose
p[15,20]=DK; p[16,20]=DK
# grin
d.polygon([(10,22),(21,22),(19,25),(12,25)],fill=(30,0,6,255))
for tx in range(11,21,2): p[tx,22]=(240,232,205,255)
for tx in (13,16,18): p[tx,24]=(240,232,205,255)
# outline around head/horns
q=im.copy(); qp=q.load()
for y in range(32):
    for x in range(32):
        c=qp[x,y]
        if c!=BG[x,y] or c[3]==0: continue
        for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
            xx,yy=x+dx,y+dy
            if 0<=xx<32 and 0<=yy<32 and qp[xx,yy]!=BG[xx,yy]:
                p[x,y]=K+(255,); break
# lava sparks bottom corners
for (x,y) in [(3,28),(5,26),(27,27),(25,29),(28,24),(4,22)]:
    p[x,y]=(255,170,50,255)
big32=im
# ---- 16x16 hand-drawn ----
G16=["................",
     ".h............h.",
     ".Hh..........hH.",
     ".HH..........HH.",
     "..HHKKKKKKKKHH..",
     "...KRLLRRRRrK...",
     "..KRLRRRRRRRrK..",
     "..KRKKRRRRKKrK..",
     "..KRYYRRRRYYrK..",
     "..KRRRRKKRRRrK..",
     "..KRMWMWMWMWrK..",
     "...KRMMMMMMrK...",
     "....KRRRRrrK....",
     ".....KRrrrK.....",
     "......KKKK......",
     "................"]
cmap={'h':HT,'H':HN,'K':K+(255,),'R':DS,'L':DL,'r':DK,'Y':(255,226,70,255),'M':(30,0,6,255),'W':(240,232,205,255)}
s16=base(16,2); sp=s16.load()
for y,row in enumerate(G16):
    assert len(row)==16,(y,len(row))
    for x,c in enumerate(row):
        if c in cmap: sp[x,y]=cmap[c]
for (x,y) in [(2,13),(13,12),(1,11)]: sp[x,y]=(255,170,50,255)
up=lambda im,n: im.resize((n,n),Image.NEAREST)
sizes={1024:up(big32,1024),512:up(big32,512),256:up(big32,256),128:up(big32,128),64:up(big32,64),32:big32,48:up(s16,48),16:s16}
for n,i in sizes.items(): i.save(OUT+f"hellpoker-icon-{n}.png")
# ico with all sizes
sizes[256].save(OUT+"hellpoker.ico",format="ICO",sizes=[(16,16),(32,32),(48,48),(64,64),(128,128),(256,256)],append_images=[sizes[n] for n in (16,32,48,64,128)])
