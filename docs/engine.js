/**
 * SharpOpenGL Space RTS — WebGL2 Browser Preview
 *
 * Live simulation of the space RTS game developed across Phases 0–12.
 * Renders Sector Alpha map: terrain, resource nodes, hero ship "Vanguard",
 * player fighters, worker drones, enemy units, laser combat, and full HUD.
 *
 * Controls: WASD / Arrow Keys — pan | Scroll — zoom | Click — select unit
 */

'use strict';

// ============================================================
// Matrix Math Utilities
// ============================================================
const Mat4 = {
    create()  { const m = new Float32Array(16); m[0]=m[5]=m[10]=m[15]=1; return m; },
    identity(out) { out.fill(0); out[0]=out[5]=out[10]=out[15]=1; return out; },

    perspective(out, fovY, aspect, near, far) {
        const f = 1.0 / Math.tan(fovY / 2);
        out.fill(0);
        out[0] = f / aspect; out[5] = f;
        out[10] = (far + near) / (near - far); out[11] = -1;
        out[14] = (2 * far * near) / (near - far);
        return out;
    },

    lookAt(out, eye, center, up) {
        let z0=eye[0]-center[0], z1=eye[1]-center[1], z2=eye[2]-center[2];
        let len = 1/Math.sqrt(z0*z0+z1*z1+z2*z2); z0*=len; z1*=len; z2*=len;
        let x0=up[1]*z2-up[2]*z1, x1=up[2]*z0-up[0]*z2, x2=up[0]*z1-up[1]*z0;
        len=Math.sqrt(x0*x0+x1*x1+x2*x2);
        if(len){len=1/len;x0*=len;x1*=len;x2*=len;}else{x0=x1=x2=0;}
        let y0=z1*x2-z2*x1, y1=z2*x0-z0*x2, y2=z0*x1-z1*x0;
        len=Math.sqrt(y0*y0+y1*y1+y2*y2);
        if(len){len=1/len;y0*=len;y1*=len;y2*=len;}else{y0=y1=y2=0;}
        out[0]=x0;out[1]=y0;out[2]=z0;out[3]=0;
        out[4]=x1;out[5]=y1;out[6]=z1;out[7]=0;
        out[8]=x2;out[9]=y2;out[10]=z2;out[11]=0;
        out[12]=-(x0*eye[0]+x1*eye[1]+x2*eye[2]);
        out[13]=-(y0*eye[0]+y1*eye[1]+y2*eye[2]);
        out[14]=-(z0*eye[0]+z1*eye[1]+z2*eye[2]);
        out[15]=1;
        return out;
    }
};

function vec3Normalize(v){
    const l=Math.sqrt(v[0]*v[0]+v[1]*v[1]+v[2]*v[2]);
    if(l>0){v[0]/=l;v[1]/=l;v[2]/=l;} return v;
}

// ============================================================
// RTS Camera — top-down perspective, pan + zoom
// ============================================================
class RTSCamera {
    constructor() {
        this.cx = 180;   // world X centre
        this.cz = 180;   // world Z centre (south = +Z)
        this.height = 260; // camera Y altitude (zoom)
        this.minH = 60; this.maxH = 520;
    }
    pan(dx, dz) {
        const limit = 640;
        this.cx = Math.max(-80, Math.min(limit+80, this.cx + dx));
        this.cz = Math.max(-80, Math.min(limit+80, this.cz + dz));
    }
    zoom(delta) {
        this.height = Math.max(this.minH, Math.min(this.maxH, this.height + delta));
    }
    getViewMatrix(out) {
        return Mat4.lookAt(out, [this.cx, this.height, this.cz],
                                [this.cx, 0,           this.cz],
                                [0, 0, -1]);
    }
    getProjectionMatrix(out, aspect) {
        return Mat4.perspective(out, 52 * Math.PI / 180, aspect, 0.5, 6000);
    }
}

// ============================================================
// Dynamic Geometry Batch — upload new vertex data each frame
// ============================================================
class GeomBatch {
    constructor(gl, maxVerts = 131072) {
        this.gl = gl;
        this.maxV = maxVerts;
        this.buf = new Float32Array(maxVerts * 6);
        this.count = 0;

        this.vao = gl.createVertexArray();
        gl.bindVertexArray(this.vao);
        this.vbo = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, this.vbo);
        gl.bufferData(gl.ARRAY_BUFFER, this.buf.byteLength, gl.DYNAMIC_DRAW);
        gl.vertexAttribPointer(0, 3, gl.FLOAT, false, 24, 0);
        gl.enableVertexAttribArray(0);
        gl.vertexAttribPointer(1, 3, gl.FLOAT, false, 24, 12);
        gl.enableVertexAttribArray(1);
        gl.bindVertexArray(null);
    }
    reset() { this.count = 0; }
    _v(x, y, z, r, g, b) {
        const i = this.count * 6;
        if (this.count >= this.maxV) return;
        this.buf[i]=x; this.buf[i+1]=y; this.buf[i+2]=z;
        this.buf[i+3]=r; this.buf[i+4]=g; this.buf[i+5]=b;
        this.count++;
    }
    // Flat XZ triangle (y constant)
    tri(x1,z1, x2,z2, x3,z3, y, r,g,b) {
        this._v(x1,y,z1,r,g,b); this._v(x2,y,z2,r,g,b); this._v(x3,y,z3,r,g,b);
    }
    // Axis-aligned quad (2 tris)
    quad(x,z, w,d, y, r,g,b) {
        this.tri(x,z, x+w,z, x+w,z+d, y,r,g,b);
        this.tri(x,z, x+w,z+d, x,z+d, y,r,g,b);
    }
    // Filled circle (polygon approximation)
    circle(cx,cz, radius, segs, y, r,g,b) {
        for(let i=0;i<segs;i++){
            const a1=(i/segs)*Math.PI*2, a2=((i+1)/segs)*Math.PI*2;
            this.tri(cx,cz, cx+Math.cos(a1)*radius,cz+Math.sin(a1)*radius,
                            cx+Math.cos(a2)*radius,cz+Math.sin(a2)*radius, y,r,g,b);
        }
    }
    // Ring (donut)
    ring(cx,cz, r1,r2, segs, y, r,g,b) {
        for(let i=0;i<segs;i++){
            const a1=(i/segs)*Math.PI*2, a2=((i+1)/segs)*Math.PI*2;
            const i1x=Math.cos(a1), i1z=Math.sin(a1);
            const i2x=Math.cos(a2), i2z=Math.sin(a2);
            this.tri(cx+i1x*r1,cz+i1z*r1, cx+i2x*r1,cz+i2z*r1, cx+i2x*r2,cz+i2z*r2, y,r,g,b);
            this.tri(cx+i1x*r1,cz+i1z*r1, cx+i2x*r2,cz+i2z*r2, cx+i1x*r2,cz+i1z*r2, y,r,g,b);
        }
    }
    // Ship triangle: pointing in `angle` (radians, 0=east, PI/2=south)
    ship(wx,wz, angle, scale, y, r,g,b) {
        const cos=Math.cos(angle), sin=Math.sin(angle);
        const rgt=[sin,-cos];
        const nx=wx+cos*scale*1.5,  nz=wz+sin*scale*1.5;
        const lx=wx-cos*scale*0.8+rgt[0]*scale*0.8, lz=wz-sin*scale*0.8+rgt[1]*scale*0.8;
        const rx=wx-cos*scale*0.8-rgt[0]*scale*0.8, rz=wz-sin*scale*0.8-rgt[1]*scale*0.8;
        this.tri(nx,nz, lx,lz, rx,rz, y,r,g,b);
    }
    // Outlined hexagon
    hexagon(cx,cz, radius, y, r,g,b) {
        for(let i=0;i<6;i++){
            const a1=(i/6)*Math.PI*2-Math.PI/6, a2=((i+1)/6)*Math.PI*2-Math.PI/6;
            this.tri(cx,cz, cx+Math.cos(a1)*radius,cz+Math.sin(a1)*radius,
                            cx+Math.cos(a2)*radius,cz+Math.sin(a2)*radius, y,r,g,b);
        }
    }
    // Small health bar quad
    hpBar(wx,wz, width, frac, y) {
        const h=2.5, hh=width/2;
        this.quad(wx-hh, wz-0.8, width*frac, h, y, 0.2+0.6*frac, 0.85*frac, 0.2);
        if(frac<1) this.quad(wx-hh+width*frac, wz-0.8, width*(1-frac), h, y, 0.25,0.25,0.25);
    }
    flush(gl, uniforms, identity, mode) {
        if(!this.count) return;
        gl.bindBuffer(gl.ARRAY_BUFFER, this.vbo);
        gl.bufferSubData(gl.ARRAY_BUFFER, 0, this.buf.subarray(0, this.count*6));
        gl.uniformMatrix4fv(uniforms.model, false, identity);
        gl.uniform4f(uniforms.color, 0,0,0,0);
        gl.bindVertexArray(this.vao);
        gl.drawArrays(mode, 0, this.count);
        gl.bindVertexArray(null);
    }
}

// ============================================================
// Map Data — Sector Alpha (mirrors sector_alpha.json)
// ============================================================
const C = 10; // cell size in world units
const MAP = 64; // cells
// Asteroid field cells
const ASTEROID_CELLS = [[10,10],[10,11],[11,10],[11,11],[12,10],[12,11],[13,10]];
// Asteroid rect2 [x0,z0,x1,z1]
const ASTEROID_RECT2 = [40,35,45,40];
// Nebula rect [x0,z0,x1,z1]
const NEBULA_RECT = [20,20,30,30];
// Debris cells
const DEBRIS_CELLS = [[30,5],[31,5],[32,5],[30,6],[31,6]];

// Resource nodes (world coords)
const RES_NODES = [
    { type:'energy',   wx:150, wz:150, max:5000, amount:5000 },
    { type:'minerals', wx:450, wz:200, max:3000, amount:3000 },
    { type:'data',     wx:320, wz:480, max:2000, amount:2000 },
    { type:'crew',     wx:80,  wz:500, max:1500, amount:1500 },
];
const RES_COLOR = {
    energy:   [1.0, 0.85, 0.0],
    minerals: [0.0, 0.85, 1.0],
    data:     [0.8, 0.2,  1.0],
    crew:     [0.4, 1.0,  0.5],
};
const RES_DOM = {
    energy: 'val-energy', minerals: 'val-minerals', data: 'val-data', crew: 'val-crew'
};

// ============================================================
// Game State
// ============================================================
let _uid = 0;
function mkUnit(name, type, wx, wz, maxHp, shields, speed, range, dmg, fireRate, team, scale) {
    return {
        id: _uid++, name, type, wx, wz,
        angle: team===0 ? -Math.PI*0.25 : Math.PI*0.75,
        hp: maxHp, maxHp, shields, maxShields: shields,
        speed, range, dmg, fireRate, fireCooldown: Math.random()*fireRate,
        team, scale, alive: true,
        cargo: 0, cargoMax: 20, harvestTarget: null, returning: false,
        patrolT: Math.random()*Math.PI*2,
    };
}

const PLAYER_BASE = { wx: 30, wz: 30 };
const ENEMY_BASE  = { wx: 600, wz: 600 };

const UNITS = [
    mkUnit('Vanguard',  'hero',    35, 35,  1000, 500, 80, 180, 25, 0.25, 0, 4.5),
    mkUnit('Fighter-1', 'fighter', 20, 25,  100,  0,   120, 100, 10, 0.33, 0, 2.2),
    mkUnit('Fighter-2', 'fighter', 25, 15,  100,  0,   120, 100, 10, 0.33, 0, 2.2),
    mkUnit('Fighter-3', 'fighter', 15, 38,  100,  0,   120, 100, 10, 0.33, 0, 2.2),
    mkUnit('Drone-A',   'worker',  48, 28,   50,  0,    60, 0,    0, 0,    0, 1.6),
    mkUnit('Drone-B',   'worker',  32, 48,   50,  0,    60, 0,    0, 0,    0, 1.6),
    mkUnit('Ravager-1', 'fighter', 580, 580, 150, 0,   100, 100, 12, 0.33, 1, 2.2),
    mkUnit('Ravager-2', 'fighter', 575, 592, 150, 0,   100, 100, 12, 0.33, 1, 2.2),
    mkUnit('Ravager-3', 'fighter', 592, 575, 150, 0,   100, 100, 12, 0.33, 1, 2.2),
    mkUnit('Ravager-4', 'fighter', 568, 572, 150, 0,   100, 100, 12, 0.33, 1, 2.2),
    mkUnit('Bomber-1',  'bomber',  585, 560, 300, 100, 60,  120, 40, 0.15, 1, 3.5),
    mkUnit('Reaper',    'hero',    610, 610, 800, 300, 70,  200, 30, 0.20, 1, 4.5),
];

const PROJECTILES = [];
const EXPLOSIONS  = [];

const RESOURCES = { energy: 500, minerals: 300, data: 0, crew: 10 };
let gameTime = 0;
let selectedId = -1;

function dist2(ax,az, bx,bz){ const dx=ax-bx,dz=az-bz; return dx*dx+dz*dz; }

function spawnProjectile(src, tgt) {
    PROJECTILES.push({
        x: src.wx, z: src.wz,
        tx: tgt.wx, tz: tgt.wz,
        targetId: tgt.id,
        speed: 280,
        dmg: src.dmg,
        team: src.team,
        r: src.team===0?0.3:1.0,
        g: src.team===0?0.8:0.25,
        b: src.team===0?1.0:0.2,
        life: 1.2,
    });
}

function updateGame(dt) {
    gameTime += dt;

    // Passive resource income
    RESOURCES.energy   = Math.min(2000, RESOURCES.energy   + 5   * dt);
    RESOURCES.crew     = Math.min(200,  RESOURCES.crew     + 0.1 * dt);

    const alive = UNITS.filter(u => u.alive);

    for (const u of alive) {
        if (u.type === 'worker') {
            updateWorker(u, dt);
            continue;
        }

        // Find nearest enemy
        let nearest = null, nearD2 = Infinity;
        for (const e of alive) {
            if (e.team !== u.team) {
                const d2 = dist2(u.wx, u.wz, e.wx, e.wz);
                if (d2 < nearD2) { nearD2 = d2; nearest = e; }
            }
        }

        if (nearest) {
            const rangeSq = u.range * u.range;
            if (nearD2 > rangeSq) {
                // Move toward enemy
                const d = Math.sqrt(nearD2);
                const nx = (nearest.wx - u.wx)/d, nz = (nearest.wz - u.wz)/d;
                u.wx += nx * u.speed * dt;
                u.wz += nz * u.speed * dt;
                u.angle = Math.atan2(nz, nx);
            } else {
                // Face enemy
                const d = Math.sqrt(nearD2);
                u.angle = Math.atan2((nearest.wz-u.wz)/d, (nearest.wx-u.wx)/d);
                // Fire
                u.fireCooldown -= dt;
                if (u.fireCooldown <= 0) {
                    u.fireCooldown = u.fireRate + Math.random()*0.1;
                    spawnProjectile(u, nearest);
                }
            }
        } else {
            // Patrol near base
            u.patrolT += dt * 0.4;
            const base = u.team === 0 ? PLAYER_BASE : ENEMY_BASE;
            const pr = u.type==='hero'?30:60;
            const tx = base.wx + Math.cos(u.patrolT)*pr;
            const tz = base.wz + Math.sin(u.patrolT)*pr;
            const d = Math.sqrt(dist2(u.wx,u.wz,tx,tz));
            if (d > 5) {
                const nx=(tx-u.wx)/d, nz=(tz-u.wz)/d;
                u.wx += nx*u.speed*0.5*dt;
                u.wz += nz*u.speed*0.5*dt;
                u.angle = Math.atan2(nz, nx);
            }
        }

        // Shield regen
        if (u.shields < u.maxShields) u.shields = Math.min(u.maxShields, u.shields + 5*dt);
    }

    // Projectiles
    for (let i = PROJECTILES.length-1; i >= 0; i--) {
        const p = PROJECTILES[i];
        p.life -= dt;
        // Update target position
        const tgt = UNITS.find(u=>u.id===p.targetId && u.alive);
        if (tgt) { p.tx=tgt.wx; p.tz=tgt.wz; }

        const dx=p.tx-p.x, dz=p.tz-p.z;
        const d=Math.sqrt(dx*dx+dz*dz);
        if (d < 8 || p.life <= 0) {
            if (tgt && d < 20) {
                // Apply damage (shields first)
                if (tgt.shields > 0) {
                    tgt.shields = Math.max(0, tgt.shields - p.dmg);
                } else {
                    tgt.hp -= p.dmg;
                    if (tgt.hp <= 0) {
                        tgt.alive = false;
                        EXPLOSIONS.push({ wx:tgt.wx, wz:tgt.wz, life:0.6, maxLife:0.6, scale:tgt.scale*3 });
                    }
                }
            }
            PROJECTILES.splice(i, 1);
        } else {
            p.x += dx/d * p.speed * dt;
            p.z += dz/d * p.speed * dt;
        }
    }

    // Explosions
    for (let i = EXPLOSIONS.length-1; i >= 0; i--) {
        EXPLOSIONS[i].life -= dt;
        if (EXPLOSIONS[i].life <= 0) EXPLOSIONS.splice(i, 1);
    }

    // Respawn fallen units after delay (demo keeps running)
    for (const u of UNITS) {
        if (!u.alive) {
            u._respawn = (u._respawn||0) + dt;
            if (u._respawn > 12) {
                u._respawn = 0;
                u.alive = true;
                u.hp = u.maxHp;
                u.shields = u.maxShields;
                const base = u.team===0?PLAYER_BASE:ENEMY_BASE;
                u.wx = base.wx + (Math.random()-0.5)*30;
                u.wz = base.wz + (Math.random()-0.5)*30;
            }
        }
    }
}

function updateWorker(w, dt) {
    const alive = UNITS.filter(u=>u.alive);
    if (w.returning || w.cargo >= w.cargoMax) {
        w.returning = true;
        const dx=PLAYER_BASE.wx-w.wx, dz=PLAYER_BASE.wz-w.wz;
        const d=Math.sqrt(dx*dx+dz*dz);
        if (d < 12) {
            // Deposit
            RESOURCES.energy = Math.min(2000, RESOURCES.energy + w.cargo);
            w.cargo = 0; w.returning = false;
        } else {
            w.wx += dx/d*w.speed*dt; w.wz += dz/d*w.speed*dt;
            w.angle = Math.atan2(dz, dx);
        }
        return;
    }
    // Pick nearest non-depleted resource node
    if (!w.harvestTarget) {
        let best=null, bD=Infinity;
        for (const n of RES_NODES) {
            if (n.amount > 0) {
                const d2=dist2(w.wx,w.wz,n.wx,n.wz);
                if(d2<bD){bD=d2;best=n;}
            }
        }
        w.harvestTarget = best;
    }
    if (!w.harvestTarget) return;
    const n = w.harvestTarget;
    const dx=n.wx-w.wx, dz=n.wz-w.wz;
    const d=Math.sqrt(dx*dx+dz*dz);
    if (d < 12) {
        const rate=5*dt;
        const take=Math.min(rate, n.amount, w.cargoMax-w.cargo);
        n.amount -= take; w.cargo += take;
        if (n.amount <= 0) w.harvestTarget = null;
    } else {
        w.wx += dx/d*w.speed*dt; w.wz += dz/d*w.speed*dt;
        w.angle = Math.atan2(dz, dx);
    }
}

// ============================================================
// Input Handler — WASD pan, scroll zoom, click select
// ============================================================
class RTSInputHandler {
    constructor(canvas) {
        this.keys = {};
        this.scrollDelta = 0;
        this.clickWorld = null;
        document.addEventListener('keydown', e => { this.keys[e.code]=true; });
        document.addEventListener('keyup',   e => { this.keys[e.code]=false; });
        canvas.addEventListener('wheel', e => {
            this.scrollDelta += e.deltaY * 0.5;
            e.preventDefault();
        }, { passive: false });
        canvas.addEventListener('click', e => {
            this.clickWorld = { cx: e.clientX, cy: e.clientY };
        });
    }
    consumeScroll() { const v=this.scrollDelta; this.scrollDelta=0; return v; }
    consumeClick()  { const v=this.clickWorld;  this.clickWorld=null; return v; }
}

// ============================================================
// Minimap
// ============================================================
function renderMinimap(mmCanvas, camera) {
    const ctx = mmCanvas.getContext('2d');
    const W = mmCanvas.width, H = mmCanvas.height;
    const scale = W / (MAP * C);
    ctx.fillStyle = '#020208';
    ctx.fillRect(0, 0, W, H);

    // Nebula
    ctx.fillStyle = 'rgba(50,20,100,0.6)';
    ctx.fillRect(NEBULA_RECT[0]*C*scale, NEBULA_RECT[1]*C*scale,
        (NEBULA_RECT[2]-NEBULA_RECT[0])*C*scale, (NEBULA_RECT[3]-NEBULA_RECT[1])*C*scale);

    // Asteroids
    ctx.fillStyle = '#445';
    for (const [cx,cz] of ASTEROID_CELLS)
        ctx.fillRect(cx*C*scale, cz*C*scale, C*scale+1, C*scale+1);
    const [ax0,az0,ax1,az1]=ASTEROID_RECT2;
    ctx.fillRect(ax0*C*scale, az0*C*scale, (ax1-ax0)*C*scale, (az1-az0)*C*scale);

    // Resource nodes
    for (const n of RES_NODES) {
        const [r,g,b] = RES_COLOR[n.type];
        ctx.fillStyle = `rgb(${r*255|0},${g*255|0},${b*255|0})`;
        ctx.beginPath();
        ctx.arc(n.wx*scale, n.wz*scale, 3, 0, Math.PI*2);
        ctx.fill();
    }

    // Bases
    ctx.fillStyle = '#4fc3f7';
    ctx.fillRect(PLAYER_BASE.wx*scale-3, PLAYER_BASE.wz*scale-3, 6, 6);
    ctx.fillStyle = '#ef5350';
    ctx.fillRect(ENEMY_BASE.wx*scale-3, ENEMY_BASE.wz*scale-3, 6, 6);

    // Units
    for (const u of UNITS) {
        if (!u.alive) continue;
        ctx.fillStyle = u.team===0 ? '#4fc3f7' : '#ef5350';
        ctx.beginPath();
        ctx.arc(u.wx*scale, u.wz*scale, 1.5, 0, Math.PI*2);
        ctx.fill();
    }

    // Camera viewport
    const aspect = window.innerWidth / window.innerHeight;
    const visH = 2 * camera.height * Math.tan(26 * Math.PI / 180);
    const visW = visH * aspect;
    ctx.strokeStyle = 'rgba(255,255,255,0.35)';
    ctx.lineWidth = 1;
    ctx.strokeRect(
        (camera.cx - visW/2) * scale,
        (camera.cz - visH/2) * scale,
        visW * scale, visH * scale
    );
}

// ============================================================
// HUD DOM Updater
// ============================================================
function updateHUD(dt) {
    document.getElementById('val-energy').textContent   = RESOURCES.energy.toFixed(0);
    document.getElementById('val-minerals').textContent = RESOURCES.minerals.toFixed(0);
    document.getElementById('val-data').textContent     = RESOURCES.data.toFixed(0);
    document.getElementById('val-crew').textContent     = RESOURCES.crew.toFixed(1);
    const t = Math.floor(gameTime);
    document.getElementById('game-time').textContent =
        'T+' + Math.floor(t/60) + ':' + String(t%60).padStart(2,'0');

    // Selected unit info
    const uip = document.getElementById('unit-info');
    const sel = UNITS.find(u => u.id === selectedId && u.alive);
    if (sel) {
        uip.style.display = 'block';
        document.getElementById('ui-name').textContent = sel.name;
        document.getElementById('ui-type').textContent =
            (sel.team===0?'Player ':'Enemy ') + sel.type.charAt(0).toUpperCase()+sel.type.slice(1);
        const hpFrac = sel.hp / sel.maxHp;
        document.getElementById('ui-hp-bar').style.width = (hpFrac*100).toFixed(1)+'%';
        document.getElementById('ui-hp-bar').style.background =
            hpFrac > 0.5 ? '#4caf50' : hpFrac > 0.25 ? '#ff9800' : '#f44336';
        document.getElementById('ui-hp-text').textContent =
            `HP: ${sel.hp.toFixed(0)}/${sel.maxHp}` +
            (sel.maxShields>0 ? `  | Shield: ${sel.shields.toFixed(0)}/${sel.maxShields}` : '');
        document.getElementById('ui-extra').textContent =
            sel.type==='worker'
                ? `Cargo: ${sel.cargo.toFixed(1)}/${sel.cargoMax}  ${sel.returning?'→ Base':'→ Node'}`
                : `Range: ${sel.range}  Dmg: ${sel.dmg}  Speed: ${sel.speed}`;
    } else {
        uip.style.display = 'none';
    }
}

// ============================================================
// Render Scene
// ============================================================
function renderScene(batch, identity, pulse) {
    batch.reset();

    // ---- Terrain background quads ----
    const nx0=NEBULA_RECT[0]*C, nz0=NEBULA_RECT[1]*C;
    const nw=(NEBULA_RECT[2]-NEBULA_RECT[0])*C, nh=(NEBULA_RECT[3]-NEBULA_RECT[1])*C;
    batch.quad(nx0, nz0, nw, nh, -1, 0.06, 0.02, 0.18);
    // Inner nebula glow
    batch.quad(nx0+nw*0.15, nz0+nh*0.15, nw*0.7, nh*0.7, -1, 0.10, 0.04, 0.28);

    // Asteroid cells
    for (const [cx,cz] of ASTEROID_CELLS)
        batch.quad(cx*C, cz*C, C, C, -1, 0.24, 0.26, 0.28);
    const [ax0,az0,ax1,az1] = ASTEROID_RECT2;
    for (let cx=ax0; cx<ax1; cx++)
        for (let cz=az0; cz<az1; cz++)
            batch.quad(cx*C, cz*C, C, C, -1, 0.22, 0.24, 0.26);

    // Debris
    for (const [cx,cz] of DEBRIS_CELLS)
        batch.quad(cx*C+2, cz*C+2, C-4, C-4, -1, 0.18, 0.20, 0.22);

    // ---- Resource nodes ----
    for (const n of RES_NODES) {
        const [r,g,b] = RES_COLOR[n.type];
        const frac = n.amount / n.max;
        const glow = 14 + 6 * Math.sin(pulse * 2 + n.wx * 0.05);
        // Outer glow ring
        batch.ring(n.wx, n.wz, glow*0.55, glow*0.85, 16, -0.5,
            r*0.5*frac, g*0.5*frac, b*0.5*frac);
        // Core
        batch.circle(n.wx, n.wz, glow*0.5, 12, -0.5, r*frac, g*frac, b*frac);
    }

    // ---- Player base (blue hexagon) ----
    batch.hexagon(PLAYER_BASE.wx, PLAYER_BASE.wz, 14, -0.5, 0.10, 0.38, 0.65);
    batch.hexagon(PLAYER_BASE.wx, PLAYER_BASE.wz, 10, -0.5, 0.18, 0.56, 0.88);
    batch.ring(PLAYER_BASE.wx, PLAYER_BASE.wz, 11, 14, 6, -0.5, 0.1, 0.5, 0.9);

    // ---- Enemy base (red hexagon) ----
    batch.hexagon(ENEMY_BASE.wx, ENEMY_BASE.wz, 14, -0.5, 0.50, 0.10, 0.10);
    batch.hexagon(ENEMY_BASE.wx, ENEMY_BASE.wz, 10, -0.5, 0.75, 0.18, 0.18);
    batch.ring(ENEMY_BASE.wx, ENEMY_BASE.wz, 11, 14, 6, -0.5, 0.7, 0.1, 0.1);

    // ---- Units ----
    for (const u of UNITS) {
        if (!u.alive) continue;
        const flash = (selectedId === u.id) ? 0.35 * (0.5 + 0.5*Math.sin(pulse*6)) : 0;
        if (u.team === 0) {
            if (u.type === 'hero') {
                batch.ship(u.wx, u.wz, u.angle, u.scale, 0,
                    0.15+flash, 0.75+flash*0.3, 1.0);
                batch.ship(u.wx, u.wz, u.angle, u.scale*0.7, 0, 0.6, 0.9, 1.0);
            } else if (u.type === 'worker') {
                batch.circle(u.wx, u.wz, u.scale, 6, 0, 0.3+flash, 0.7+flash*0.2, 0.5);
            } else {
                batch.ship(u.wx, u.wz, u.angle, u.scale, 0,
                    0.1+flash, 0.55+flash*0.2, 0.9);
            }
        } else {
            if (u.type === 'hero') {
                batch.ship(u.wx, u.wz, u.angle, u.scale, 0, 1.0, 0.15+flash, 0.10);
                batch.ship(u.wx, u.wz, u.angle, u.scale*0.7, 0, 1.0, 0.55, 0.35);
            } else if (u.type === 'bomber') {
                batch.ship(u.wx, u.wz, u.angle, u.scale, 0, 0.9+flash, 0.2, 0.1);
                batch.circle(u.wx, u.wz, u.scale*0.5, 6, 0, 0.7, 0.1, 0.05);
            } else {
                batch.ship(u.wx, u.wz, u.angle, u.scale, 0, 0.9+flash, 0.15, 0.10);
            }
        }
        // Health bar
        const hpFrac = u.hp / u.maxHp;
        const bw = u.scale * 2.2;
        batch.hpBar(u.wx, u.wz - u.scale*1.8, bw, hpFrac, 0.5);
        // Shield bar (above HP bar)
        if (u.maxShields > 0) {
            const sf = u.shields / u.maxShields;
            if (sf > 0) batch.quad(u.wx-bw/2, u.wz-u.scale*1.8-3.5, bw*sf, 2.0, 0.5, 0.2, 0.5, 1.0);
        }
    }

    // ---- Explosions ----
    for (const ex of EXPLOSIONS) {
        const t = 1 - ex.life/ex.maxLife;
        const r = ex.scale * (1 + t*2);
        const bright = ex.life / ex.maxLife;
        batch.circle(ex.wx, ex.wz, r*0.7, 10, 0.3, bright, bright*0.5, 0.1);
        batch.circle(ex.wx, ex.wz, r*0.35, 8, 0.4, 1.0, bright*0.8, 0.2);
    }
}

// Line batch for projectiles (drawn as GL_LINES)
function renderProjectiles(batch, identity, gl, uniforms) {
    batch.reset();
    for (const p of PROJECTILES) {
        const dx = p.tx - p.x, dz = p.tz - p.z;
        const d = Math.sqrt(dx*dx+dz*dz) || 1;
        const tail = 10;
        batch._v(p.x - dx/d*tail, 0.3, p.z - dz/d*tail, p.r*0.3, p.g*0.3, p.b*0.3);
        batch._v(p.x, 0.3, p.z, p.r, p.g, p.b);
    }
    batch.flush(gl, uniforms, identity, gl.LINES);
}

// Thin star-scatter rendered as POINTS behind the terrain
function renderStars(starBatch, identity, gl, uniforms) {
    starBatch.flush(gl, uniforms, identity, gl.POINTS);
}

// ============================================================
// Main Engine
// ============================================================
(function main() {
    const canvas = document.getElementById('glcanvas');
    const gl = canvas.getContext('webgl2', { antialias: true });
    if (!gl) {
        document.body.innerHTML = '<h2 style="color:red;padding:48px">WebGL2 not supported in this browser.</h2>';
        return;
    }

    function resize() {
        canvas.width  = window.innerWidth;
        canvas.height = window.innerHeight;
        gl.viewport(0, 0, canvas.width, canvas.height);
    }
    window.addEventListener('resize', resize);
    resize();

    // ── Shaders ──
    const vsrc = `#version 300 es
precision highp float;
layout(location=0) in vec3 aPos;
layout(location=1) in vec3 aCol;
uniform mat4 projection, view, model;
uniform vec4 overrideColor;
out vec3 vCol;
void main(){
    gl_Position = projection * view * model * vec4(aPos,1.0);
    gl_PointSize = 2.0;
    vCol = overrideColor.a>0.0 ? overrideColor.rgb : aCol;
}`;
    const fsrc = `#version 300 es
precision highp float;
in vec3 vCol;
out vec4 outColor;
void main(){ outColor = vec4(vCol,1.0); }`;

    function compileShader(type, src) {
        const s = gl.createShader(type);
        gl.shaderSource(s, src); gl.compileShader(s);
        if (!gl.getShaderParameter(s, gl.COMPILE_STATUS)) {
            console.error(gl.getShaderInfoLog(s)); gl.deleteShader(s); return null;
        }
        return s;
    }
    const prog = gl.createProgram();
    gl.attachShader(prog, compileShader(gl.VERTEX_SHADER, vsrc));
    gl.attachShader(prog, compileShader(gl.FRAGMENT_SHADER, fsrc));
    gl.linkProgram(prog);
    if (!gl.getProgramParameter(prog, gl.LINK_STATUS)) {
        console.error(gl.getProgramInfoLog(prog)); return;
    }
    const U = {
        projection: gl.getUniformLocation(prog, 'projection'),
        view:       gl.getUniformLocation(prog, 'view'),
        model:      gl.getUniformLocation(prog, 'model'),
        color:      gl.getUniformLocation(prog, 'overrideColor'),
    };

    // ── Geometry buffers ──
    const batch    = new GeomBatch(gl, 131072);
    const projBatch = new GeomBatch(gl, 8192);

    // Pre-generate static star field (world-space points at y=-1)
    const starBatch = new GeomBatch(gl, 1200);
    {
        let s = 42;
        const rng = () => { s=(s*1103515245+12345)&0x7fffffff; return s/0x7fffffff; };
        for (let i = 0; i < 600; i++) {
            const wx = rng() * 840 - 100;
            const wz = rng() * 840 - 100;
            const br = 0.3 + rng() * 0.5;
            starBatch._v(wx, -1, wz, br*0.8, br*0.85, br);
        }
    }

    const camera = new RTSCamera();
    const input  = new RTSInputHandler(canvas);
    const mmCanvas = document.getElementById('minimap');
    const identity = Mat4.create();

    gl.clearColor(0.01, 0.01, 0.03, 1);
    gl.enable(gl.DEPTH_TEST);
    gl.depthFunc(gl.LEQUAL);

    const projMatrix = Mat4.create(), viewMatrix = Mat4.create();
    let lastTime = performance.now(), frameCount = 0, lastFps = performance.now();
    const fpsEl = document.getElementById('fps');
    let pulse = 0;

    // Click-to-select: project click to world XZ
    function pickUnit(cx, cy) {
        const aspect = canvas.width / canvas.height;
        // Approximate: camera looks straight down; screen → world with linear map
        const visH = 2 * camera.height * Math.tan(26 * Math.PI / 180);
        const visW = visH * aspect;
        const ndcX = (cx / canvas.width)  * 2 - 1;
        const ndcY = 1 - (cy / canvas.height) * 2;
        const wx = camera.cx + ndcX * visW * 0.5;
        const wz = camera.cz - ndcY * visH * 0.5;
        let best = null, bD = 300;
        for (const u of UNITS) {
            if (!u.alive) continue;
            const d = Math.sqrt(dist2(wx, wz, u.wx, u.wz));
            if (d < bD) { bD = d; best = u; }
        }
        return best ? best.id : -1;
    }

    function frame(now) {
        const dt = Math.min((now - lastTime) / 1000, 0.05);
        lastTime = now;
        pulse += dt;

        // FPS
        frameCount++;
        if (now - lastFps >= 1000) {
            fpsEl.textContent = 'FPS: ' + frameCount;
            frameCount = 0; lastFps = now;
        }

        // Input
        const PAN = camera.height * 0.9 * dt;
        if (input.keys['KeyW']     || input.keys['ArrowUp'])    camera.pan(0,  -PAN);
        if (input.keys['KeyS']     || input.keys['ArrowDown'])  camera.pan(0,   PAN);
        if (input.keys['KeyA']     || input.keys['ArrowLeft'])  camera.pan(-PAN, 0);
        if (input.keys['KeyD']     || input.keys['ArrowRight']) camera.pan( PAN, 0);
        camera.zoom(input.consumeScroll());
        const clk = input.consumeClick();
        if (clk) selectedId = pickUnit(clk.cx, clk.cy);

        // Game simulation
        updateGame(dt);
        updateHUD(dt);
        renderMinimap(mmCanvas, camera);

        // ── WebGL Render ──
        gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);

        const aspect = canvas.width / canvas.height;
        camera.getProjectionMatrix(projMatrix, aspect);
        camera.getViewMatrix(viewMatrix);

        gl.useProgram(prog);
        gl.uniformMatrix4fv(U.projection, false, projMatrix);
        gl.uniformMatrix4fv(U.view,       false, viewMatrix);

        // Stars (background, y=-1)
        renderStars(starBatch, identity, gl, U);

        // Scene geometry (terrain, bases, units, effects)
        renderScene(batch, identity, pulse);
        batch.flush(gl, U, identity, gl.TRIANGLES);

        // Projectiles (lines)
        renderProjectiles(projBatch, identity, gl, U);

        requestAnimationFrame(frame);
    }
    requestAnimationFrame(frame);
    console.log('SharpOpenGL Space RTS WebGL2 preview initialised.');
})();

// ============================================================
// WebAudio Manager (Phase 10 — Audio)
// Mirrors the C# IAudioManager interface for the browser build.
// ============================================================

/**
 * @typedef {'WeaponFire'|'WeaponLaunch'|'Explosion'|'ShieldHit'|
 *           'UnitMoveAck'|'UnitAttackAck'|'ResourceCollected'|
 *           'UIClick'|'UIHover'|'MissionComplete'|'MissionFail'|
 *           'BuildingPlaced'|'EngineIdle'} AudioEventType
 */

/**
 * Procedural placeholder sound generator using the Web Audio API.
 * Mirrors PlaceholderSoundGenerator.cs logic.
 */
class WebAudioPlaceholderGenerator {
    /**
     * @param {AudioContext} ctx
     */
    constructor(ctx) {
        this._ctx = ctx;
    }

    /** Generate a sine-wave tone buffer. */
    tone(frequency, duration, attack = 0.01, release = 0.05) {
        const sr = this._ctx.sampleRate;
        const buf = this._ctx.createBuffer(1, Math.floor(sr * duration), sr);
        const data = buf.getChannelData(0);
        const attackSamples  = Math.floor(sr * attack);
        const releaseSamples = Math.floor(sr * release);
        for (let i = 0; i < data.length; i++) {
            const t = i / sr;
            let env;
            if (i < attackSamples)             env = i / attackSamples;
            else if (i >= data.length - releaseSamples) env = (data.length - i) / releaseSamples;
            else                               env = 1;
            data[i] = Math.sin(2 * Math.PI * frequency * t) * env * 0.7;
        }
        return buf;
    }

    /** Generate a frequency-swept tone (laser effect). */
    sweep(startHz, endHz, duration, release = 0.03) {
        const sr = this._ctx.sampleRate;
        const buf = this._ctx.createBuffer(1, Math.floor(sr * duration), sr);
        const data = buf.getChannelData(0);
        const releaseSamples = Math.floor(sr * release);
        let phase = 0;
        for (let i = 0; i < data.length; i++) {
            const t    = i / data.length;
            const freq = startHz + (endHz - startHz) * t;
            phase += 2 * Math.PI * freq / sr;
            const env = i >= data.length - releaseSamples ? (data.length - i) / releaseSamples : 1;
            data[i] = Math.sin(phase) * env * 0.6;
        }
        return buf;
    }

    /** Generate white noise (explosion / static). */
    noise(duration, release = 0.15) {
        const sr = this._ctx.sampleRate;
        const buf = this._ctx.createBuffer(1, Math.floor(sr * duration), sr);
        const data = buf.getChannelData(0);
        const releaseSamples = Math.floor(sr * release);
        for (let i = 0; i < data.length; i++) {
            const env = i >= data.length - releaseSamples ? (data.length - i) / releaseSamples : 1;
            data[i] = (Math.random() * 2 - 1) * env * 0.5;
        }
        return buf;
    }

    /** @param {AudioEventType} type @returns {AudioBuffer} */
    getPlaceholder(type) {
        switch (type) {
            case 'WeaponFire':        return this.sweep(800, 200, 0.12);
            case 'WeaponLaunch':      return this.sweep(400, 150, 0.18);
            case 'Explosion':         return this.noise(0.45, 0.20);
            case 'ShieldHit':         return this.sweep(1200, 600, 0.08);
            case 'UnitMoveAck':       return this.tone(660, 0.05);
            case 'UnitAttackAck':     return this.tone(440, 0.05);
            case 'ResourceCollected': return this.sweep(400, 800, 0.10);
            case 'UIClick':           return this.tone(1000, 0.04, 0.005, 0.02);
            case 'UIHover':           return this.tone(800, 0.03, 0.005, 0.015);
            case 'MissionComplete':   return this.tone(523, 0.6, 0.02, 0.3);
            case 'MissionFail':       return this.sweep(400, 100, 0.8, 0.4);
            case 'BuildingPlaced':    return this.tone(330, 0.08);
            case 'EngineIdle':        return this.noise(0.5, 0.05);
            default:                  return this.tone(440, 0.05);
        }
    }
}

/**
 * WebAudio-backed audio manager for the browser build.
 * Mirrors the C# IAudioManager interface.
 *
 * Usage:
 *   const audio = new WebAudioManager();
 *   audio.playSound('UIClick');
 *   audio.setMasterVolume(0.8);
 */
class WebAudioManager {
    constructor() {
        /** @type {AudioContext|null} */
        this._ctx = null;
        /** @type {GainNode|null} */
        this._masterGain = null;
        /** @type {GainNode|null} */
        this._sfxGain = null;
        /** @type {GainNode|null} */
        this._musicGain = null;
        /** @type {AudioBufferSourceNode|null} */
        this._musicSource = null;
        /** @type {Map<AudioEventType, AudioBuffer>} */
        this._buffers = new Map();
        /** @type {WebAudioPlaceholderGenerator|null} */
        this._gen = null;

        this.settings = {
            masterVolume: 1.0,
            sfxVolume:    1.0,
            musicVolume:  0.7,
        };
    }

    /** Lazily initialise AudioContext on first user interaction (browser policy). */
    _ensureContext() {
        if (this._ctx) return;
        this._ctx = new (window.AudioContext || window.webkitAudioContext)();
        this._masterGain = this._ctx.createGain();
        this._sfxGain    = this._ctx.createGain();
        this._musicGain  = this._ctx.createGain();
        this._sfxGain.connect(this._masterGain);
        this._musicGain.connect(this._masterGain);
        this._masterGain.connect(this._ctx.destination);
        this._applyGains();
        this._gen = new WebAudioPlaceholderGenerator(this._ctx);
    }

    _applyGains() {
        if (!this._masterGain) return;
        this._masterGain.gain.value = this.settings.masterVolume;
        this._sfxGain.gain.value    = this.settings.sfxVolume;
        this._musicGain.gain.value  = this.settings.musicVolume;
    }

    /**
     * Play a one-shot sound effect.
     * @param {AudioEventType} eventType
     * @param {{x:number,y:number,z:number}} [position] world position (for panning)
     */
    playSound(eventType, position = { x: 0, y: 0, z: 0 }) {
        this._ensureContext();
        if (!this._buffers.has(eventType))
            this._buffers.set(eventType, this._gen.getPlaceholder(eventType));
        const src = this._ctx.createBufferSource();
        src.buffer = this._buffers.get(eventType);
        src.connect(this._sfxGain);
        src.start();
    }

    /**
     * Start background music.
     * @param {string} trackId  (placeholder: ignored, plays a tone)
     * @param {boolean} [loop]
     * @param {number} [crossfadeSeconds]
     */
    playMusic(trackId, loop = true, crossfadeSeconds = 1.0) {
        this._ensureContext();
        this.stopMusic(crossfadeSeconds);
        const buf = this._gen.tone(110, 4.0, 0.5, 0.5);
        this._musicSource = this._ctx.createBufferSource();
        this._musicSource.buffer = buf;
        this._musicSource.loop   = loop;
        this._musicSource.connect(this._musicGain);
        this._musicSource.start();
    }

    /**
     * Stop background music.
     * @param {number} [fadeOutSeconds]
     */
    stopMusic(fadeOutSeconds = 1.0) {
        if (!this._musicSource) return;
        const gain = this._musicGain.gain;
        gain.setValueAtTime(gain.value, this._ctx.currentTime);
        gain.linearRampToValueAtTime(0, this._ctx.currentTime + fadeOutSeconds);
        const src = this._musicSource;
        setTimeout(() => { try { src.stop(); } catch(_) {} }, fadeOutSeconds * 1000 + 50);
        this._musicSource = null;
    }

    /** @param {number} volume 0–1 */
    setMasterVolume(volume) {
        this.settings.masterVolume = Math.max(0, Math.min(1, volume));
        this._applyGains();
    }

    /** @param {number} volume 0–1 */
    setSfxVolume(volume) {
        this.settings.sfxVolume = Math.max(0, Math.min(1, volume));
        this._applyGains();
    }

    /** @param {number} volume 0–1 */
    setMusicVolume(volume) {
        this.settings.musicVolume = Math.max(0, Math.min(1, volume));
        this._applyGains();
    }

    /** Resume suspended context (call after user gesture). */
    resume() {
        if (this._ctx && this._ctx.state === 'suspended')
            this._ctx.resume();
    }
}

// Expose globally for the browser build
if (typeof window !== 'undefined') {
    window.WebAudioManager = WebAudioManager;
}
