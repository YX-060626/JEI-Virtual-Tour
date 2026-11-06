#!/usr/bin/env python3
"""Generate measured SVG and an offline interactive HTML from the shared Unity plan JSON."""
from pathlib import Path
import json, math, html, hashlib
ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'Assets/JEI_Blockout/Floor01_Plan.json'
OUT=Path(__file__).resolve().parent
p=json.loads(SOURCE.read_text())
W,H=1680,1120
S=30
X=lambda x:170+(x+18)*S
Y=lambda z:180+(13-z)*S
F=lambda x:f'{x:.2f}'
svg=[]
def a(s):svg.append(s)
def text(x,y,s,size=15,fill='#304445',weight=400,anchor='middle',extra=''):
 a(f'<text x="{F(x)}" y="{F(y)}" font-size="{size}" fill="{fill}" font-weight="{weight}" text-anchor="{anchor}" {extra}>{html.escape(str(s))}</text>')
def line(x1,y1,x2,y2,stroke='#39484b',width=1,extra=''):
 a(f'<line x1="{F(x1)}" y1="{F(y1)}" x2="{F(x2)}" y2="{F(y2)}" stroke="{stroke}" stroke-width="{width}" {extra}/>')
def rect(x,y,w,h,fill,extra=''):
 a(f'<rect x="{F(x)}" y="{F(y)}" width="{F(w)}" height="{F(h)}" fill="{fill}" {extra}/>')
def clear(r):
 l=.15 if abs(r['x0']+17.85)<.001 else .1
 rr=.15 if abs(r['x1']-17.85)<.001 else .1
 b=.15 if abs(r['z0']+12.85)<.001 else .1
 t=.15 if abs(r['z1']-12.85)<.001 else .1
 return r['x0']+l,r['x1']-rr,r['z0']+b,r['z1']-t
colors={'teaching':'#dcebf1','collaboration':'#cce1ec','office':'#eadfca','admin':'#e8d8bd','public':'#f6e9cd','service':'#e8ebea','stair':'#dce2e1','courtyard':'#e4eddb'}
roomdata=[]
for r in p['rooms']:
 x0,x1,z0,z1=clear(r)
 roomdata.append({**r,'clearWidth':round(x1-x0,2),'clearDepth':round(z1-z0,2),'clearArea':round((x1-x0)*(z1-z0),2)})
a(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {H}" width="{W}" height="{H}" role="img" aria-labelledby="plan-title plan-desc">')
a('<title id="plan-title">学院楼一层概念户型，36 × 26 米</title><desc id="plan-desc">北侧教学、南侧办公与接待，中央采光庭院，东西两座楼梯。标注房间净面积，尺寸及门窗依统一数据源生成。</desc>')
a('''<defs><pattern id="court" width="18" height="18" patternUnits="userSpaceOnUse"><path d="M0 18L18 0" stroke="#b9cba8" stroke-width=".5"/></pattern><marker id="route-arrow" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="5" markerHeight="5" orient="auto-start-reverse"><path d="M0 0L10 5L0 10z" fill="#508466"/></marker><style>text{font-family:"PingFang SC","Hiragino Sans GB","Microsoft YaHei",sans-serif} .room{cursor:pointer;outline:none}.room:hover .room-fill,.room:focus .room-fill{filter:brightness(.95);stroke:#a46e34;stroke-width:2}.dim{font-variant-numeric:tabular-nums}.selected .room-fill{stroke:#a46e34;stroke-width:3}.route{display:none}</style></defs>''')
rect(0,0,W,H,'#fffefb')
text(80,57,'学院楼 · 一层户型草案',31,'#233c42',650,'start')
text(81,87,'JEI VIRTUAL TOUR  /  FLOOR 01  /  SPATIAL BLOCKOUT v01',13,'#778481',500,'start',extra='letter-spacing="1.5"')
text(1610,54,'36.0 × 26.0 m',24,'#233c42',600,'end')
text(1610,82,'建筑轮廓 936.0 m² · 米制 · 北上南下',13,'#778481',400,'end')
line(80,106,1610,106,'#d9ddd8',1)
# Whole dimensional frame
for xx in [X(-18),X(18)]:line(xx,150,xx,175,'#9ba7a3',1)
line(X(-18),139,X(18),139,'#71827c',1)
for xx in [X(-18),X(18)]:line(xx-4,143,xx+4,135,'#71827c',1.3)
rect(651,126,118,25,'#fffefb');text(710,144,'36 000 mm',15,'#556861',500)
for yy in [Y(13),Y(-13)]:line(128,yy,165,yy,'#9ba7a3',1)
line(137,Y(13),137,Y(-13),'#71827c',1)
for yy in [Y(13),Y(-13)]:line(133,yy+4,141,yy-4,'#71827c',1.3)
a('<g transform="translate(118 570) rotate(-90)">');rect(-58,-15,116,28,'#fffefb');text(0,5,'26 000 mm',15,'#556861',500);a('</g>')
# Base floor and room fill
rect(X(-18),Y(13),1080,780,'#f6f3e9')
for r in roomdata:
 x0,x1,z0,z1=clear(r);cx=X((x0+x1)/2);cy=Y((z0+z1)/2)
 a(f'<g class="room" id="room-{r["id"]}" data-room="{r["id"]}" tabindex="0" role="button" aria-label="{html.escape(r["nameZh"])}，净面积{r["clearArea"]:.1f}平方米">')
 rect(X(x0),Y(z1),(x1-x0)*S,(z1-z0)*S,colors[r['kind']],extra='class="room-fill"')
 if r['kind']=='courtyard':rect(X(x0),Y(z1),(x1-x0)*S,(z1-z0)*S,'url(#court)')
 if r['kind']=='stair':
  # Two clear 1.50m flights and shared half landing, indicative only.
  xa=x0+.42;xb=x1-.42;zbot=z0+.95;ztop=z1-2.15;flight=1.5
  for xx in [xa,xb-flight]:
   rect(X(xx),Y(ztop),flight*S,(ztop-zbot)*S,'#f8faf7',extra='stroke="#7d9089" stroke-width="1"')
   for n in range(1,11):
    zz=zbot+(ztop-zbot)*n/11;line(X(xx),Y(zz),X(xx+flight),Y(zz),'#8e9f99',.8)
  rect(X(xa),Y(z1-.65),(xb-xa)*S,(z1-.65-ztop)*S,'#f8faf7',extra='stroke="#7d9089" stroke-width="1"')
  line(X(xa+flight/2),Y(zbot+.2),X(xa+flight/2),Y(ztop-.2),'#536d66',1.5,extra='marker-end="url(#route-arrow)"')
  line(X(xb-flight/2),Y(ztop-.2),X(xb-flight/2),Y(zbot+.2),'#536d66',1.5,extra='marker-end="url(#route-arrow)"')
  # Labels below stair drawing; stair ID top.
  text(cx,Y(z1)+45,r['id']+' / '+('西侧楼梯 A' if r['id']=='C01' else '东侧楼梯 B'),12,'#304445',600)
  text(cx,Y(z0)-14,f'{r["clearArea"]:.1f} m² · 两跑梯示意',11,'#526963',500)
 elif r['id'].startswith('N') or r['id'].startswith('S'):
  text(cx,cy-28,r['id'],12,'#738481',500,extra='letter-spacing="1"')
  text(cx,cy+1,r['nameZh'],17 if r['id']!='S02' else 14,'#2d454a',600)
  text(cx,cy+29,f'{r["clearArea"]:.1f} m²',20,'#2d454a',500,extra='class="dim"')
  text(cx,cy+54,f'{r["clearWidth"]:.2f} × {r["clearDepth"]:.2f} m 净尺寸',11,'#6d7d79')
 elif r['kind']=='courtyard':
  text(cx,cy-36,'C04',12,'#768c65',500)
  text(cx,cy-10,'采光庭院',20,'#496538',600)
  text(cx,cy+17,f'{r["clearArea"]:.1f} m²',17,'#496538',500)
  text(cx,cy+42,'各层须统一留洞',12,'#60794e')
 else:
  labels={'C02':['女卫生间','占位'],'C03':['男卫生间','占位'],'C05':['清洁 / 储物'],'C06':['无障碍','电梯井预留'],'C07':['独立无障碍','卫生间预留'],'C08':['设备 / 弱电']}
  ls=labels.get(r['id'],[r['nameZh']]);text(cx,cy-23,r['id'],10,'#7d8d86',500)
  for n,l in enumerate(ls):text(cx,cy-5+n*17,l,12,'#435b57',550)
  text(cx,cy+(32 if len(ls)>1 else 22),f'{r["clearArea"]:.1f} m²',12,'#526963',500)
 a('</g>')
# Walls: omit openings geometrically instead of white masking
for w in p['walls']:
 t=p['externalWall'] if w['external'] else p['internalWall'];last=w['start']
 for o in sorted(w['openings'],key=lambda v:v['at']):
  end=o['at']-o['width']/2
  if end>last:
   if w['axis']=='H':line(X(last),Y(w['at']),X(end),Y(w['at']),'#41514f',t*S)
   else:line(X(w['at']),Y(last),X(w['at']),Y(end),'#41514f',t*S)
  last=o['at']+o['width']/2
 if last<w['end']:
  if w['axis']=='H':line(X(last),Y(w['at']),X(w['end']),Y(w['at']),'#41514f',t*S)
  else:line(X(w['at']),Y(last),X(w['at']),Y(w['end']),'#41514f',t*S)
 # Door / window drafting
 for o in w['openings']:
  at=o['at'];wid=o['width'];half=wid/2;px,py=(X(at),Y(w['at'])) if w['axis']=='H' else (X(w['at']),Y(at))
  if o['kind']=='window':
   if w['axis']=='H':
    line(px-half*S,py-2,px+half*S,py-2,'#64a9bf',1.5);line(px-half*S,py+2,px+half*S,py+2,'#64a9bf',1.5)
    for q in [-half,0,half]:line(px+q*S,py-4,px+q*S,py+4,'#64a9bf',1)
   else:
    line(px-2,py-half*S,px-2,py+half*S,'#64a9bf',1.5);line(px+2,py-half*S,px+2,py+half*S,'#64a9bf',1.5)
    for q in [-half,0,half]:line(px-4,py+q*S,px+4,py+q*S,'#64a9bf',1)
  else:
   a(f'<g class="door" transform="translate({px:.2f} {py:.2f})'+(' rotate(90)' if w['axis']=='V' else '')+'">')
   # Positive SVG Y is south. Central band doors open into rooms; perimeter exits open outwards.
   direction=1
   if w['axis']=='H':
    direction=-1 if w['at'] in [5.9,-3.3] else 1
   else:direction=1 if w['at']<0 else -1
   leaves=2 if wid>=1.4 else 1;d=wid*S/leaves
   for n in range(leaves):
    hinge=-half*S if n==0 else half*S;side=1 if n==0 else -1
    line(hinge,0,hinge,d*direction,'#8d9c94',1.1)
    # Quadrant arc from shut leaf tip to open leaf tip.
    a(f'<path d="M {hinge+side*d:.2f} 0 A {d:.2f} {d:.2f} 0 0 {1 if side*direction>0 else 0} {hinge:.2f} {d*direction:.2f}" fill="none" stroke="#a3afa7" stroke-width=".8"/>')
   a('</g>')
# Corridor labels and clear-width dimension ticks
text(X(0),Y(4.6)+5,'北走廊  ·  净宽 2.40 m',13,'#8b8a76',500)
text(X(0),Y(-4.6)+5,'南走廊  ·  净宽 2.40 m',13,'#8b8a76',500)
for xx in [-5.3,5.3]:
 a(f'<text x="{X(xx):.2f}" y="{Y(0):.2f}" transform="rotate(-90 {X(xx):.2f} {Y(0):.2f})" fill="#8b8a76" font-size="11" text-anchor="middle">连接廊 2.40 m 净宽</text>')
# Exit tags (side corridor ends)
for x,anch,dx in [(-18,'end',-17),(18,'start',17)]:
 for z in [-4.6,4.6]:
  xx,yy=X(x)+dx,Y(z)
  text(xx,yy+4,'室外出口',11,'#508466',500,anch)
# Main entrance
line(X(0),Y(-13)+47,X(0),Y(-13)+10,'#b68e50',2,extra='marker-end="url(#route-arrow)"')
text(X(0),Y(-13)+73,'南侧主入口  ·  2.40 m 门洞',14,'#8a693d',600)
# Optional escape route layer; conceptual, not fire-engineering verification
a('<g class="route" id="route-layer" fill="none" stroke="#508466" stroke-width="3" stroke-dasharray="9 7" opacity=".78">')
for z in [-4.6,4.6]:
 for end in [-17.95,17.95]:
  line(X(0),Y(z),X(end),Y(z),'#508466',3,extra='stroke-dasharray="9 7" marker-end="url(#route-arrow)"')
for xx in [-5.3,5.3]:line(X(xx),Y(-4.6),X(xx),Y(4.6),'#508466',3,extra='stroke-dasharray="9 7"')
line(X(0),Y(-7),X(0),Y(-12.9),'#508466',3,extra='stroke-dasharray="9 7" marker-end="url(#route-arrow)"')
a('</g>')
# Right margin: north indicator, legend, compact facts
northx=1475;northy=185
line(northx,northy+57,northx,northy+10,'#344b4d',2)
a(f'<path d="M{northx} {northy} L{northx-9} {northy+20} L{northx} {northy+14} L{northx+9} {northy+20}Z" fill="#344b4d"/>')
text(northx,northy-12,'N',19,'#344b4d',650)
text(1380,300,'功能分区',17,'#294449',600,'start')
for i,(kind,label) in enumerate([('teaching','教学 / 研讨'),('collaboration','协作 / 电脑教室'),('office','教师 / 会议办公'),('admin','行政 / 学生服务'),('public','门厅 / 接待'),('service','卫生 / 设备配套'),('stair','楼梯交通'),('courtyard','采光庭院')]):
 yy=330+i*31;rect(1380,yy-13,18,18,colors[kind],extra='rx="3"');text(1410,yy+1,label,13,'#546762',400,'start')
line(1380,602,1610,602,'#dce0da',1)
text(1380,634,'结构与交通',17,'#294449',600,'start')
for i,l in enumerate(['外墙 300 mm / 内墙 200 mm','两条主走廊净宽 2.40 m','两座端部 U 形楼梯','梯宽 1.50 m / 踏步 150 × 300','四处走廊尽端室外出口','层高预留 3.60 m','一层地坪为 ±0.000']):text(1380,664+i*27,l,13,'#687b73',400,'start')
line(1380,840,1610,840,'#dce0da',1)
line(1380,872,1413,872,'#41514f',6);text(1427,877,'墙体',12,'#687b73',400,'start')
line(1498,870,1530,870,'#64a9bf',1.5);line(1498,874,1530,874,'#64a9bf',1.5);text(1545,877,'窗',12,'#687b73',400,'start')
line(1380,908,1413,908,'#508466',3,extra='stroke-dasharray="6 4"');text(1427,913,'可切换疏散示意',12,'#687b73',400,'start')
# Scale bar, footer
scaleX=190;scaleY=1010
for i in range(5):rect(scaleX+i*S,scaleY,S,7,'#455950' if i%2==0 else '#c4cdc5')
for i in [0,1,5]:text(scaleX+i*S,scaleY+26,str(i),11,'#687b73')
text(scaleX+6*S,scaleY+26,'m',11,'#687b73')
line(80,1061,1610,1061,'#d9ddd8',1)
text(80,1089,'概念户型 / 非施工图 · 门窗标注为洞口尺寸 · 面积按墙内净尺寸计算 · 楼梯仅示意，待统一楼层衔接',12,'#7b8278',400,'start')
text(1610,1089,'数据源：Floor01_Plan.json',11,'#7b8278',400,'end')
a('</svg>')
svgstr='\n'.join(svg)
(OUT/'Floor01_Plan.svg').write_text(svgstr)
(OUT/'Floor01_Rooms.json').write_text(json.dumps(roomdata,ensure_ascii=False,indent=2))
ht='''<!doctype html><html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>学院楼一层 · 36 × 26 m</title><style>*{box-sizing:border-box}body{margin:0;color:#2d454a;background:#f0f1ea;font-family:"PingFang SC",system-ui,sans-serif}header{display:flex;gap:18px;align-items:center;padding:16px 24px;background:#fffefb;border-bottom:1px solid #d9ddd8;position:sticky;top:0;z-index:3}header b{margin-right:auto}button{border:1px solid #bdc9bd;background:#fffefb;border-radius:7px;padding:8px 12px;color:#3f5b50;cursor:pointer}button.on{background:#dfebdd;border-color:#83a283}.canvas{max-width:1680px;margin:22px auto;background:#fffefb;box-shadow:0 5px 30px #17322510}.canvas svg{display:block;width:100%;height:auto}#detail{position:fixed;right:25px;bottom:20px;width:335px;padding:20px;background:#fffefbf7;border:1px solid #cad2c6;border-radius:12px;box-shadow:0 6px 30px #17322520;z-index:4}#detail:empty{display:none}#detail h2{font-size:18px;margin:0 0 8px}#detail .id{font-size:12px;color:#8b947e}#detail .area{font-size:28px;font-weight:600;margin:13px 0 5px}#detail p{font-size:13px;line-height:1.75;margin:7px 0;color:#6d7d73}#detail .close{float:right;border:0;padding:0;font-size:20px;background:none}.tip{font-size:13px;color:#7f8e7d}@media(max-width:750px){header{padding:12px;flex-wrap:wrap;gap:9px}header b{font-size:14px}.tip{display:none}.canvas{min-width:1050px;margin:0}#detail{right:12px;bottom:12px;width:295px}body{overflow:auto}}@media print{header,#detail{display:none}.canvas{margin:0;box-shadow:none}}</style></head><body><header><b>一层概念户型 · v01</b><span class="tip">点击房间查看净尺寸与面积 · 北向为暂定</span><button id="route-toggle">显示疏散示意</button><button id="download">下载 SVG</button><button onclick="window.print()">打印</button></header><main class="canvas">__SVG__</main><aside id="detail" aria-live="polite"></aside><script>const rooms=__DATA__;let on=false;document.querySelector('#route-toggle').onclick=function(){on=!on;document.querySelector('#route-layer').style.display=on?'block':'none';this.textContent=on?'隐藏疏散示意':'显示疏散示意';this.classList.toggle('on',on)};const detail=document.querySelector('#detail');function show(id){const r=rooms.find(x=>x.id===id);document.querySelectorAll('.room').forEach(x=>x.classList.toggle('selected',x.dataset.room===id));let note=r.kind==='stair'?'两跑 U 形楼梯示意：24 级 × 150 mm 对应 3.60 m 层高，踏步深 300 mm，梯段净宽及平台深均为 1.50 m；须随各组楼层衔接复核。':r.kind==='courtyard'?'采光庭院为跨层留洞预留；所有小组须统一其位置和开口尺寸。':r.kind==='service'?'配套功能占位，洁具、设备及维护空间待下一阶段布置。':'当前为建筑空间草模，后续再布置家具、材质与灯光。';detail.innerHTML='<button class="close" aria-label="关闭">×</button><div class="id">'+r.id+' / '+r.name+'</div><h2>'+r.nameZh+'</h2><div class="area">'+r.clearArea.toFixed(1)+' m²</div><p>净尺寸 '+r.clearWidth.toFixed(2)+' × '+r.clearDepth.toFixed(2)+' m</p><p>'+note+'</p>';detail.querySelector('.close').onclick=()=>{detail.innerHTML='';document.querySelectorAll('.room').forEach(x=>x.classList.remove('selected'))}}document.querySelectorAll('.room').forEach(x=>{x.onclick=()=>show(x.dataset.room);x.onkeydown=e=>{if(e.key==='Enter'||e.key===' '){e.preventDefault();show(x.dataset.room)}}});document.querySelector('#download').onclick=()=>{const s=new XMLSerializer().serializeToString(document.querySelector('svg'));const u=URL.createObjectURL(new Blob([s],{type:'image/svg+xml'}));const a=document.createElement('a');a.href=u;a.download='Floor01_Plan.svg';a.click();setTimeout(()=>URL.revokeObjectURL(u),5000)};</script></body></html>'''
(OUT/'Floor01_Plan.html').write_text(ht.replace('__SVG__',svgstr).replace('__DATA__',json.dumps(roomdata,ensure_ascii=False)))
(OUT/'plan_source.sha256').write_text(hashlib.sha256(SOURCE.read_bytes()).hexdigest()+'  Assets/JEI_Blockout/Floor01_Plan.json\n')
print(f'Generated {len(roomdata)} rooms, gross {p["width"]*p["depth"]:.1f} m², room clear area total {sum(r["clearArea"] for r in roomdata):.2f} m²')
for r in roomdata:print(r['id'],r['nameZh'],r['clearWidth'],r['clearDepth'],r['clearArea'])
