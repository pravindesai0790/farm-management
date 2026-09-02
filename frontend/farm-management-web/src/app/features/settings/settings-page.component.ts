import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [MatButtonModule, MatCardModule, MatIconModule, RouterLink, RouterOutlet],
  template: `
    <section class="settings-page" aria-labelledby="settings-title">
      <p class="eyebrow">Settings</p>
      <h1 id="settings-title">Your account</h1>
      <p class="description">Manage your sign-in preferences and account security.</p>
      <mat-card class="settings-card">
        <mat-card-content>
          <mat-icon>password</mat-icon>
          <div><h2>Change password</h2><p>Update your password and sign in again securely.</p></div>
          <a mat-stroked-button routerLink="change-password">Change password</a>
        </mat-card-content>
      </mat-card>
      <router-outlet />
    </section>
  `,
  styles: [`
    .eyebrow { margin:0 0 10px; color:var(--app-primary); font-size:.72rem; font-weight:700; letter-spacing:.12em; text-transform:uppercase; }
    h1 { margin:0; font-size:clamp(2rem, 4vw, 3rem); }.description { margin:12px 0 28px; color:var(--app-muted); }
    .settings-card { max-width:760px; }.settings-card mat-card-content { display:flex; align-items:center; gap:16px; padding:24px !important; }.settings-card mat-icon { color:var(--app-primary); }.settings-card h2, .settings-card p { margin:0; }.settings-card p { margin-top:4px; color:var(--app-muted); }.settings-card a { margin-left:auto; white-space:nowrap; }
    @media (max-width:600px) { .settings-card mat-card-content { align-items:flex-start; flex-wrap:wrap; }.settings-card a { margin-left:46px; } }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsPageComponent {}
