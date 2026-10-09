import { reducedMotion } from './models';

let settled: Promise<void> = Promise.resolve();

export function holdForRoute(done: Promise<void>): void {
  settled = done.then(() => undefined, () => undefined);
}

export function whenSettled(): Promise<void> {
  return settled;
}

export function easeInOut(t: number): number {
  const x = Math.min(1, Math.max(0, t));
  return bezier(x, 0.2, 0.7, 0.2, 1);
}

export function countTo(target: number, apply: (value: number) => void, duration = 640): () => void {
  let frame = 0;
  let dead = false;
  const run = () => {
    if (dead || reducedMotion()) {
      apply(target);
      return;
    }
    const start = performance.now();
    const tick = (now: number) => {
      if (dead) return;
      const t = Math.min(1, (now - start) / duration);
      apply(target * easeInOut(t));
      if (t < 1) frame = window.requestAnimationFrame(tick);
    };
    apply(0);
    frame = window.requestAnimationFrame(tick);
  };
  void whenSettled().then(run);
  return () => {
    dead = true;
    window.cancelAnimationFrame(frame);
  };
}

export function onVisible(el: Element, enter: () => void): () => void {
  if (reducedMotion() || typeof IntersectionObserver === 'undefined') {
    enter();
    return () => undefined;
  }
  const observer = new IntersectionObserver((entries) => {
    if (entries.some((entry) => entry.isIntersecting)) {
      enter();
      observer.disconnect();
    }
  }, { threshold: 0.35 });
  observer.observe(el);
  return () => observer.disconnect();
}

function bezier(t: number, x1: number, y1: number, x2: number, y2: number): number {
  let lo = 0;
  let hi = 1;
  for (let i = 0; i < 16; i++) {
    const mid = (lo + hi) / 2;
    const x = sample(mid, x1, x2);
    if (x < t) lo = mid;
    else hi = mid;
  }
  return sample((lo + hi) / 2, y1, y2);
}

function sample(t: number, p1: number, p2: number): number {
  const u = 1 - t;
  return 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t;
}
