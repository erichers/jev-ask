import { Component, DestroyRef, ElementRef, Injector, afterNextRender, effect, inject, input, output, signal, viewChild } from '@angular/core';
import { Chart } from 'chart.js/auto';
import { FlowDiagramComponent } from './flow-diagram.component';
import { AskResponse, dataLabel, parserLabel, reducedMotion } from './models';
import { countTo, onVisible, whenSettled } from './motion';
import { PathFieldComponent } from './path-field.component';

@Component({
  selector: 'app-result',
  standalone: true,
  imports: [FlowDiagramComponent, PathFieldComponent],
  templateUrl: './result.component.html',
  styleUrl: './result.component.css'
})
export class ResultComponent {
  readonly result = input.required<AskResponse>();
  readonly theme = input.required<'light' | 'dark'>();
  readonly updating = input(false);
  readonly edited = output<{
    ticker: string;
    condition: string;
    style: string;
    levelMode: string;
    level: number;
    expiry: string;
  }>();

  readonly ticker = signal('');
  readonly condition = signal<'above' | 'below'>('above');
  readonly style = signal<'close' | 'touch'>('close');
  readonly levelMode = signal<'absolute' | 'percent'>('absolute');
  readonly level = signal(0);
  readonly expiry = signal('');
  readonly shown = signal(0);
  readonly reasonText = signal('');
  readonly visibleSteps = signal(0);
  readonly chipsOn = signal(true);
  readonly railOn = signal(false);

  readonly marketSteps = ['Spot', 'Vol', 'Days', 'Chance'];
  readonly flowNote = 'Spot and realized vol set the curve. Trading days widen it. The chance is the area past the target.';
  readonly parserText = () => parserLabel(this.result());
  readonly dataText = () => dataLabel(this.result());

  private readonly canvas = viewChild<ElementRef<HTMLCanvasElement>>('chart');
  private readonly rail = viewChild<ElementRef<HTMLElement>>('rail');
  private chart: Chart | null = null;
  private timer = 0;
  private stopCount: () => void = () => undefined;
  private stopRail: () => void = () => undefined;
  private drawGen = 0;

  constructor(injector: Injector) {
    inject(DestroyRef).onDestroy(() => {
      window.clearTimeout(this.timer);
      this.stopCount();
      this.stopRail();
      this.chart?.destroy();
    });

    effect(() => {
      const row = this.result();
      this.ticker.set(row.intent.ticker);
      this.condition.set(row.intent.direction === 'below' ? 'below' : 'above');
      this.style.set(row.intent.style);
      this.levelMode.set(row.intent.levelMode);
      this.level.set(row.intent.level);
      this.expiry.set(row.intent.expiry.slice(0, 10));
      this.play(row);
    });

    afterNextRender(() => {
      effect(() => {
        const row = this.result();
        this.theme();
        const gen = ++this.drawGen;
        void whenSettled().then(() => {
          if (gen !== this.drawGen) return;
          const canvas = this.canvas()?.nativeElement;
          if (!canvas) return;
          this.draw(canvas, row);
        });
      }, { injector });
    });
  }

  pct(value: number): string {
    return (value * 100).toFixed(1) + '%';
  }

  px(value: number): string {
    return value.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  bandLeft(): number {
    return Math.min(100, Math.max(0, this.result().bandLow * 100));
  }

  bandWidth(): number {
    const width = (this.result().bandHigh - this.result().bandLow) * 100;
    return Math.min(100 - this.bandLeft(), Math.max(width, 0.8));
  }

  markLeft(): number {
    return Math.min(100, Math.max(0, this.result().probability * 100));
  }

  steps() {
    return this.result().steps.slice(0, this.visibleSteps());
  }

  onField(): void {
    window.clearTimeout(this.timer);
    this.timer = window.setTimeout(() => this.emitEdit(), 320);
  }

  setMode(mode: 'absolute' | 'percent'): void {
    this.levelMode.set(mode);
    this.onField();
  }

  asText(event: Event): string {
    return (event.target as HTMLInputElement).value;
  }

  asNumber(event: Event): number {
    return Number((event.target as HTMLInputElement).value);
  }

  asSelect(event: Event): 'above' | 'below' {
    return (event.target as HTMLSelectElement).value === 'below' ? 'below' : 'above';
  }

  asStyle(event: Event): 'close' | 'touch' {
    return (event.target as HTMLSelectElement).value === 'touch' ? 'touch' : 'close';
  }

  private play(row: AskResponse): void {
    this.stopCount();
    this.chipsOn.set(true);
    this.reasonText.set(row.reasoning);
    this.visibleSteps.set(row.steps.length);
    this.stopCount = countTo(row.probability, (value) => this.shown.set(value));
    window.setTimeout(() => this.armRail(), 0);
  }

  private armRail(tries = 0): void {
    this.stopRail();
    const el = this.rail()?.nativeElement;
    if (!el) {
      if (tries < 6) window.setTimeout(() => this.armRail(tries + 1), 16);
      else this.railOn.set(true);
      return;
    }
    if (reducedMotion()) {
      this.railOn.set(true);
      return;
    }
    this.railOn.set(false);
    this.stopRail = onVisible(el, () => {
      void whenSettled().then(() => this.railOn.set(true));
    });
  }

  private emitEdit(): void {
    const level = Number(this.level());
    if (!this.ticker().trim() || !Number.isFinite(level) || level <= 0 || !this.expiry()) return;
    this.edited.emit({
      ticker: this.ticker().trim(),
      condition: this.condition(),
      style: this.style(),
      levelMode: this.levelMode(),
      level,
      expiry: this.expiry()
    });
  }

  private draw(canvas: HTMLCanvasElement, row: AskResponse): void {
    const styles = getComputedStyle(document.documentElement);
    const signal = styles.getPropertyValue('--signal').trim() || '#4f7a00';
    const down = styles.getPropertyValue('--down').trim() || '#a33b3b';
    const muted = styles.getPropertyValue('--muted').trim() || '#5c6b7e';
    const line = styles.getPropertyValue('--line').trim() || 'rgba(11,18,32,0.08)';
    const calm = reducedMotion();
    this.chart?.destroy();
    this.chart = new Chart(canvas, {
      type: 'line',
      data: {
        labels: row.chart.map((point) => point.date),
        datasets: [
          {
            label: '',
            data: row.chart.map((point) => point.close),
            borderColor: 'transparent',
            backgroundColor: styles.getPropertyValue('--signal-soft').trim() || 'rgba(79, 122, 0, 0.16)',
            pointRadius: 0,
            borderWidth: 0,
            tension: 0.15,
            fill: 'origin'
          },
          {
            label: row.intent.ticker,
            data: row.chart.map((point) => point.close),
            borderColor: signal,
            backgroundColor: 'transparent',
            pointRadius: 0,
            borderWidth: 2,
            tension: 0.15,
            fill: false
          },
          {
            label: 'Target',
            data: row.chart.map(() => row.targetPrice),
            borderColor: down,
            borderDash: [5, 4],
            pointRadius: 0,
            borderWidth: 1.5,
            fill: false
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        animation: calm ? false : {
          duration: 400,
          easing: 'easeOutCubic',
          delay(ctx) {
            if (ctx.type !== 'data') return 0;
            if (ctx.datasetIndex === 0) return 420;
            if (ctx.datasetIndex === 2) return 520;
            return (ctx.dataIndex ?? 0) * 5;
          }
        },
        plugins: {
          legend: {
            labels: {
              color: muted,
              boxWidth: 12,
              font: { family: 'Inter', size: 14 },
              filter: (item) => item.text !== ''
            }
          }
        },
        scales: {
          x: {
            ticks: {
              color: muted,
              maxTicksLimit: 5,
              maxRotation: 0,
              autoSkipPadding: 16,
              font: (c: { chart: { width: number } }) => ({ family: 'JetBrains Mono', size: c.chart.width < 480 ? 12 : 14 }),
              // "2026-03-06" -> "Mar 26": short labels stay level, no rotated overlap
              callback(value) {
                const raw = String(this.getLabelForValue(Number(value)));
                const m = /^(\d{4})-(\d{2})/.exec(raw);
                return m ? ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'][Number(m[2]) - 1] + ' ' + m[1].slice(2) : raw;
              }
            },
            grid: { color: line }
          },
          y: {
            ticks: { color: muted, font: (c: { chart: { width: number } }) => ({ family: 'JetBrains Mono', size: c.chart.width < 480 ? 12 : 14 }) },
            grid: { color: line }
          }
        }
      }
    });
  }
}
