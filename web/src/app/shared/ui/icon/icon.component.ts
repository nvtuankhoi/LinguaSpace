import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Minimal inline-SVG icon set (stroke = currentColor). Kept dependency-free;
 * add cases as the UI needs them. Use a single, consistent set across the app.
 */
@Component({
  selector: 'app-icon',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <svg [attr.width]="size()" [attr.height]="size()" viewBox="0 0 24 24" fill="none"
         stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"
         aria-hidden="true" focusable="false">
      @switch (name()) {
        @case ('home') { <path d="M3 10.5 12 3l9 7.5" /><path d="M5 9.5V20a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V9.5" /><path d="M9 21v-6h6v6" /> }
        @case ('rooms') { <rect x="9" y="3" width="6" height="11" rx="3" /><path d="M5 11a7 7 0 0 0 14 0" /><path d="M12 18v3" /> }
        @case ('messages') { <path d="M21 11.5a8.5 8.5 0 0 1-12.3 7.6L3 21l1.9-5.7A8.5 8.5 0 1 1 21 11.5z" /> }
        @case ('notifications') { <path d="M6 8a6 6 0 0 1 12 0c0 7 3 7 3 9H3c0-2 3-2 3-9z" /><path d="M10 21a2 2 0 0 0 4 0" /> }
        @case ('profile') { <circle cx="12" cy="8" r="4" /><path d="M4 21a8 8 0 0 1 16 0" /> }
        @case ('search') { <circle cx="11" cy="11" r="7" /><path d="m21 21-4.3-4.3" /> }
        @case ('sun') { <circle cx="12" cy="12" r="4" /><path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4" /> }
        @case ('moon') { <path d="M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8z" /> }
        @case ('login') { <path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4" /><path d="M10 17l5-5-5-5" /><path d="M15 12H3" /> }
        @case ('logout') { <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" /><path d="M16 17l5-5-5-5" /><path d="M21 12H9" /> }
        @case ('user-plus') { <circle cx="9" cy="8" r="4" /><path d="M3 21a6 6 0 0 1 12 0" /><path d="M19 8v6M22 11h-6" /> }
        @case ('arrow-right') { <path d="M5 12h14M13 6l6 6-6 6" /> }
        @case ('sparkles') { <path d="M12 3l1.6 4.4L18 9l-4.4 1.6L12 15l-1.6-4.4L6 9l4.4-1.6z" /><path d="M19 14l.7 1.9L21.5 17l-1.8.7L19 19.5l-.7-1.8L16.5 17l1.8-.7z" /> }
        @case ('check') { <path d="M20 6 9 17l-5-5" /> }
        @case ('heart') { <path d="M12 21s-7-4.4-9.5-9A5 5 0 0 1 12 6a5 5 0 0 1 9.5 6c-2.5 4.6-9.5 9-9.5 9z" /> }
        @case ('heart-filled') { <path d="M12 21s-7-4.4-9.5-9A5 5 0 0 1 12 6a5 5 0 0 1 9.5 6c-2.5 4.6-9.5 9-9.5 9z" fill="currentColor" /> }
        @case ('google') { <path d="M21.35 11.1H12v2.6h5.35c-.5 2.4-2.6 3.9-5.35 3.9a6.4 6.4 0 1 1 0-12.8c1.7 0 3.2.6 4.4 1.7l1.9-1.9A9 9 0 1 0 12 21a9 9 0 0 0 9.35-9.9z" /> }
        @case ('award') { <circle cx="12" cy="8" r="6" /><path d="M8.21 13.9l-1.2 8.1 5-3 5 3-1.2-8.1" /> }
        @case ('user-check') { <circle cx="9" cy="8" r="4" /><path d="M3 21a6 6 0 0 1 12 0" /><path d="M16 11l2 2 4-4" /> }
        @case ('eye') { <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" /><circle cx="12" cy="12" r="3" /> }
        @case ('trash') { <path d="M3 6h18M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" /><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" /> }
        @case ('info') { <circle cx="12" cy="12" r="9" /><path d="M12 8h.01" /><path d="M11 12h1v4h1" /> }
        @case ('trophy') { <path d="M8 21h8" /><path d="M12 17v4" /><path d="M7 4h10v4a5 5 0 0 1-10 0z" /><path d="M7 4H4v1a3 3 0 0 0 3 3" /><path d="M17 4h3v1a3 3 0 0 1-3 3" /> }
        @case ('flame') { <path d="M12 3c2 3 4.5 4.5 4.5 8.5a4.5 4.5 0 0 1-9 0c0-2 1-3.5 2-4.5C9 9 9.5 6 12 3z" /> }
        @case ('trending') { <path d="M3 17l6-6 4 4 8-8" /><path d="M21 7v5h-5" /> }
        @case ('mic') { <rect x="9" y="2" width="6" height="11" rx="3" /><path d="M5 11a7 7 0 0 0 14 0" /><path d="M12 18v3" /><path d="M9 21h6" /> }
        @case ('mic-off') { <line x1="2" y1="2" x2="22" y2="22" /><path d="M15 10v1a3 3 0 0 1-5.7 1.3" /><path d="M9 9V4a3 3 0 0 1 5.93-.7" /><path d="M17 17a7 7 0 0 1-9.82-.57" /><path d="M5 11a7 7 0 0 0 2.1 4.9" /><path d="M12 19v3" /><path d="M9 22h6" /> }
        @case ('camera') { <path d="M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z" /><circle cx="12" cy="13" r="4" /> }
        @case ('camera-off') { <line x1="1" y1="1" x2="23" y2="23" /><path d="M21 21H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h3m3-3h6l2 3h4a2 2 0 0 1 2 2v9.34" /><path d="M15 13a4 4 0 1 1-5.46-3.7" /> }
        @case ('screen') { <rect x="3" y="4" width="18" height="12" rx="2" /><path d="M8 20h8" /><path d="M12 16v4" /> }
        @case ('screen-off') { <line x1="2" y1="2" x2="22" y2="22" /><rect x="3" y="4" width="18" height="12" rx="2" /><path d="M8 20h8" /><path d="M12 16v4" /> }
        @case ('phone-off') { <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72c.13.96.36 1.9.7 2.81a2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45c.9.34 1.85.57 2.81.7A2 2 0 0 1 22 16.92z" /><line x1="2" y1="2" x2="22" y2="22" /> }
        @case ('plus') { <path d="M12 5v14M5 12h14" /> }
        @case ('flag') { <path d="M4 22V3" /><path d="M4 4h13l-2.5 3.5L17 11H4" /> }
        @case ('edit') { <path d="M12 20h9" /><path d="M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4z" /> }
        @case ('shield') { <path d="M12 3l7 3v5c0 4.5-3 7.6-7 9-4-1.4-7-4.5-7-9V6z" /> }
      }
    </svg>
  `,
  styles: [`:host { display: inline-flex; line-height: 0; }`],
})
export class IconComponent {
  readonly name = input.required<string>();
  readonly size = input<number>(20);
}
