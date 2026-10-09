import { Component, DestroyRef, ElementRef, effect, inject, input, signal, viewChild } from '@angular/core';
import { Router } from '@angular/router';
import { ApiService } from './api.service';
import { FlowDiagramComponent } from './flow-diagram.component';
import { FunAnswer, FunCard, reducedMotion } from './models';
import { countTo, onVisible, whenSettled } from './motion';
import { ThemeService } from './theme.service';

@Component({
  selector: 'app-fun',
  standalone: true,
  imports: [FlowDiagramComponent],
  templateUrl: './fun-page.component.html',
  styleUrl: './fun-page.component.css'
})
export class FunPageComponent {
  readonly id = input<string>();
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  readonly theme = inject(ThemeService);

  readonly question = signal('');
  readonly category = signal('All');
  readonly categories = signal<string[]>([]);
  readonly bank = signal<FunCard[]>([]);
  readonly answer = signal<FunAnswer | null>(null);
  readonly shown = signal(0);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly copied = signal(false);
  readonly flipping = signal(false);
  readonly recent = signal<FunAnswer[]>([]);
  readonly fill = signal(0);
  readonly funSteps = ['Question', 'Match', 'Chance'];
  readonly flowNote = 'A close question uses the bank. Anything else is hashed, so the same words stay put.';
  private stopCount: () => void = () => undefined;
  private stopMeter: () => void = () => undefined;
  private loadedId = '';
  private readonly meterEl = viewChild<ElementRef<HTMLElement>>('meter');

  constructor() {
    inject(DestroyRef).onDestroy(() => {
      this.stopCount();
      this.stopMeter();
    });
    void this.loadBank();
    void this.loadRecent();
    effect(() => {
      const id = this.id();
      if (!id) return;
      if (this.loadedId === id && this.answer()?.id === Number(id)) return;
      const state = history.state?.result as FunAnswer | undefined;
      if (state && String(state.id) === id) {
        this.loadedId = id;
        this.reveal(state);
        return;
      }
      this.loadedId = id;
      void this.open(id);
    });
  }

  text(event: Event): string {
    return (event.target as HTMLTextAreaElement).value;
  }

  setCategory(name: string): void {
    this.category.set(name);
  }

  filtered(): FunCard[] {
    const name = this.category();
    if (name === 'All') return [];
    return this.bank().filter((row) => row.category === name);
  }

  async submit(event: Event): Promise<void> {
    event.preventDefault();
    const text = this.question().trim();
    if (!text) {
      this.error.set('Type a fun question first.');
      return;
    }
    await this.ask(text);
  }

  async pick(card: FunCard): Promise<void> {
    this.question.set(card.question);
    await this.ask(card.question);
  }

  async shuffle(): Promise<void> {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set(null);
    this.copied.set(false);
    try {
      await this.deal(await this.api.funShuffle());
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Could not shuffle.');
    } finally {
      this.busy.set(false);
    }
  }

  async copyLink(): Promise<void> {
    const row = this.answer();
    if (!row?.id) return;
    this.loadedId = String(row.id);
    if (this.id() !== String(row.id)) {
      await this.router.navigate(['/fun', row.id], { state: { result: row }, replaceUrl: true });
    }
    try {
      await navigator.clipboard.writeText(location.href);
      this.copied.set(true);
    } catch {
      this.error.set('Could not copy the link.');
    }
  }

  download(): void {
    const row = this.answer();
    if (!row) return;
    const canvas = document.createElement('canvas');
    const width = 1080;
    const height = 1400;
    canvas.width = width;
    canvas.height = height;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    const dark = this.theme.mode() === 'dark';
    const paper = dark ? '#111111' : '#f5f5f3';
    const ink = dark ? '#f4f4f2' : '#111111';
    const muted = dark ? '#a3a3a3' : '#5e5e5e';
    const signal = dark ? '#cefb31' : '#4f7a00';
    const line = dark ? 'rgba(244,244,242,0.18)' : 'rgba(17,17,17,0.14)';
    ctx.fillStyle = paper;
    ctx.fillRect(0, 0, width, height);
    ctx.strokeStyle = line;
    ctx.lineWidth = 2;
    ctx.strokeRect(36, 36, width - 72, height - 72);

    ctx.fillStyle = ink;
    ctx.font = '500 42px Georgia, serif';
    ctx.fillText('Jev Ask', 84, 130);
    ctx.fillStyle = muted;
    ctx.font = '600 22px sans-serif';
    ctx.fillText((row.category + '  ·  ' + row.tag).toUpperCase(), 84, 188);

    ctx.fillStyle = ink;
    ctx.font = '500 54px Georgia, serif';
    let y = 280;
    for (const lineText of wrap(ctx, row.question, width - 168)) {
      ctx.fillText(lineText, 84, y);
      y += 68;
    }

    ctx.fillStyle = signal;
    ctx.font = '500 180px sans-serif';
    ctx.fillText(row.percent + '%', 84, y + 190);

    ctx.fillStyle = ink;
    ctx.font = '400 32px sans-serif';
    y += 280;
    for (const lineText of wrap(ctx, row.reasoning, width - 168)) {
      ctx.fillText(lineText, 84, y);
      y += 46;
    }

    ctx.fillStyle = muted;
    ctx.font = '400 26px sans-serif';
    ctx.fillText('For fun. Not a prediction.', 84, height - 140);
    ctx.fillText('by Ulric studio', 84, height - 96);

    const link = document.createElement('a');
    link.href = canvas.toDataURL('image/png');
    link.download = 'jev-ask-fun.png';
    link.click();
  }

  async openRecent(row: FunAnswer): Promise<void> {
    this.loadedId = String(row.id);
    this.reveal(row);
    await this.router.navigate(['/fun', row.id], { state: { result: row } });
  }

  pct(value: number): string {
    return Math.round(value * 100) + '%';
  }

  private async ask(text: string): Promise<void> {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set(null);
    this.copied.set(false);
    try {
      await this.deal(await this.api.funAsk(text));
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Could not answer that.');
    } finally {
      this.busy.set(false);
    }
  }

  private async open(id: string): Promise<void> {
    try {
      const row = await this.api.funById(id);
      if (this.id() === id) this.reveal(row);
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'That card is not in the history.');
    }
  }

  private async deal(row: FunAnswer): Promise<void> {
    const motion = !reducedMotion();
    this.flipping.set(false);
    if (motion) await wait(16);
    this.flipping.set(motion);
    this.reveal(row);
    this.loadedId = String(row.id);
    await this.router.navigate(['/fun', row.id], { state: { result: row }, replaceUrl: true });
    if (motion) {
      await wait(420);
      this.flipping.set(false);
    }
    void this.loadRecent();
  }

  private reveal(row: FunAnswer): void {
    this.answer.set(row);
    this.stopCount();
    this.stopCount = countTo(row.likelihood, (value) => this.shown.set(value));
    window.setTimeout(() => this.armMeter(row.percent), 0);
  }

  private armMeter(percent: number): void {
    this.stopMeter();
    const el = this.meterEl()?.nativeElement;
    if (!el || reducedMotion()) {
      this.fill.set(percent);
      return;
    }
    this.fill.set(0);
    this.stopMeter = onVisible(el, () => {
      void whenSettled().then(() => this.fill.set(percent));
    });
  }

  private async loadBank(): Promise<void> {
    try {
      const rows = await this.api.funBank();
      this.bank.set(rows);
      const names = [...new Set(rows.map((row) => row.category))];
      this.categories.set(names);
    } catch {
      this.bank.set([]);
    }
  }

  private async loadRecent(): Promise<void> {
    try {
      const seen = new Set<string>();
      const rows = (await this.api.funHistory()).filter((row) => {
        const key = row.question.trim().toLowerCase();
        if (seen.has(key)) return false;
        seen.add(key);
        return true;
      });
      this.recent.set(rows.slice(0, 8));
    } catch {
      this.recent.set([]);
    }
  }
}

function wait(ms: number): Promise<void> {
  return new Promise((resolve) => window.setTimeout(resolve, ms));
}

function wrap(ctx: CanvasRenderingContext2D, text: string, max: number): string[] {
  const lines: string[] = [];
  let line = '';
  for (const word of text.split(/\s+/)) {
    const next = line ? line + ' ' + word : word;
    if (ctx.measureText(next).width > max && line) {
      lines.push(line);
      line = word;
    } else {
      line = next;
    }
  }
  if (line) lines.push(line);
  return lines;
}
