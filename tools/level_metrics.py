#!/usr/bin/env python3
"""Level metrics: exact solution counts for the 128 classic levels and for
LevelGenerator output, using the docs/tools reference rules.

    python3 tools/level_metrics.py classic          # all 128 shipped levels
    python3 tools/level_metrics.py gen 120          # 120 boards per difficulty

A solution is a set of (tile, cell) placements whose final board wins for at
least one placement order. Static coverage (walls/switches/traps stop arms,
repels ignored) is a necessary condition, so the search enumerates covering
sets by most-constrained-cell first, then replays every order through the
reference Game for boards with switches or traps. Writes <mode>.json in cwd.
The generator here is a Python mirror of Core/Generator/LevelGenerator.cs
including Pcg32, seeded 1000*difficulty + n.
"""
import json, os, sys, time, itertools
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'docs', 'tools'))
from verify_test_vectors import Game, arms, W, H, DIRS, SPREAD_ORDER

TILES = "L R U D LR LU LD RU RD UD LRU LRD LUD RUD LRUD".split()
ARMBITS = [1,2,4,8,3,5,9,6,10,12,7,11,13,14,15]

class Pcg32:
    M = 6364136223846793005
    def __init__(self, seed, seq=54):
        self.state = 0; self.inc = ((seq << 1) | 1) & (2**64-1)
        self.next_uint(); self.state = (self.state + seed) & (2**64-1); self.next_uint()
    def next_uint(self):
        old = self.state
        self.state = (old * self.M + self.inc) & (2**64-1)
        xs = (((old >> 18) ^ old) >> 27) & 0xffffffff
        rot = old >> 59
        return ((xs >> rot) | (xs << ((-rot) & 31))) & 0xffffffff
    def next(self, bound): return self.next_uint() % bound

CONFIGS = [(2,1,5,3,5),(3,1,5,3,6),(4,0,6,2,7),(4,0,6,0,11),(5,0,6,0,11)]
SHRINK = {"L":(2,-2,0,0),"R":(0,-2,0,0),"U":(0,0,2,-2),"D":(0,0,0,-2),"LR":(2,-2,0,0),
 "LU":(2,-2,2,-2),"LD":(2,-2,0,-2),"RU":(0,-2,2,-2),"RD":(0,-2,0,-2),"UD":(0,0,2,-4),
 "LRU":(2,-4,2,-2),"LRD":(2,-4,0,-2),"LUD":(2,-2,2,-4),"RUD":(0,-2,2,-4),"LRUD":(2,-4,2,-4)}

def has(tile, d): return d in tile
def generate(diff, rng):
    pieces,xo,xc,yo,yc = CONFIGS[diff]
    board=[0]*66; tiles=[]; pi=[]; pj=[]
    for n in range(pieces):
        while True:
            tile = TILES[rng.next(15)]; overlap=False
            if diff==0 and len(tile)>=3: overlap=True
            if diff==4 and tile in ("LR","UD"): overlap=True
            if tile in tiles: overlap=True
            a,b,c,d = SHRINK[tile]
            XO,XC,YO,YC = xo+a, xc+b, yo+c, yc+d
            x = XO + rng.next(XC); y = YO + rng.next(YC)
            for k in range(n):
                if pi[k]==y or pj[k]==x: overlap=True
            if overlap: continue
            tiles.append(tile); pi.append(y); pj.append(x)
            board[y*W+x]=1
            for off in range(1,11):
                for dd in SPREAD_ORDER:
                    if dd not in tile: continue
                    di,dj = DIRS[dd]; i=y+di*off; j=x+dj*off
                    if not (0<=i<H and 0<=j<W): continue
                    if rng.next(20)-off>4: board[i*W+j]=1
            break
    return board, tiles, list(zip(pi,pj))

def static_cov(board, tile, i0, j0):
    """cells infected by placing tile at (i0,j0) on the static board; plus flags"""
    m = 1 << (i0*W+j0)
    sw=False; tr=False
    for dd in tile:
        di,dj = DIRS[dd]
        for off in range(1,11):
            i=i0+di*off; j=j0+dj*off
            if not (0<=i<H and 0<=j<W): continue
            v = board[i*W+j]
            if v==2: break
            if v==3: sw=True; break
            if v==5: tr=True; break
            if v==1: m |= 1<<(i*W+j)
    return m, sw, tr

def solve(board, tiles, cap=200000, tlimit=20.0):
    t0=time.time()
    A = 0
    for loc,v in enumerate(board):
        if v==1: A |= 1<<loc
    cells = [loc for loc,v in enumerate(board) if v==1]
    n=len(tiles)
    cov = {}  # (k,loc)->mask
    for k,t in enumerate(tiles):
        for loc in cells:
            m,_,_ = static_cov(board,t,loc//W,loc%W)
            cov[(k,loc)] = m
    # options per cell
    opts = {c:[(k,loc) for (k,loc),m in cov.items() if m>>c & 1] for c in cells}
    sols=set(); timed_out=[False]; hit_cap=[False]
    def rec(S, used, occ, chosen):
        if time.time()-t0>tlimit: timed_out[0]=True; return
        if len(sols)>=cap: hit_cap[0]=True; return
        if S & A == A:
            sols.add(frozenset((tiles[k],loc) for k,loc in chosen)); return
        # MRV: uncovered cell with fewest available options
        best=None; bo=None
        rem = A & ~S
        c=0
        while rem:
            if rem & 1:
                o=[(k,loc) for (k,loc) in opts[c] if not used>>k&1 and not occ>>loc&1]
                if best is None or len(o)<len(bo):
                    best=c; bo=o
                    if len(o)==0: return
            rem>>=1; c+=1
        for k,loc in bo:
            rec(S|cov[(k,loc)], used|1<<k, occ|1<<loc, chosen+[(k,loc)])
    rec(0,0,0,[])
    # order verification for levels with switches/traps
    dynamic = any(v in (3,5) for v in board)
    valid=set()
    if dynamic and not hit_cap[0]:  # was `not hit_cap` (a list): the check never ran before 2026-09-02
        for s in sols:
            if time.time()-t0>tlimit*2: timed_out[0]=True; break
            items=list(s)
            ok=False
            # need piece indices: assign tiles greedily (duplicates identical anyway)
            idxs=[]; pool=list(range(n))
            for t,loc in items:
                for k in pool:
                    if tiles[k]==t: idxs.append(k); pool.remove(k); break
            for perm in itertools.permutations(range(len(items))):
                g=Game(board,tiles)
                won=False
                for p in perm:
                    k=idxs[p]; loc=items[p][1]
                    if not g.can_place(k, loc//W, loc%W): won=False; break
                    won=g.set_piece(k, loc//W, loc%W)
                    if won: break
                    if not g.placed: break  # reset tripped
                if won: ok=True; break
            if ok: valid.add(s)
    else:
        valid=sols
    minp = min((len(s) for s in valid), default=None)
    return dict(solutions=len(valid), static=len(sols), min_pieces=minp, capped=hit_cap[0], timeout=timed_out[0],
                active=len(cells), walls=board.count(2), switches=board.count(3), traps=board.count(5), secs=round(time.time()-t0,2))

if __name__=="__main__":
    mode=sys.argv[1] if len(sys.argv)>1 else 'classic'
    out=[]
    if mode=="classic":
        d=json.load(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'docs', 'test_vectors.json')))['levels']
        if isinstance(d,dict): d=[d[k] for k in sorted(d, key=lambda x:int(x))]
        d=[lv for lv in d]
        import ast
        for lv in d:
            board=lv['board'] if isinstance(lv['board'],list) else ast.literal_eval(lv['board']); tiles=lv['pieces'] if isinstance(lv['pieces'],list) else ast.literal_eval(lv['pieces'])
            lv['id']=int(lv['level_id'])
            r=solve(board,tiles)
            r.update(id=lv.get('id'), pieces=len(tiles), tiles=tiles, dup_tiles=len(tiles)!=len(set(tiles)))
            out.append(r); print(r, flush=True)
    else:
        N=int(sys.argv[2])
        for diff in range(5):
            for s in range(N):
                rng=Pcg32(1000*diff+s)
                board,tiles,sol=generate(diff,rng)
                r=solve(board,tiles,cap=50000,tlimit=10)
                r.update(diff=diff, seed=s, pieces=len(tiles), tiles=tiles)
                out.append(r); print(r, flush=True)
    json.dump(out, open(f'{mode}.json','w'))
    import statistics as st
    if mode=="classic":
        print("unique-solution levels:", sum(1 for x in out if x['solutions']==1), "of", len(out))
        print("levels using walls/switches/traps:", sum(1 for x in out if x['walls']), sum(1 for x in out if x['switches']), sum(1 for x in out if x['traps']))
        print("levels needing every piece:", sum(1 for x in out if x['min_pieces']==x['pieces']))
    else:
        for diff in range(5):
            sols=sorted(x['solutions'] for x in out if x['diff']==diff)
            print(f"difficulty {diff}: median solutions {st.median(sols):.0f}, p90 {sols[9*len(sols)//10]}, unique {sum(1 for v in sols if v==1)}/{len(sols)}")
