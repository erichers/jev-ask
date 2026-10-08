import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { Router } from '@angular/router';
import { ApiService } from './api.service';
import { HistoryService } from './history.service';
import { HistoryItem, TapeRow } from './models';

const FALLBACK = [
  'Will NVDA close above 250 by end of month?',
  'Chance SPY drops 5% this month?',
  'Does TSLA touch 420 before Dec 20?',
  'Will AAPL finish below 300 by end of month?',
  'QQQ rises 3% in 10 days',
  'Will AMZN close above 280 by year end?'
];

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  readonly history = inject(HistoryService);

  readonly question = signal('');
  readonly examples = signal<string[]>(FALLBACK);
  readonly tape = signal<TapeRow[]>([]);
  readonly tapeReady = signal(false);
  readonly recent = signal<HistoryItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  private readonly box = viewChild<ElementRef<HTMLTextAreaElement>>('box');

  constructor() {
    void this.api.examples().then((rows) => {
      if (rows.length) this.examples.set(rows);
    }).catch(() => undefined);

    void this.api.tape().then((rows) => this.tape.set(rows)).catch(() => this.tape.set([])).finally(() => this.tapeReady.set(true));

    void this.api.history().then((rows) => {
      const mapped = rows.map((row) => ({
        id: row.id,
        question: row.question,
        probability: row.probability,
        ticker: row.ticker,
        at: row.createdAtUtc
      }));
      this.recent.set(mapped.length ? mapped : this.history.items());
    }).catch(() => this.recent.set(this.history.items()));
  }

  text(event: Event): string {
    return (event.target as HTMLTextAreaElement).value;
  }

  async submit(event: Event): Promise<void> {
    event.preventDefault();
    await this.run(this.question());
  }

  async use(text: string): Promise<void> {
    const field = this.box()?.nativeElement;
    if (field) field.value = text;
    this.question.set(text);
    await this.run(text);
  }

  async open(item: HistoryItem): Promise<void> {
    if (item.id) {
      await this.router.navigate(['/q', item.id]);
      return;
    }
    await this.use(item.question);
  }

  pct(value: number): string {
    const sign = value > 0 ? '+' : '';
    return sign + (value * 100).toFixed(2) + '%';
  }

  px(value: number): string {
    return value.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  chance(value: number): string {
    return (value * 100).toFixed(1) + '%';
  }

  private async run(text: string): Promise<void> {
    const question = text.trim();
    if (!question) {
      this.error.set('Type a market question first.');
      return;
    }
    this.question.set(question);
    this.loading.set(true);
    this.error.set(null);
    try {
      const response = await this.api.ask(question);
      this.history.remember({
        id: response.id,
        question,
        probability: response.probability,
        ticker: response.intent.ticker,
        at: new Date().toISOString()
      });
      await this.router.navigate(['/q', response.id], { state: { result: response } });
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Could not answer that question.');
    } finally {
      this.loading.set(false);
    }
  }
}
