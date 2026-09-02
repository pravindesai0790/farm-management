import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { debounceTime, distinctUntilChanged, finalize } from 'rxjs';

import { AdministrationService } from '../../../core/administration/administration.service';
import { Permission } from '../../../core/administration/administration.models';
import { getApiErrorMessage } from '../../../core/models/api-error.model';

@Component({
  selector: 'app-permissions-page',
  standalone: true,
  imports: [MatCardModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressSpinnerModule, ReactiveFormsModule],
  templateUrl: './permissions-page.component.html',
  styleUrl: './permissions-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PermissionsPageComponent implements OnInit {
  private readonly administrationService = inject(AdministrationService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formBuilder = inject(FormBuilder);
  readonly permissions = signal<readonly Permission[]>([]);
  readonly isLoading = signal(true);
  readonly search = signal('');
  readonly searchForm = this.formBuilder.nonNullable.group({ search: [''] });
  readonly filteredPermissions = computed(() => {
    const search = this.search().trim().toLowerCase();
    return this.permissions().filter((permission) => search.length === 0 || permission.name.toLowerCase().includes(search) || permission.module.toLowerCase().includes(search));
  });
  readonly groupedPermissions = computed(() => {
    const groups = new Map<string, readonly Permission[]>();
    for (const permission of this.filteredPermissions()) {
      groups.set(permission.module, [...(groups.get(permission.module) ?? []), permission]);
    }
    return [...groups.entries()].sort(([left], [right]) => left.localeCompare(right));
  });

  ngOnInit(): void {
    this.searchForm.controls.search.valueChanges.pipe(debounceTime(200), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef)).subscribe((search) => this.search.set(search));
    this.administrationService.listPermissions().pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.isLoading.set(false))).subscribe({
      next: (permissions) => this.permissions.set(permissions),
      error: (error: unknown) => this.snackBar.open(getApiErrorMessage(error, 'Permissions could not be loaded.'), 'Dismiss', { duration: 5000 })
    });
  }
}
