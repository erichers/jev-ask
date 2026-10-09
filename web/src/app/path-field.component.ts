import { Component, DestroyRef, ElementRef, afterNextRender, computed, effect, inject, input, signal, viewChild } from '@angular/core';
import { reducedMotion } from './models';
import { fitResting } from './camera-fit';
import { installAccentMask } from './accent-mask';
import { AdaptiveQuality, webglAvailable } from './quality';
import { paintStill } from './scene-still';

type ThemeMode = 'light' | 'dark';

@Component({
  selector: 'app-path-field',
  standalone: true,
  template: `
    <section class="field">
      <h2>Path field</h2>
      <p>{{ still() ? 'A still of the same field.' : 'Zero-drift samples. Lime marks the area past the target. Press Rotate, then drag to turn it.' }}</p>
      @if (webgl()) {
        <div class="bar">
          <button type="button" class="orbit-toggle" [attr.aria-pressed]="orbit()" (click)="toggleOrbit()">{{ orbit() ? 'Done' : 'Rotate' }}</button>
        </div>
      }
      <div class="viewport" [class.is-orbit]="orbit()" [class.is-fallback]="still()" #viewport>
        <svg #stillSvg class="still" [class.on]="still()" role="img" aria-label="Still of the path field from the resting camera" [attr.aria-hidden]="still() ? null : 'true'"></svg>
      </div>
    </section>
  `,
  styles: `
    :host { display: block; }
    .field h2 {
      margin: 0 0 var(--s-2);
      font-size: 14px;
      font-weight: 600;
      letter-spacing: 0.04em;
      text-transform: uppercase;
      color: var(--muted);
    }
    .field p { margin: 0 0 var(--s-3); color: var(--muted); font-size: 14px; }
    .viewport {
      position: relative;
      grid-area: view;
      height: 280px;
      border: 1px solid var(--line);
      border-radius: var(--r-lg);
      background: var(--paper-2);
      overflow: hidden;
    }
    /* The still is the same scene from the same camera (scene-still.ts paints it with presentation attributes). */
    .still { position: absolute; inset: 0; width: 100%; height: 100%; display: block; visibility: hidden; pointer-events: none; }
    .still.on { visibility: visible; }
    .viewport.is-fallback ::ng-deep canvas { visibility: hidden; }
    :host ::ng-deep canvas { display: block; width: 100%; height: 100%; touch-action: pan-y; }
    .viewport.is-orbit ::ng-deep canvas { touch-action: none; cursor: grab; }
    .bar { grid-area: bar; display: flex; justify-content: flex-end; margin: 0 0 var(--s-3); }
    /* From 600px the frame is 8:5, close to the shape of the drawing from the resting camera. */
    @media (min-width: 600px) {
      .viewport { height: auto; aspect-ratio: 8 / 5; }
    }
    /* Wide: title and caption on the left, the field on the right, so the frame keeps the drawing's shape. */
    @media (min-width: 860px) {
      .field {
        display: grid;
        grid-template-columns: minmax(0, 240px) minmax(0, 1fr);
        grid-template-areas: 'title bar' 'copy view';
        grid-template-rows: auto 1fr;
        column-gap: var(--s-7);
        align-items: start;
      }
      .field h2 { grid-area: title; }
      .field p { grid-area: copy; }
      .bar { justify-self: end; }
    }
    /* v2.3: Rotate is a soft 44 pill; pressed = ink thumb. The 3D frame is 16 and clips the square canvas. */
    .orbit-toggle {
      position: relative;
      min-width: 88px;
      height: 44px;
      padding: 0 24px;
      border: 0;
      border-radius: var(--r-full);
      background: var(--btn-soft-bg);
      color: var(--ink);
      font: 500 14px/1 var(--sans);
      cursor: pointer;
      transition: background-color var(--dur-xs) var(--ease-standard), transform var(--dur-xs) var(--ease-standard);
    }
    .orbit-toggle[aria-pressed="true"] { background: var(--ink); color: var(--paper-2); }
    @media (hover: hover) and (pointer: fine) {
      .orbit-toggle:hover { background: var(--btn-soft-bg-hover); }
      .orbit-toggle[aria-pressed="true"]:hover { background: var(--btn-primary-bg-hover); }
    }
    .orbit-toggle:active { transform: scale(0.98); }
    .orbit-toggle:focus-visible { outline: 2px solid var(--ink); outline-offset: 2px; }
    @media (prefers-reduced-motion: reduce) { .orbit-toggle:active { transform: none; } }
  `
})
export class PathFieldComponent {
  readonly theme = input.required<ThemeMode>();
  /** WebGL is up. With reduced motion it is the same scene at its resting camera, drawn only when something changes. */
  readonly webgl = signal(false);
  /** WebGL could not start: the still of the same scene shows in the same frame. */
  readonly fallback = signal(false);
  /** The WebGL context was lost: the still shows until webglcontextrestored. */
  readonly lost = signal(false);
  readonly still = computed(() => this.fallback() || this.lost());
  /** Drag-to-rotate stays off until Rotate is pressed, so a swipe over the field scrolls the page. */
  readonly orbit = signal(false);
  private setOrbit: ((on: boolean) => void) | null = null;

  toggleOrbit(): void {
    const next = !this.orbit();
    this.orbit.set(next);
    this.setOrbit?.(next);
  }
  private readonly viewport = viewChild<ElementRef<HTMLElement>>('viewport');
  private readonly stillSvg = viewChild<ElementRef<SVGSVGElement>>('stillSvg');
  private paint: ((mode: ThemeMode) => void) | null = null;

  constructor() {
    effect(() => {
      const mode = this.theme();
      this.paint?.(mode);
    });

    const stop: { current: () => void } = { current: () => undefined };
    let dead = false;
    inject(DestroyRef).onDestroy(() => {
      dead = true;
      stop.current();
    });
    afterNextRender(() => {
      if (dead) return;
      const spin = !reducedMotion();
      const el = this.viewport()?.nativeElement;
      const svg = this.stillSvg()?.nativeElement;
      if (!el || !svg) return;
      let started = false;
      const observer = new IntersectionObserver((entries) => {
        if (started || dead || !entries.some((entry) => entry.isIntersecting)) return;
        started = true;
        observer.disconnect();
        void mountField(el, this.theme(), (apply) => {
          this.paint = apply;
        }, (orbit) => {
          this.setOrbit = orbit;
        }, spin, svg, (state) => {
          if (dead) return;
          if (state !== 'live') {
            this.orbit.set(false);
            this.setOrbit?.(false);
          }
          this.webgl.set(state === 'live');
          this.lost.set(state === 'lost');
          this.fallback.set(state === 'fallback');
        }).then((dispose) => {
          if (dead) {
            dispose();
            return;
          }
          stop.current = dispose;
        }).catch(() => {
          if (!dead) {
            this.webgl.set(false);
            this.fallback.set(true);
          }
        });
      }, { threshold: 0.2 });
      observer.observe(el);
      stop.current = () => observer.disconnect();
    });
  }
}

async function mountField(
  host: HTMLElement,
  initial: ThemeMode,
  bind: (apply: (mode: ThemeMode) => void) => void,
  bindOrbit: (orbit: (on: boolean) => void) => void,
  spin: boolean,
  still: SVGSVGElement,
  onState: (state: 'live' | 'lost' | 'fallback') => void
): Promise<() => void> {
  const THREE = await import('three');
  const { OrbitControls } = await import('three/addons/controls/OrbitControls.js');
  const width = host.clientWidth || 640;
  const height = host.clientHeight || 280;

  const scene = new THREE.Scene();
  const camera = new THREE.PerspectiveCamera(38, width / height, 0.1, 40);
  camera.position.set(4.1, 2.5, 4.35);

  const surface = buildSurface(THREE);
  // Neutral surface; vertex colors put lime only on the band past the target side.
  const material = new THREE.MeshPhysicalMaterial({
    color: 0xffffff,
    vertexColors: true,
    roughness: 0.38,
    metalness: 0.04,
    clearcoat: 0.45,
    clearcoatRoughness: 0.35,
    side: THREE.DoubleSide
  });
  // the lime band past the target is vertex color on this material; neutral pixels are achromatic
  material.userData['token'] = 'accent';
  const mesh = new THREE.Mesh(surface, material);
  scene.add(mesh);

  const paths = buildPaths(THREE, initial);
  scene.add(paths);

  scene.add(new THREE.AmbientLight(0xffffff, 0.72));
  const key = new THREE.DirectionalLight(0xffffff, 1.35);
  key.position.set(3, 6, 4);
  scene.add(key);
  const fill = new THREE.DirectionalLight(0xffffff, 0.22);
  fill.position.set(-4, 2, -2);
  scene.add(fill);

  // Resting camera: the original diagonal view; elevation and distance are fitted so the field fills the frame
  // (higher on a tall phone frame, lower on the 8:5 frame). The live view starts here; the reduced-motion still stays here.
  const fitPoints: any[] = [];
  const fitPos = surface.getAttribute('position');
  for (let i = 0; i < fitPos.count; i++) {
    fitPoints.push(new THREE.Vector3(fitPos.getX(i), fitPos.getY(i), fitPos.getZ(i)));
    fitPoints.push(new THREE.Vector3(fitPos.getX(i), 0, fitPos.getZ(i)));
  }
  paths.traverse((child: any) => {
    const p = child.geometry?.getAttribute('position');
    if (!p) return;
    for (let i = 0; i < p.count; i += 2) fitPoints.push(new THREE.Vector3(p.getX(i), p.getY(i), p.getZ(i)));
  });
  const restTarget = new THREE.Vector3(0, 0.2, 0);
  const restAzimuth = Math.atan2(4.1, 4.35);

  let mode = initial;
  const paintScene = (next: ThemeMode) => {
    mode = next;
    paintSurface(THREE, surface, next);
    paths.traverse((child: { userData?: { past?: boolean }; material?: { color?: { set: (hex: number) => void } } }) => {
      const past = !!child.userData?.past;
      child.material?.color?.set(past ? (next === 'dark' ? 0xcefb31 : 0x4f7a00) : (next === 'dark' ? 0x8a8a88 : 0x6b6b6b));
    });
  };
  const clear = () => (mode === 'dark' ? '#191919' : '#ffffff');
  /** The still: the same scene through the resting camera fitted to this frame, on the same clear color. */
  const paintFallback = () => {
    const w = host.clientWidth || 640;
    const h = host.clientHeight || 280;
    camera.aspect = w / h;
    camera.updateProjectionMatrix();
    const target = restTarget.clone();
    fitResting(camera, target, fitPoints, { azimuth: restAzimuth, minEl: 15, maxEl: 62, margin: 0.94 });
    paintStill(THREE, still, scene, camera, w, h, { background: clear() });
  };
  const disposeScene = () => {
    surface.dispose();
    material.dispose();
    paths.traverse((child: { geometry?: { dispose: () => void }; material?: { dispose: () => void } }) => {
      child.geometry?.dispose();
      child.material?.dispose();
    });
  };

  let renderer: any = null;
  try {
    if (webglAvailable()) {
      renderer = new THREE.WebGLRenderer({ antialias: true, alpha: false });
      if (!renderer.getContext()) {
        renderer.dispose();
        renderer = null;
      }
    }
  } catch {
    renderer = null;
  }
  if (!renderer) {
    // WebGL could not start: the still of the same scene, repainted on resize and theme change.
    paintScene(initial);
    bind((next) => {
      paintScene(next);
      paintFallback();
    });
    paintFallback();
    onState('fallback');
    const ro = new ResizeObserver(() => paintFallback());
    ro.observe(host);
    return () => {
      ro.disconnect();
      disposeScene();
    };
  }
  const gl = renderer;
  const quality = new AdaptiveQuality(host, (ratio) => {
    gl.setPixelRatio(ratio);
    gl.setSize(host.clientWidth || 640, host.clientHeight || 280, false);
    dirty = true;
    kick();
  });
  gl.setPixelRatio(quality.ratio());
  gl.setSize(width, height, false);
  gl.outputColorSpace = THREE.SRGBColorSpace;
  gl.domElement.dataset['engine'] = 'three';
  host.appendChild(gl.domElement);

  let visible = true;
  let disposed = false;
  let looping = false;
  let lost = false;

  installAccentMask(THREE, gl, scene, camera);
  const controls = new OrbitControls(camera, gl.domElement);
  controls.target.copy(restTarget);
  controls.enablePan = false;
  controls.enableDamping = true;
  controls.dampingFactor = 0.08;
  // MOTION 3.8: never auto-rotate on load; the slow turn (motion on) runs only while Rotate is pressed.
  controls.autoRotate = false;
  controls.autoRotateSpeed = 0.45;
  controls.minDistance = 2.4;
  controls.maxDistance = 8;
  controls.enabled = false;
  controls.update();
  gl.domElement.style.touchAction = 'pan-y';
  let orbiting = false;
  let dirty = true;
  controls.addEventListener('change', () => { dirty = true; kick(); });
  bindOrbit((on) => {
    orbiting = on;
    controls.enabled = on && !lost;
    controls.autoRotate = spin && on && !lost;
    gl.domElement.style.touchAction = on ? 'none' : 'pan-y';
    if (!on) rest();
    dirty = true;
    kick();
  });

  const rest = () => {
    if (orbiting) return;
    controls.target.copy(restTarget);
    fitResting(camera, controls.target, fitPoints, { azimuth: restAzimuth, minEl: 15, maxEl: 62, margin: 0.94 });
    // keep the zoom range around the fitted distance so the controls do not clamp the resting camera
    const fitted = camera.position.distanceTo(controls.target);
    controls.maxDistance = Math.max(8, fitted * 1.6);
    controls.minDistance = Math.min(2.4, fitted * 0.5);
    controls.update();
  };

  const pauseSpin = () => { controls.autoRotate = false; };
  const resumeSpin = () => { controls.autoRotate = spin && orbiting && !lost; };
  gl.domElement.addEventListener('pointerdown', pauseSpin);
  gl.domElement.addEventListener('pointerup', resumeSpin);
  gl.domElement.addEventListener('pointerleave', resumeSpin);

  const paint = (next: ThemeMode) => {
    paintScene(next);
    gl.setClearColor(next === 'dark' ? 0x191919 : 0xffffff, 1);
    if (lost) paintFallback();
    dirty = true;
    kick();
  };
  bind(paint);
  paint(initial);

  // Context loss (iOS drops contexts in background tabs): the still of the same scene, Rotate hidden; back on restore.
  const onLost = (event: Event) => {
    event.preventDefault();
    lost = true;
    orbiting = false;
    controls.enabled = false;
    controls.autoRotate = false;
    gl.domElement.style.touchAction = 'pan-y';
    paintFallback();
    onState('lost');
  };
  const onRestored = () => {
    lost = false;
    paintScene(mode);
    gl.setClearColor(mode === 'dark' ? 0x191919 : 0xffffff, 1);
    resize();
    onState('live');
  };
  gl.domElement.addEventListener('webglcontextlost', onLost);
  gl.domElement.addEventListener('webglcontextrestored', onRestored);

  const io = new IntersectionObserver((entries) => {
    visible = entries.some((entry) => entry.isIntersecting);
    if (visible) kick();
  });
  io.observe(host);

  const resize = () => {
    const w = host.clientWidth || 640;
    const h = host.clientHeight || 280;
    camera.aspect = w / h;
    camera.updateProjectionMatrix();
    gl.setPixelRatio(quality.ratio());
    gl.setSize(w, h, false);
    if (lost) {
      paintFallback();
      return;
    }
    rest();
    dirty = true;
    kick();
  };
  resize();
  quality.push();
  const ro = new ResizeObserver(resize);
  ro.observe(host);

  function kick(): void {
    if (looping || disposed || !visible || lost) return;
    looping = true;
    const loop = (now: number) => {
      if (disposed || !visible || lost) {
        looping = false;
        quality.idle();
        return;
      }
      const moving = controls.update();
      if (moving) dirty = true;
      if (controls.autoRotate || dirty) {
        dirty = false;
        gl.render(scene, camera);
        host.dataset['3d'] = orbiting ? 'orbit' : 'settled';
        if (controls.autoRotate || moving) quality.frame(now);
      }
      // Render on demand in both modes: stop once the view is still (a drag or the Rotate turn restarts it).
      if (!controls.autoRotate && !dirty && !moving) {
        looping = false;
        quality.idle();
        return;
      }
      window.requestAnimationFrame(loop);
    };
    window.requestAnimationFrame(loop);
  }
  kick();
  onState('live');

  return () => {
    disposed = true;
    io.disconnect();
    ro.disconnect();
    gl.domElement.removeEventListener('pointerdown', pauseSpin);
    gl.domElement.removeEventListener('pointerup', resumeSpin);
    gl.domElement.removeEventListener('pointerleave', resumeSpin);
    gl.domElement.removeEventListener('webglcontextlost', onLost);
    gl.domElement.removeEventListener('webglcontextrestored', onRestored);
    controls.dispose();
    disposeScene();
    gl.dispose();
    gl.domElement.remove();
  };
}

/** Where "past the target" starts on the field's spread axis (-1..1). */
const PAST = 0.7;

function paintSurface(THREE: any, geometry: any, mode: ThemeMode): void {
  const pos = geometry.getAttribute('position');
  const base = new THREE.Color(mode === 'dark' ? 0x4a4a47 : 0xd9d9d4);
  const lime = new THREE.Color(mode === 'dark' ? 0xcefb31 : 0x4f7a00);
  const colors = new Float32Array(pos.count * 3);
  for (let i = 0; i < pos.count; i++) {
    const c = pos.getZ(i) / 1.8 > PAST ? lime : base;
    colors[i * 3] = c.r;
    colors[i * 3 + 1] = c.g;
    colors[i * 3 + 2] = c.b;
  }
  geometry.setAttribute('color', new THREE.BufferAttribute(colors, 3));
}

function buildSurface(THREE: any): any {
  const xCount = 42;
  const zCount = 26;
  const row = zCount + 1;
  const positions = new Float32Array((xCount + 1) * row * 3);
  let p = 0;
  for (let i = 0; i <= xCount; i++) {
    const t = i / xCount;
    const sigma = 0.14 + 0.62 * Math.sqrt(t);
    for (let j = 0; j <= zCount; j++) {
      const m = (j / zCount) * 2 - 1;
      const density = Math.exp(-0.5 * (m / sigma) * (m / sigma)) / (sigma * 3.6);
      positions[p++] = (t - 0.5) * 4.6;
      positions[p++] = density;
      positions[p++] = m * 1.8;
    }
  }
  const indices: number[] = [];
  for (let i = 0; i < xCount; i++) {
    for (let j = 0; j < zCount; j++) {
      const a = i * row + j;
      indices.push(a, a + 1, a + row, a + 1, a + row + 1, a + row);
    }
  }
  const geometry = new THREE.BufferGeometry();
  geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
  geometry.setIndex(indices);
  geometry.computeVertexNormals();
  geometry.userData.stillQuads = true; // the still shades each quad as one color (no teeth on the band edge)
  return geometry;
}

function buildPaths(THREE: any, mode: ThemeMode): any {
  const group = new THREE.Group();
  const rand = mulberry32(20261008);
  const color = mode === 'dark' ? 0xcefb31 : 0x3f6200;
  for (let k = 0; k < 12; k++) {
    const points = [];
    let log = (rand() - 0.5) * 0.2;
    for (let i = 0; i <= 36; i++) {
      const t = i / 36;
      log += (rand() - 0.48) * 0.11;
      const m = Math.max(-1, Math.min(1, log));
      const sigma = 0.14 + 0.62 * Math.sqrt(t);
      const density = Math.exp(-0.5 * (m / sigma) * (m / sigma)) / (sigma * 3.6);
      points.push(new THREE.Vector3((t - 0.5) * 4.6, density + 0.04, m * 1.8));
    }
    const geometry = new THREE.BufferGeometry().setFromPoints(points);
    const line = new THREE.Line(geometry, new THREE.LineBasicMaterial({ color }));
    line.userData = { past: points[points.length - 1].z > PAST * 1.8 };
    if (line.userData['past']) line.material.userData['token'] = 'accent';
    group.add(line);
  }
  return group;
}

function mulberry32(seed: number): () => number {
  let a = seed;
  return () => {
    a |= 0;
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}
