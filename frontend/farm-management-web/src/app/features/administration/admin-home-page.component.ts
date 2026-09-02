import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';

import { PermissionService } from '../../core/auth/permission.service';

@Component({
  selector: 'app-admin-home-page',
  standalone: true,
  imports: [MatButtonModule, MatCardModule, MatIconModule, RouterLink],
  template: `
    <div class="admin-cards">
      @if (permissionService.has('Users.View')) {
        <mat-card>
          <mat-card-content>
            <mat-icon>group</mat-icon>
            <h2>Users</h2>
            <p>Invite people, manage access, and keep accounts secure.</p>
            <a mat-stroked-button routerLink="/administration/users">Manage users</a>
          </mat-card-content>
        </mat-card>
      }
      @if (permissionService.has('Roles.View')) {
        <mat-card>
          <mat-card-content>
            <mat-icon>shield</mat-icon>
            <h2>Roles</h2>
            <p>Shape role access for teams across your organization.</p>
            <a mat-stroked-button routerLink="/administration/roles">Manage roles</a>
          </mat-card-content>
        </mat-card>
      }
      @if (permissionService.has('Permissions.View')) {
        <mat-card>
          <mat-card-content>
            <mat-icon>key</mat-icon>
            <h2>Permissions</h2>
            <p>Review the system permissions available to roles.</p>
            <a mat-stroked-button routerLink="/administration/permissions">View permissions</a>
          </mat-card-content>
        </mat-card>
      }
    </div>
  `,
  styles: [`
    .admin-cards { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 16px; }
    mat-card-content { display: flex; flex-direction: column; align-items: flex-start; gap: 10px; padding: 24px !important; }
    mat-icon { color: var(--app-primary); font-size: 30px; width: 30px; height: 30px; }
    h2, p { margin: 0; }
    p { color: var(--app-muted); min-height: 48px; }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminHomePageComponent {
  readonly permissionService = inject(PermissionService);
}
