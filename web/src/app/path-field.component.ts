import { Component, DestroyRef, ElementRef, afterNextRender, effect, inject, input, signal, viewChild } from '@angular/core';
import { reducedMotion } from './models';

type ThemeMode = 'light' | 'dark';

@Component({
  selector: 'app-path-field',
  standalone: true,
  template: `
    <section class="field">
      <h2>Path field</h2>
      <p>{{ webgl() ? 'Zero-drift samples. Drag to orbit.' : 'A still of the same field.' }}</p>
      <div class="viewport" #viewport>
        @if (!webgl()) {
          <svg viewBox="0 0 640 280" aria-hidden="true">
            <path class="bell" d="M70 220 C 160 220 210 70 320 70 C 430 70 480 220 570 220 Z" />
            <path d="M48 190 C 140 188 180 120 250 132 S 390 210 470 150 S 560 96 600 140" />
            <path d="M48 168 C 150 150 200 90 280 110 S 410 176 500 128 S 570 80 604 112" />
            <path d="M48 210 C 130 214 190 160 270 168 S 400 230 490 188 S 560 150 602 176" />
            <path d="M48 146 C 160 130 210 70 300 96 S 430 150 520 108 S 575 72 608 96" />
          </svg>
        }
      </div>
    </section>
  `,
  styles: `
    :host { display: block; }
    .field h2 {
      margin: 0 0 8px;
      font-size: 0.75rem;
      letter-spacing: 0.12em;
      text-transform: uppercase;
      color: var(--muted);
    }
    .field p { margin: 0 0 12px; color: var(--muted); font-size: 0.88rem; }
    .viewport {
      position: relative;
      height: 280px;
      border: 1px solid var(--line);
      border-radius: 8px;
      background: var(--paper-2);
      overflow: hidden;
    }
    svg { width: 100%; height: 100%; display: block; }
    .bell { fill: var(--signal-soft); stroke: none; }
    path { fill: none; stroke: var(--signal); stroke-width: 1.5; }
    :host ::ng-deep canvas { display: block; width: 100%; height: 100%; }
  `
})
export class PathFieldComponent {
  readonly theme = input.required<ThemeMode>();
  readonly webgl = signal(false);
  private readonly viewport = viewChild<ElementRef<HTMLElement>>('viewport');
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
      if (reducedMotion() || dead) return;
      const el = this.viewport()?.nativeElement;
      if (!el) return;
      let started = false;
      const observer = new IntersectionObserver((entries) => {
        if (started || dead || !entries.some((entry) => entry.isIntersecting)) return;
        started = true;
        observer.disconnect();
        void mountField(el, this.theme(), (apply) => {
          this.paint = apply;
        }).then((dispose) => {
          if (dead) {
            dispose();
            return;
          }
          stop.current = dispose;
          this.webgl.set(true);
        }).catch(() => {
          if (!dead) this.webgl.set(false);
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
  bind: (apply: (mode: ThemeMode) => void) => void
): Promise<() => void> {
  const THREE = await import('three');
  const { OrbitControls } = await import('three/addons/controls/OrbitControls.js');
  const width = host.clientWidth || 640;
  const height = host.clientHeight || 280;

  const scene = new THREE.Scene();
  const camera = new THREE.PerspectiveCamera(38, width / height, 0.1, 40);
  camera.position.set(3.4, 2.15, 3.6);

  const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: false });
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
  renderer.setSize(width, height, false);
  renderer.outputColorSpace = THREE.SRGBColorSpace;
  host.appendChild(renderer.domElement);

  const surface = buildSurface(THREE);
  const material = new THREE.MeshPhysicalMaterial({
    color: initial === 'dark' ? 0xccff00 : 0x4f7a00,
    roughness: 0.38,
    metalness: 0.04,
    clearcoat: 0.45,
    clearcoatRoughness: 0.35,
    side: THREE.DoubleSide
  });
  const mesh = new THREE.Mesh(surface, material);
  scene.add(mesh);

  const paths = buildPaths(THREE, initial);
  scene.add(paths);

  scene.add(new THREE.AmbientLight(0xffffff, 0.72));
  const key = new THREE.DirectionalLight(0xffffff, 1.35);
  key.position.set(3, 6, 4);
  scene.add(key);
  const fill = new THREE.DirectionalLight(0xccff00, 0.28);
  fill.position.set(-4, 2, -2);
  scene.add(fill);

  const controls = new OrbitControls(camera, renderer.domElement);
  controls.target.set(0, 0.35, 0);
  controls.enablePan = false;
  controls.enableDamping = true;
  controls.dampingFactor = 0.08;
  controls.autoRotate = true;
  controls.autoRotateSpeed = 0.45;
  controls.minDistance = 2.4;
  controls.maxDistance = 8;
  controls.update();

  const pauseSpin = () => { controls.autoRotate = false; };
  const resumeSpin = () => { controls.autoRotate = true; };
  renderer.domElement.addEventListener('pointerdown', pauseSpin);
  renderer.domElement.addEventListener('pointerup', resumeSpin);
  renderer.domElement.addEventListener('pointerleave', resumeSpin);

  const paint = (mode: ThemeMode) => {
    material.color.set(mode === 'dark' ? 0xccff00 : 0x4f7a00);
    renderer.setClearColor(mode === 'dark' ? 0x191919 : 0xffffff, 1);
    paths.traverse((child: { material?: { color?: { set: (hex: number) => void } } }) => {
      child.material?.color?.set(mode === 'dark' ? 0xccff00 : 0x4f7a00);
    });
  };
  bind(paint);
  paint(initial);

  let visible = true;
  let disposed = false;
  let looping = false;
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
    renderer.setSize(w, h, false);
  };
  const ro = new ResizeObserver(resize);
  ro.observe(host);

  const kick = () => {
    if (looping || disposed || !visible) return;
    looping = true;
    const loop = () => {
      if (disposed || !visible) {
        looping = false;
        return;
      }
      controls.update();
      renderer.render(scene, camera);
      window.requestAnimationFrame(loop);
    };
    window.requestAnimationFrame(loop);
  };
  kick();

  return () => {
    disposed = true;
    io.disconnect();
    ro.disconnect();
    renderer.domElement.removeEventListener('pointerdown', pauseSpin);
    renderer.domElement.removeEventListener('pointerup', resumeSpin);
    renderer.domElement.removeEventListener('pointerleave', resumeSpin);
    controls.dispose();
    surface.dispose();
    material.dispose();
    paths.traverse((child: { geometry?: { dispose: () => void }; material?: { dispose: () => void } }) => {
      child.geometry?.dispose();
      child.material?.dispose();
    });
    renderer.dispose();
    renderer.domElement.remove();
  };
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
  return geometry;
}

function buildPaths(THREE: any, mode: ThemeMode): any {
  const group = new THREE.Group();
  const rand = mulberry32(20261008);
  const color = mode === 'dark' ? 0xccff00 : 0x3f6200;
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
    group.add(new THREE.Line(geometry, new THREE.LineBasicMaterial({ color })));
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
