import { Component, DestroyRef, ElementRef, Injector, afterNextRender, effect, inject, input, output, signal, viewChild } from '@angular/core';
import { Chart } from 'chart.js/auto';
import { AskResponse, dataLabel, parserLabel, reducedMotion } from './models';

@Component({
  selector: 'app-result',
  standalone: true,
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

  readonly parserText = () => parserLabel(this.result());
  readonly dataText = () => dataLabel(this.result());

  private readonly canvas = viewChild<ElementRef<HTMLCanvasElement>>('chart');
  private chart: Chart | null = null;
  private timer = 0;
  private frame = 0;
  private wordTimer = 0;
  private stepTimer = 0;

  constructor(injector: Injector) {
    inject(DestroyRef).onDestroy(() => {
      window.clearTimeout(this.timer);
      window.clearInterval(this.wordTimer);
      window.clearInterval(this.stepTimer);
      window.cancelAnimationFrame(this.frame);
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
        const canvas = this.canvas()?.nativeElement;
        if (!canvas) return;
        this.draw(canvas, row);
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
    window.clearInterval(this.wordTimer);
    window.clearInterval(this.stepTimer);
    window.cancelAnimationFrame(this.frame);
    const calm = reducedMotion();
    this.chipsOn.set(false);
    this.railOn.set(calm);
    if (calm) {
      this.chipsOn.set(true);
      this.shown.set(row.probability);
      this.reasonText.set(row.reasoning);
      this.visibleSteps.set(row.steps.length);
      this.railOn.set(true);
      return;
    }

    queueMicrotask(() => this.chipsOn.set(true));
    window.setTimeout(() => this.railOn.set(true), 40);

    const start = performance.now();
    const target = row.probability;
    const tick = (now: number) => {
      const t = Math.min(1, (now - start) / 900);
      const eased = 1 - Math.pow(1 - t, 3);
      this.shown.set(target * eased);
      if (t < 1) this.frame = window.requestAnimationFrame(tick);
    };
    this.shown.set(0);
    this.frame = window.requestAnimationFrame(tick);

    const words = row.reasoning.split(' ');
    let word = 0;
    this.reasonText.set('');
    this.wordTimer = window.setInterval(() => {
      word += 2;
      this.reasonText.set(words.slice(0, word).join(' '));
      if (word >= words.length) window.clearInterval(this.wordTimer);
    }, 28);

    let step = 0;
    this.visibleSteps.set(0);
    this.stepTimer = window.setInterval(() => {
      step += 1;
      this.visibleSteps.set(step);
      if (step >= row.steps.length) window.clearInterval(this.stepTimer);
    }, 180);
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
    const signal = styles.getPropertyValue('--signal').trim() || '#19c39c';
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
            label: row.intent.ticker,
            data: row.chart.map((point) => point.close),
            borderColor: signal,
            backgroundColor: 'transparent',
            pointRadius: 0,
            borderWidth: 2,
            tension: 0.15
          },
          {
            label: 'Target',
            data: row.chart.map(() => row.targetPrice),
            borderColor: down,
            borderDash: [5, 4],
            pointRadius: 0,
            borderWidth: 1.5
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        animation: calm ? false : { duration: 1100, easing: 'easeOutQuart' },
        plugins: {
          legend: {
            labels: { color: muted, boxWidth: 12, font: { family: 'Inter', size: 12 } }
          }
        },
        scales: {
          x: {
            ticks: { color: muted, maxTicksLimit: 5, font: { family: 'JetBrains Mono', size: 10 } },
            grid: { color: line }
          },
          y: {
            ticks: { color: muted, font: { family: 'JetBrains Mono', size: 10 } },
            grid: { color: line }
          }
        }
      }
    });
  }
}
