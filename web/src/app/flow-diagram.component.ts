import { Component, DestroyRef, ElementRef, afterNextRender, inject, input, signal, viewChild } from '@angular/core';
import { reducedMotion } from './models';
import { onVisible, whenSettled } from './motion';

@Component({
  selector: 'app-flow',
  standalone: true,
  template: `
    <figure class="flow" #host>
      @if (!wide()) {
        <ol class="flow-list" [class.go]="go()" [attr.aria-label]="note()">
          @for (node of nodes(); track node.label) {
            <li [class.last]="node.last"><span class="mark" aria-hidden="true"></span>{{ node.label }}</li>
          }
        </ol>
      } @else {
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
      }
      <figcaption>{{ note() }}</figcaption>
    </figure>
  `,
  styles: `
    :host { display: block; }
    .flow { margin: 0; }
    svg { width: 720px; max-width: none; height: auto; display: block; overflow: visible; }
    .flow-list { list-style: none; margin: 0; padding: 0; display: grid; gap: var(--s-3); }
    .flow-list li {
      position: relative;
      display: flex;
      align-items: center;
      gap: var(--s-3);
      min-height: 24px;
      max-width: none;
      font-size: 14px;
      font-weight: 600;
      letter-spacing: 0.04em;
      text-transform: uppercase;
      color: var(--muted);
    }
    .flow-list li:not(:last-child)::after {
      content: "";
      position: absolute;
      left: 11px;
      top: 24px;
      height: var(--s-3);
      border-left: 2px solid var(--line);
    }
    .flow-list .mark { flex: none; width: 24px; height: 24px; display: grid; place-items: center; }
    .flow-list .mark::before { content: ""; width: 8px; height: 8px; border-radius: 50%; background: var(--ink); }
    .flow-list .last .mark { border: 2px solid var(--ink); border-radius: 50%; }
    .flow-list .last { color: var(--ink); }
    .link {
      fill: none;
      stroke: var(--muted);
      stroke-width: 1.5;
      stroke-dasharray: 640;
      stroke-dashoffset: 640;
    }
    .go .link { stroke-dashoffset: 0; transition: stroke-dashoffset 640ms var(--ease); }
    .dot { fill: var(--ink); }
    .track { fill: none; stroke: var(--line); stroke-width: 1.5; }
    .arc {
      fill: none;
      stroke: var(--ink);
      stroke-width: 2;
      transform: rotate(-90deg);
      transform-origin: center;
      transform-box: fill-box;
      transition: stroke-dashoffset 640ms var(--ease);
    }
    text {
      fill: var(--muted);
      font-size: 14px;
      letter-spacing: 0.04em;
      text-anchor: middle;
      text-transform: uppercase;
    }
    figcaption { margin: var(--s-2) 0 0; color: var(--muted); font-size: 14px; max-width: 52ch; }
  `
})
export class FlowDiagramComponent {
  readonly steps = input.required<string[]>();
  readonly value = input(0);
  readonly note = input('');
  readonly go = signal(reducedMotion());
  /** The SVG only draws at 1:1 (720 wide); narrower hosts get the same steps as a list. */
  readonly wide = signal(false);
  readonly circ = 2 * Math.PI * 16;
  private readonly host = viewChild<ElementRef<HTMLElement>>('host');

  constructor() {
    const stop: { current: () => void } = { current: () => undefined };
    inject(DestroyRef).onDestroy(() => stop.current());
    afterNextRender(() => {
      const el = this.host()?.nativeElement;
      if (!el) return;
      const fit = () => this.wide.set(el.clientWidth >= 720);
      fit();
      const ro = new ResizeObserver(fit);
      ro.observe(el);
      const prev = stop.current;
      stop.current = () => { ro.disconnect(); prev(); };
      if (reducedMotion()) {
        this.go.set(true);
        return;
      }
      const off = onVisible(el, () => {
        void whenSettled().then(() => this.go.set(true));
      });
      stop.current = () => { ro.disconnect(); off(); };
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
