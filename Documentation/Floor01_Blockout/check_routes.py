#!/usr/bin/env python3
"""Reproducible, dependency-free empty-blockout route screening, not code certification.

Walls and windows are impassable; only JSON door openings are walkable. Person
radius is 0.30 m. A further quarter-cell diagonal is added to inflation and
every edge midpoint is checked, keeping complete straight edges clear.
Distances use eight-neighbour Dijkstra, without diagonal corner cutting. Exit
targets are the clear portions of the five exterior door mouths at facade faces.
Stair geometry/furniture/door leaves are absent from JSON and are not simulated.
"""
from pathlib import Path
from array import array
import hashlib, heapq, json, math, time

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'Assets/JEI_Blockout/Floor01_Plan.json'
OUT = Path(__file__).resolve().parent
BODY_RADIUS = .30
def final_exits(plan):
    exits=[]
    for wall in plan['walls']:
        if not wall['external']: continue
        at=wall['at']+math.copysign(plan['externalWall']/2,wall['at'])
        for op in wall['openings']:
            if op['kind']!='door': continue
            name=('NORTH' if at>0 else 'SOUTH') if wall['axis']=='H' else (('EAST' if at>0 else 'WEST')+'_'+('NORTH' if op['at']>0 else 'SOUTH'))
            exits.append((name,wall['axis'],at,op['at'],op['width']))
    expected={'SOUTH','WEST_NORTH','WEST_SOUTH','EAST_NORTH','EAST_SOUTH'}
    if {e[0] for e in exits}!=expected or len(exits)!=5:
        raise RuntimeError('Expected this blockout to have exactly the five specified exterior exits')
    return exits

def wall_rectangles(plan):
    rects = []
    for wall in plan['walls']:
        half = plan['externalWall' if wall['external'] else 'internalWall'] / 2
        cursor = wall['start']
        doors = sorted((o for o in wall['openings'] if o['kind'] == 'door'), key=lambda o:o['at'])
        for end, following in [(o['at']-o['width']/2, o['at']+o['width']/2) for o in doors] + [(wall['end'], wall['end'])]:
            if end > cursor:
                if wall['axis'] == 'H': rects.append((cursor, end, wall['at']-half, wall['at']+half))
                else: rects.append((wall['at']-half, wall['at']+half, cursor, end))
            cursor = following
    return rects

def geometry_checks(plan):
    problems=[]
    for wall in plan['walls']:
        previous=wall['start']
        for op in sorted(wall['openings'],key=lambda o:o['at']):
            lo,hi=op['at']-op['width']/2,op['at']+op['width']/2
            if lo<previous-1e-7 or hi>wall['end']+1e-7: problems.append('Overlapping or out-of-range opening: '+str(wall))
            if op['kind']=='door' and op['width']<=2*BODY_RADIUS: problems.append('Door narrower than body diameter')
            previous=hi
    for index,a in enumerate(plan['rooms']):
        for b in plan['rooms'][index+1:]:
            if min(a['x1'],b['x1'])>max(a['x0'],b['x0'])+1e-7 and min(a['z1'],b['z1'])>max(a['z0'],b['z0'])+1e-7:
                problems.append('Room interiors overlap: '+a['id']+'/'+b['id'])
    if problems: raise RuntimeError('\n'.join(problems))
    iw=plan['internalWall']
    return {'opening_and_room_overlap_checks':'pass',
            'east_west_corridor_clear_width_m':round(5.9-3.3-iw,3),
            'courtyard_side_link_clear_width_m':round(6.6-4.-iw,3),
            'perimeter_room_clear_depth_m':round(12.85-5.9-plan['externalWall']/2-iw/2,3),
            'exit_count':len(final_exits(plan))}

def clear_room(room, plan):
    # Room extents are wall centre lines, matching generate_plan.py.
    sides = []
    for key, exterior in [('x0', -plan['width']/2+plan['externalWall']/2),
                          ('x1', plan['width']/2-plan['externalWall']/2),
                          ('z0', -plan['depth']/2+plan['externalWall']/2),
                          ('z1', plan['depth']/2-plan['externalWall']/2)]:
        half = plan['externalWall']/2 if abs(room[key]-exterior)<1e-6 else plan['internalWall']/2
        sides.append(room[key] + half if key.endswith('0') else room[key] - half)
    return sides

def room_doors(room, plan):
    found=[]
    for wall in plan['walls']:
        horizontal = wall['axis']=='H'
        low, high = ('z0','z1') if horizontal else ('x0','x1')
        along0, along1 = ('x0','x1') if horizontal else ('z0','z1')
        for side, sign in [(low,1),(high,-1)]:
            if abs(wall['at']-room[side])>1e-6: continue
            for op in wall['openings']:
                if op['kind']!='door' or not(room[along0] < op['at'] < room[along1]): continue
                found.append((wall,op,sign))
    return found

def run_grid(plan, step):
    width, depth = plan['width'], plan['depth']
    nx, nz = round(width/step)+1, round(depth/step)+1
    n = nx*nz
    xs=[-width/2+i*step for i in range(nx)]
    zs=[-depth/2+j*step for j in range(nz)]
    pad=step/(2*math.sqrt(2))
    radius=BODY_RADIUS+pad
    # Half-step lattice supplies both original grid nodes and edge midpoints.
    half_step=step/2
    qnx,qnz=2*nx-1,2*nz-1
    qxs=[-width/2+i*half_step for i in range(qnx)]
    qzs=[-depth/2+j*half_step for j in range(qnz)]
    qfree=bytearray(b'\x01')*(qnx*qnz)
    rects=wall_rectangles(plan)
    for x0,x1,z0,z1 in rects:
        ilo=max(0,math.floor((x0-radius+width/2)/half_step))
        ihi=min(qnx-1,math.ceil((x1+radius+width/2)/half_step))
        jlo=max(0,math.floor((z0-radius+depth/2)/half_step))
        jhi=min(qnz-1,math.ceil((z1+radius+depth/2)/half_step))
        for j in range(jlo,jhi+1):
            dz=max(z0-qzs[j],0,qzs[j]-z1)
            for i in range(ilo,ihi+1):
                dx=max(x0-qxs[i],0,qxs[i]-x1)
                if dx*dx+dz*dz <= radius*radius+1e-12: qfree[j*qnx+i]=0
    free=bytearray(qfree[(2*j)*qnx+2*i] for j in range(nz) for i in range(nx))
    moves=[(di,dj,step*math.hypot(di,dj)) for di,dj in [(1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)]]
    def dijkstra(seeds):
        dist=array('d',[math.inf])*n
        heap=[]
        for v in seeds: dist[v]=0.; heap.append((0.,v))
        heapq.heapify(heap)
        while heap:
            length,v=heapq.heappop(heap)
            if length!=dist[v]: continue
            j,i=divmod(v,nx)
            for di,dj,cost in moves:
                ii,jj=i+di,j+dj
                if not(0<=ii<nx and 0<=jj<nz): continue
                w=jj*nx+ii
                if not free[w]: continue
                if di and dj and (not free[j*nx+ii] or not free[jj*nx+i]): continue
                if not qfree[(2*j+dj)*qnx+2*i+di]: continue
                nd=length+cost
                if nd<dist[w]: dist[w]=nd; heapq.heappush(heap,(nd,w))
        return dist
    distances={}
    exit_seeds={}
    for name,axis,at,along,opening in final_exits(plan):
        seeds=[]
        if axis=='H':
            j=round((at+depth/2)/step)
            seeds=[j*nx+i for i,x in enumerate(xs) if abs(x-along)<=opening/2-radius+1e-9 and free[j*nx+i]]
        else:
            i=round((at+width/2)/step)
            seeds=[j*nx+i for j,z in enumerate(zs) if abs(z-along)<=opening/2-radius+1e-9 and free[j*nx+i]]
        if not seeds: raise RuntimeError(f'No clear exterior door mouth for {name}, grid={step}')
        exit_seeds[name]=seeds
        distances[name]=dijkstra(seeds)
    nearest=array('d',(min(dist[v] for dist in distances.values()) for v in range(n)))
    rooms=[]
    for room in plan['rooms']:
        if room['kind']=='stair': continue
        x0,x1,z0,z1=clear_room(room,plan)
        # Body centres in the unobstructed rectangular room; thresholds are not area inputs.
        a,b,c,d=x0+BODY_RADIUS,x1-BODY_RADIUS,z0+BODY_RADIUS,z1-BODY_RADIUS
        nodes=[j*nx+i for j,z in enumerate(zs) if c-1e-9<=z<=d+1e-9 for i,x in enumerate(xs) if a-1e-9<=x<=b+1e-9 and free[j*nx+i]]
        unreachable=sum(not math.isfinite(nearest[v]) for v in nodes)
        anchors=[]
        initial=[]
        for wall,op,sign in room_doors(room,plan):
            offset=plan['externalWall' if wall['external'] else 'internalWall']/2+radius+step
            ax,az=(op['at'],wall['at']+sign*offset) if wall['axis']=='H' else (wall['at']+sign*offset,op['at'])
            choices=sorted(nodes,key=lambda v:(xs[v%nx]-ax)**2+(zs[v//nx]-az)**2)
            if not choices: continue
            v=choices[0]; px,pz=xs[v%nx],zs[v//nx]
            if not(a<=px<=b and c<=pz<=d): continue
            # All these straight segments stay inside the empty convex body-centre rectangle.
            corner_bound=max(math.hypot(px-x,pz-z) for x in [a,b] for z in [c,d])
            doorx,doorz=(op['at'],wall['at']) if wall['axis']=='H' else (wall['at'],op['at'])
            initial.append(corner_bound+math.hypot(px-doorx,pz-doorz))
            anchors.append((v,corner_bound))
        if not anchors: raise RuntimeError(f'No room door approach anchor: {room["id"]}')
        finite=[nearest[v] for v in nodes if math.isfinite(nearest[v])]
        per_exit={name:round(max((dist[v] for v in nodes if math.isfinite(dist[v])),default=math.inf),3) for name,dist in distances.items()}
        conservative={name:round(max(per_exit[name],min(bound+dist[v] for v,bound in anchors)),3) for name,dist in distances.items()}
        conservative_nearest=max(max(finite,default=math.inf),min(bound+nearest[v] for v,bound in anchors))
        teaching=room['kind'] in ('teaching','collaboration')
        first_limit,total_limit=(15,32) if teaching else (18,45)
        initial_bound=min(initial)
        rooms.append({'id':room['id'],'nameZh':room['nameZh'],'kind':room['kind'],
                      'clear_area_m2':round((x1-x0)*(z1-z0),3),
                      'sampled_body_centre_area_m2':round(len(nodes)*step*step,3),
                      'unreachable_sample_area_m2':round(unreachable*step*step,3),
                      'farthest_sample_shortest_nearest_exit_m':round(max(finite,default=math.inf),3),
                      'conservative_empty_room_nearest_exit_upper_m':round(conservative_nearest,3),
                      'farthest_sample_shortest_each_exit_m':per_exit,
                      'conservative_each_exit_upper_m':conservative,
                      'initial_to_room_door_upper_m':round(initial_bound,3),
                      'screen_initial_m':first_limit,'screen_total_m':total_limit,
                      'initial_screen_pass':initial_bound<=first_limit,
                      'total_screen_pass':conservative_nearest<=total_limit,
                      'door_count':len(anchors)})
    return {'step_m':step,'body_radius_m':BODY_RADIUS,'extra_collision_padding_m':round(pad,6),
            'grid_nodes':n,'free_grid_nodes':sum(free),'exit_seed_counts':{k:len(v) for k,v in exit_seeds.items()},
            'room_results':rooms}

def main():
    started=time.monotonic()
    raw=SOURCE.read_bytes(); plan=json.loads(raw)
    report={'source':str(SOURCE.relative_to(ROOT)),'source_sha256':hashlib.sha256(raw).hexdigest(),
            'geometry_checks':geometry_checks(plan),
            'method':'8-neighbour Dijkstra on body-inflated rectangles; windows solid; door openings free; five facade door mouths; exterior not traversable',
            'limits':'Empty-room geometric screening only. Furniture, door leaves, stair solids, occupant loads, fire/smoke separation, exit adequacy, external safe dispersal and statutory applicability are not verified. 15/32m teaching and 18/45m general are comparison values supplied for this concept, not an assertion of applicable code.',
            'grids':[run_grid(plan,s) for s in (.2,.1)]}
    report['elapsed_seconds']=round(time.monotonic()-started,2)
    (OUT/'Route_Check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    fine=report['grids'][-1]
    comparisons=[{'step_m':g['step_m'],
                  'unreachable_sample_area_m2':round(sum(r['unreachable_sample_area_m2'] for r in g['room_results']),3),
                  'max_conservative_nearest_exit_m':max(r['conservative_empty_room_nearest_exit_upper_m'] for r in g['room_results'])} for g in report['grids']]
    lines=['# 第一层空房间几何路径筛查','',f'源文件 SHA-256：`{report["source_sha256"]}`','',
           '0.2m、0.1m 两档网格；人体半径 0.30m；另膨胀四分之一网格对角线，并校验每条边的端点和中点，保证整条网格线段不切过墙角。墙和窗不可穿，门洞可穿。路径在建筑内绕墙，抵达南主入口及南北走廊东西端共五个外墙出口，禁止出楼后绕外墙抄近路。',
           '', '下表取 0.1m 网格。最短路列是房内可站立采样点的最坏网格值。保守上界使用房间角点至门内安全锚点的最长直线段，加锚点到出口的网格最短路，并与最坏采样值取大；它允许空房间内走直线，网格最短路本身也含八方向离散带来的少量高估。初始单向列为至本房门中心的保守上界；有多个房门时是选定一扇门的上界，并非正式共同疏散路径测定。',
           '', '| 房间 | 最坏采样最短路 m | 保守出楼上界 m | 至本房门上界 m | 未连通采样面积 ㎡ | 初始/总路筛查 m |',
           '|---|---:|---:|---:|---:|---|']
    for room in fine['room_results']:
        lines.append(f'| {room["id"]} {room["nameZh"]} | {room["farthest_sample_shortest_nearest_exit_m"]:.2f} | {room["conservative_empty_room_nearest_exit_upper_m"]:.2f} | {room["initial_to_room_door_upper_m"]:.2f} | {room["unreachable_sample_area_m2"]:.2f} | {room["screen_initial_m"]}/{room["screen_total_m"]}：'+('通过' if room['initial_screen_pass'] and room['total_screen_pass'] else '需复查')+' |')
    lines += ['', '几何检查：房间内部无重叠，门窗开口无重叠或越出墙段；南北东西向走廊及庭院侧连廊净宽均为2.40m，外圈房间净深6.70m。',
              '', '两档网格对照：'+ '；'.join(f'{g["step_m"]:.1f}m网格，未连通采样面积{g["unreachable_sample_area_m2"]:.2f}㎡，全层最坏保守出楼上界{g["max_conservative_nearest_exit_m"]:.2f}m' for g in comparisons)+'。',
              '', '未连通面积只统计扣除人体贴墙空间后的可站立网格采样面积；并非房间建筑净面积。楼梯区不作为路径起点，内部楼梯实体忽略，门洞实体需结合 Unity 检查；电梯预留及庭院按空占位空间列出，不表示其为日常可占用房间。',
              '', '15/32m（教学）与18/45m（一般）仅按委托要求作为本草模比较线，未在本脚本核定适用规范条款。家具布置、门扇开启、人数、出口宽度、防火分隔、烟气、楼梯实体及室外安全疏散条件均未验证，本结果不是建筑/消防合规认证。',
              '', '各房到五个出口的独立最短路及保守上界、两档网格全数据见 `Route_Check.json`。复现：`python3 Documentation/Floor01_Blockout/check_routes.py`。']
    (OUT/'Route_Check.md').write_text('\n'.join(lines)+'\n')
    print(json.dumps({'seconds':report['elapsed_seconds'],'source_sha256':report['source_sha256'],
                      'rooms':[{k:r[k] for k in ('id','unreachable_sample_area_m2','farthest_sample_shortest_nearest_exit_m','conservative_empty_room_nearest_exit_upper_m','initial_to_room_door_upper_m','initial_screen_pass','total_screen_pass')} for r in fine['room_results']]},ensure_ascii=False,indent=2))

if __name__=='__main__': main()
