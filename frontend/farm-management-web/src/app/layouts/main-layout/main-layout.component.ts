import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import { PermissionService } from '../../core/auth/permission.service';

interface NavigationItem {
  readonly label: string;
  readonly icon: string;
  readonly route: string;
  readonly permissions?: readonly string[];
}

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatSidenavModule,
    MatToolbarModule,
    RouterLink,
    RouterLinkActive,
    RouterOutlet
  ],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MainLayoutComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly permissionService = inject(PermissionService);
  private readonly destroyRef = inject(DestroyRef);

  readonly currentUser = this.authService.user;

  readonly navigationItems: readonly NavigationItem[] = [
    { label: 'Dashboard', icon: 'space_dashboard', route: '/dashboard' },
    { label: 'Farms', icon: 'landscape', route: '/farms' },
    { label: 'Crops', icon: 'grass', route: '/crops' },
    { label: 'Activities', icon: 'event_note', route: '/activities' },
    {
      label: 'Administration',
      icon: 'admin_panel_settings',
      route: '/administration',
      permissions: ['Users.View', 'Roles.View', 'Permissions.View']
    },
    { label: 'Settings', icon: 'settings', route: '/settings' }
  ];

  readonly visibleNavigationItems = computed(() => this.navigationItems.filter((item) =>
    item.permissions === undefined || this.permissionService.hasAny(item.permissions)));

  logout(): void {
    this.authService.logout()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => void this.router.navigateByUrl('/login'),
        error: () => void this.router.navigateByUrl('/login')
      });
  }

  get userInitials(): string {
    const user = this.currentUser();
    return user === null
      ? 'FM'
      : `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase();
  }
}
