import { Injectable, signal } from '@angular/core';
import { HistoryItem } from './models';

const KEY = 'jev-ask-history';

@Injectable({ providedIn: 'root' })
export class HistoryService {
  readonly items = signal<HistoryItem[]>(this.load());

  remember(item: HistoryItem): void {
    const next = [item, ...this.items().filter((row) => row.question !== item.question)].slice(0, 8);
    this.items.set(next);
    localStorage.setItem(KEY, JSON.stringify(next));
  }

  private load(): HistoryItem[] {
    try {
      const raw = localStorage.getItem(KEY);
      if (!raw) return [];
      const parsed = JSON.parse(raw) as HistoryItem[];
      return Array.isArray(parsed) ? parsed.slice(0, 8) : [];
    } catch {
      return [];
    }
  }
}
