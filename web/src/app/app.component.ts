import { Component, DestroyRef, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ThemeService } from './theme.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  readonly theme = inject(ThemeService);
  /** Phones: the header drops its nav row (113px to 60px) while scrolling down, and brings it back on scroll up. */
  readonly compact = signal(false);

  constructor() {
    let last = window.scrollY;
    const onScroll = () => {
      const y = window.scrollY;
      if (y < 80) this.compact.set(false);
      else if (y > last + 4) this.compact.set(true);
      else if (y < last - 4) this.compact.set(false);
      last = y;
    };
    window.addEventListener('scroll', onScroll, { passive: true });
    inject(DestroyRef).onDestroy(() => window.removeEventListener('scroll', onScroll));
  }
}
