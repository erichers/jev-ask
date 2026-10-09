import { Injectable, effect, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly mode = signal<'light' | 'dark'>(this.read());

  constructor() {
    effect(() => {
      const mode = this.mode();
      document.documentElement.setAttribute('data-theme', mode);
      document.querySelectorAll('meta[name="theme-color"]').forEach((meta) => meta.setAttribute('content', mode === 'dark' ? '#111111' : '#f5f5f3'));
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
