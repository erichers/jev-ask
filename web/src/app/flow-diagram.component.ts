import { Component, DestroyRef, ElementRef, afterNextRender, inject, input, signal, viewChild } from '@angular/core';
import { reducedMotion } from './models';
import { onVisible, whenSettled } from './motion';

@Component({
  selector: 'app-flow',
  standalone: true,
  template: `
    <figure class="flow" #host>
      <svg [class.go]="go()" viewBox="0 0 720 96" role="img" [attr.aria-label]="note()">
        <path class="link" [attr.d]="link()" />
        @for (node of nodes(); track node.label) {
          <g [attr.transform]="'translate(' + node.x + ' 36)'">
            @if (node.last) {
              <circle class="track" r="16" />
              <circle class="arc" r="16" [attr.stroke-dasharray]="circ" [attr.stroke-dashoffset]="offset()" />
            } @else {
              <circle class="dot" r="4.5" />
            }
            <text y="34">{{ node.label }}</text>
          </g>
        }
      </svg>
      <figcaption>{{ note() }}</figcaption>
    </figure>
  `,
  styles: `
    :host { display: block; }
    .flow { margin: 0; }
    svg { width: 100%; height: auto; display: block; overflow: visible; }
    .link {
      fill: none;
      stroke: var(--signal);
      stroke-width: 1.5;
      stroke-dasharray: 640;
      stroke-dashoffset: 640;
    }
    .go .link { stroke-dashoffset: 0; transition: stroke-dashoffset 640ms var(--ease); }
    .dot { fill: var(--signal); }
    .track { fill: none; stroke: var(--line); stroke-width: 1.5; }
    .arc {
      fill: none;
      stroke: var(--signal);
      stroke-width: 2;
      transform: rotate(-90deg);
      transform-origin: center;
      transform-box: fill-box;
      transition: stroke-dashoffset 640ms var(--ease);
    }
    text {
      fill: var(--muted);
      font-size: 13px;
      letter-spacing: 0.08em;
      text-anchor: middle;
      text-transform: uppercase;
    }
    figcaption { margin: 8px 0 0; color: var(--muted); font-size: 0.88rem; max-width: 40rem; }
  `
})
export class FlowDiagramComponent {
  readonly steps = input.required<string[]>();
  readonly value = input(0);
  readonly note = input('');
  readonly go = signal(reducedMotion());
  readonly circ = 2 * Math.PI * 16;
  private readonly host = viewChild<ElementRef<HTMLElement>>('host');

  constructor() {
    const stop: { current: () => void } = { current: () => undefined };
    inject(DestroyRef).onDestroy(() => stop.current());
    afterNextRender(() => {
      const el = this.host()?.nativeElement;
      if (!el) return;
      if (reducedMotion()) {
        this.go.set(true);
        return;
      }
      stop.current = onVisible(el, () => {
        void whenSettled().then(() => this.go.set(true));
      });
    });
  }

  nodes(): { x: number; label: string; last: boolean }[] {
    const labels = this.steps();
    const last = labels.length - 1;
    return labels.map((label, index) => ({
      x: last <= 0 ? 360 : 56 + (640 * index) / last,
      label,
      last: index === last
    }));
  }

  link(): string {
    const points = this.nodes();
    if (points.length < 2) return '';
    return 'M ' + points[0].x + ' 36 H ' + points[points.length - 1].x;
  }

  offset(): number {
    const amount = Math.min(1, Math.max(0, this.value()));
    return this.go() ? this.circ * (1 - amount) : this.circ;
  }
}
