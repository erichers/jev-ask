import { Component, effect, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from './api.service';
import { HistoryService } from './history.service';
import { AskResponse } from './models';
import { ResultComponent } from './result.component';
import { ThemeService } from './theme.service';

@Component({
  selector: 'app-result-page',
  standalone: true,
  imports: [ResultComponent, RouterLink],
  templateUrl: './result-page.component.html',
  styleUrl: './result-page.component.css'
})
export class ResultPageComponent {
  readonly id = input.required<string>();
  readonly theme = inject(ThemeService);
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly history = inject(HistoryService);

  readonly result = signal<AskResponse | null>(null);
  readonly loading = signal(true);
  readonly updating = signal(false);
  readonly error = signal<string | null>(null);

  constructor() {
    effect(() => {
      const id = this.id();
      void this.load(id);
    });
  }

  async onEdit(body: {
    ticker: string;
    condition: string;
    style: string;
    levelMode: string;
    level: number;
    expiry: string;
  }): Promise<void> {
    this.updating.set(true);
    this.error.set(null);
    try {
      const response = await this.api.recompute(body);
      this.history.remember({
        id: response.id,
        question: response.question,
        probability: response.probability,
        ticker: response.intent.ticker,
        at: new Date().toISOString()
      });
      await this.router.navigate(['/q', response.id], { state: { result: response } });
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Could not update that question.');
    } finally {
      this.updating.set(false);
    }
  }

  async download(): Promise<void> {
    const current = this.result();
    if (!current) return;
    try {
      const blob = await this.api.pdf(current);
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = 'jev-ask.pdf';
      link.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Could not build the PDF.');
    }
  }

  private async load(id: string): Promise<void> {
    const state = history.state?.result as AskResponse | undefined;
    if (state && String(state.id) === id) {
      this.result.set(state);
      this.loading.set(false);
      this.error.set(null);
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    try {
      this.result.set(await this.api.historyById(id));
    } catch (err) {
      this.result.set(null);
      this.error.set(err instanceof Error ? err.message : 'That question is not in the history.');
    } finally {
      this.loading.set(false);
    }
  }
}
