# HLSL -> GLSL ES 3.00 port of the three Grid Infect shaders, for the WebGL
# bench only (tools/style-bench/README.md). Mechanical: type names, intrinsics
# and a handful of GLSL strictness fixes. The .shader files stay the source.
import re, sys, json
import os
HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, '..', '..', 'unity', 'Assets', '_Project', 'Game', 'Shaders') + '/'
def port(name):
    s = open(SRC + name).read()
    body = s[s.index('CBUFFER_END') + len('CBUFFER_END'):s.index('ENDHLSL')]
    # drop the vertex stage and the Varyings/Attributes structs
    body = re.sub(r'struct Attributes.*?\n', '', body)
    body = re.sub(r'struct Varyings.*?\n', '', body)
    body = re.sub(r'Varyings Vert\(Attributes input\)\s*\{.*?\n            \}\n', '', body, flags=re.S)
    # uniforms from the CBUFFER
    cb = s[s.index('CBUFFER_START(UnityPerMaterial)') + len('CBUFFER_START(UnityPerMaterial)'):s.index('CBUFFER_END')]
    uniforms = ''
    for line in cb.strip().splitlines():
        line = line.strip().rstrip(';')
        t, names = line.split(None, 1)
        t = {'float': 'float', 'float4': 'vec4', 'float2': 'vec2'}[t]
        for n in names.split(','):
            uniforms += f'uniform {t} {n.strip()};\n'
    if '_StateTex' in s: uniforms += 'uniform sampler2D _StateTex;\nuniform sampler2D _NoiseTex;\n'
    b = body
    b = re.sub(r'\bfloat([234])\b', r'vec\1', b)
    b = re.sub(r'\bhalf4\b', 'vec4', b)
    b = b.replace('SAMPLE_TEXTURE2D_LOD(_StateTex, sampler_StateTex, uv, 0)', 'texture(_StateTex, uv)')
    b = b.replace('SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, uv, 0)', 'texture(_NoiseTex, uv)')
    b = re.sub(r'\(int\)round\(([^)]*)\)', r'int(round(\1))', b)
    b = b.replace('vec4 pm = 0;', 'vec4 pm = vec4(0.0);')
    b = b.replace('vec4 Frag(Varyings input) : SV_Target', 'vec4 Frag(vec2 uv_in)')
    b = b.replace('input.uv', 'uv_in')
    b = re.sub(r'vec2 neighbours\[4\] = \{(.*?)\};', r'vec2 neighbours[4] = vec2[4](\1);', b)
    # comparisons on vectors
    b = b.replace('any(nXY < 0.0)', 'any(lessThan(nXY, vec2(0.0)))')
    b = b.replace('any(nXY >= vec2(_Cols, _Rows))', 'any(greaterThanEqual(nXY, vec2(_Cols, _Rows)))')
    b = b.replace('all(cellXY >= 0.0)', 'all(greaterThanEqual(cellXY, vec2(0.0)))')
    b = b.replace('all(cellXY < vec2(_Cols, _Rows))', 'all(lessThan(cellXY, vec2(_Cols, _Rows)))')
    b = b.replace('any(pos <= 0.0)', 'any(lessThanEqual(pos, vec2(0.0)))')
    b = b.replace('any(pos >= 1.0)', 'any(greaterThanEqual(pos, vec2(1.0)))')
    b = b.replace('all(abs(floor(pos * _Blocks) - blockInCell) < 0.5)', 'all(lessThan(abs(floor(pos * _Blocks) - blockInCell), vec2(0.5)))')
    b = b.replace('any(abs(UnpackDir(ns.b) - nd) > 0.01)', 'any(greaterThan(abs(UnpackDir(ns.b) - nd), vec2(0.01)))')
    b = b.replace('_Blocks - 1)', '_Blocks - 1.0)')
    b = b.replace('cell.x >= 0 &&', 'cell.x >= 0.0 &&').replace('cell.y >= 0 &&', 'cell.y >= 0.0 &&')
    b = b.replace('vec2(k * 7.13, k * 13.71)', 'vec2(float(k) * 7.13, float(k) * 13.71)')
    b = b.replace('vec2(_Cols - 1, _Rows - 1)', 'vec2(_Cols - 1.0, _Rows - 1.0)')
    b = b.replace('(_Rows - 1) -', '(_Rows - 1.0) -')
    b = b.replace('normalize(vec2(h1 - 0.5, h2 - 0.5) + 1e-3)', 'normalize(vec2(h1 - 0.5, h2 - 0.5) + vec2(1e-3))')
    b = b.replace('length(max(q, 0.0))', 'length(max(q, vec2(0.0)))')
    b = b.replace('if (!inLattice) return vec4(pm);', 'if (!inLattice) return pm;')
    b = b.replace('return vec4(pm);', 'return pm;')
    b = b.replace('return vec4(col, 1.0);', 'return vec4(col, 1.0);')
    b = b.replace('const vec2 dir', 'vec2 dir')
    b = re.sub(r'int(\s+)(\w+) = \(int\)', r'int\1\2 = int', b)
    pre = '''#version 300 es
precision highp float;
precision highp int;
precision highp sampler2D;
''' + uniforms + '''
float saturate(float x) { return clamp(x, 0.0, 1.0); }
vec2 saturate(vec2 x) { return clamp(x, 0.0, 1.0); }
vec3 saturate(vec3 x) { return clamp(x, 0.0, 1.0); }
float lerp(float a, float b, float t) { return mix(a, b, t); }
vec2 lerp(vec2 a, vec2 b, float t) { return mix(a, b, t); }
vec3 lerp(vec3 a, vec3 b, float t) { return mix(a, b, t); }
vec4 lerp(vec4 a, vec4 b, float t) { return mix(a, b, t); }
float frac(float x) { return fract(x); }
vec2 frac(vec2 x) { return fract(x); }
in vec2 vUv;
out vec4 fragColor;
'''
    post = '''
void main() { fragColor = Frag(vUv); }
'''
    return pre + b + post
out = {n: port(f) for n, f in [('board', 'GridInfectBoard.shader'), ('substrate', 'GridInfectSubstrate.shader'), ('glass', 'GridInfectGlass.shader')]}
open(os.path.join(HERE, 'shaders.js'), 'w').write('window.SHADERS = ' + json.dumps(out) + ';')
print('ported', list(out))
