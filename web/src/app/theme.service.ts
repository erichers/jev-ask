import { Injectable, effect, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly mode = signal<'light' | 'dark'>(this.read());

  constructor() {
    effect(() => {
      document.documentElement.setAttribute('data-theme', this.mode());
    });
  }

  toggle(): void {
    const next = this.mode() === 'dark' ? 'light' : 'dark';
    this.mode.set(next);
    localStorage.setItem('jev-theme', next);
  }

  private read(): 'light' | 'dark' {
    const stored = localStorage.getItem('jev-theme');
    if (stored === 'light' || stored === 'dark') return stored;
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }
}
