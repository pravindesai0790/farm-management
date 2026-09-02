import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { PermissionService } from '../../core/auth/permission.service';
import { FarmManagementService } from '../../core/farm-management/farm-management.service';
import { Organization } from '../../core/farm-management/farm-management.models';
import { getApiErrorMessage } from '../../core/models/api-error.model';

@Component({
  selector: 'app-organization-page', standalone: true,
  imports: [DatePipe, MatButtonModule, MatCardModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressSpinnerModule, ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="section-heading"><div><p class="eyebrow">Workspace administration</p><h2>{{ isCreating ? 'Create organization' : 'Organization' }}</h2><p>{{ isCreating ? 'Set up a new workspace for your farm team.' : 'Manage the organization connected to your account.' }}</p></div></div>
    @if (isLoading()) { <div class="loading-state"><mat-spinner diameter="36"/><span>Loading organization…</span></div> } @else {
      <mat-card><mat-card-content><form [formGroup]="form" (ngSubmit)="submit()"><div class="form-grid">
        <mat-form-field appearance="outline"><mat-label>Organization name</mat-label><input matInput formControlName="name"/>@if(form.controls.name.hasError('required')){<mat-error>Name is required.</mat-error>}</mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Organization code</mat-label><input matInput formControlName="code"/>@if(form.controls.code.hasError('required')){<mat-error>Code is required.</mat-error>}</mat-form-field>
      </div>
      @if (!isCreating && organization(); as org) { <div class="status-card"><span class="status-pill" [class.status-pill-inactive]="!org.isActive">{{org.isActive ? 'Active' : 'Inactive'}}</span><span>Created {{org.createdAt | date:'mediumDate'}}</span></div> }
      @if(errorMessage()){<p class="form-error" role="alert">{{errorMessage()}}</p>}<div class="form-actions"><a mat-button routerLink="/dashboard">Cancel</a>@if(isCreating || permissionService.has('Organization.Update')){<button mat-flat-button color="primary" type="submit" [disabled]="isSubmitting()">{{isCreating ? 'Create organization' : 'Save changes'}}</button>}</div></form></mat-card-content></mat-card>
      @if (!isCreating && organization(); as org) { <mat-card class="action-card"><mat-card-content><h3>Organization status</h3><p>{{org.isActive ? 'Active members can sign in and use this workspace.' : 'This organization is inactive.'}}</p>@if(org.isActive && permissionService.has('Organization.Deactivate')){<button mat-stroked-button color="warn" (click)="changeStatus(false)" [disabled]="isSubmitting()">Deactivate organization</button>} @else if(!org.isActive && permissionService.has('Organization.Activate')){<button mat-flat-button color="primary" (click)="changeStatus(true)" [disabled]="isSubmitting()">Activate organization</button>}</mat-card-content></mat-card> }
    }
  `,
  styles: [`:host{display:block}.section-heading{margin-bottom:24px}.section-heading h2{margin:4px 0}.section-heading p{color:var(--app-muted)}mat-card{max-width:760px}.form-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:16px}.form-actions{display:flex;justify-content:flex-end;gap:12px;margin-top:20px}.form-error{color:#b3261e}.status-card{display:flex;gap:16px;align-items:center;margin:8px 0;color:var(--app-muted)}.action-card{margin-top:18px}.action-card h3{margin-top:0}@media(max-width:640px){.form-grid{grid-template-columns:1fr}}`]
})
export class OrganizationPageComponent implements OnInit {
  private readonly service = inject(FarmManagementService); private readonly route = inject(ActivatedRoute); private readonly router = inject(Router); private readonly snack = inject(MatSnackBar); private readonly destroyRef = inject(DestroyRef); private readonly formBuilder = inject(FormBuilder);
  readonly permissionService = inject(PermissionService); readonly isCreating = this.route.snapshot.routeConfig?.path === 'new'; readonly organization = signal<Organization | null>(null); readonly isLoading = signal(false); readonly isSubmitting = signal(false); readonly errorMessage = signal<string | null>(null);
  readonly form = this.formBuilder.nonNullable.group({ name: ['', [Validators.required, Validators.maxLength(200)]], code: ['', [Validators.required, Validators.maxLength(50)]] });
  ngOnInit(): void { if (this.isCreating) return; this.isLoading.set(true); this.service.getOrganization().pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.isLoading.set(false))).subscribe({ next: org => { this.organization.set(org); this.form.patchValue({ name: org.name, code: org.code }); }, error: e => this.errorMessage.set(getApiErrorMessage(e, 'Organization could not be loaded.')) }); }
  submit(): void { if (this.form.invalid) { this.form.markAllAsTouched(); return; } this.isSubmitting.set(true); const value = this.form.getRawValue(); const request = this.isCreating ? this.service.createOrganization(value) : this.service.updateOrganization(value); request.pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.isSubmitting.set(false))).subscribe({ next: org => { this.organization.set(org); this.snack.open(this.isCreating ? 'Organization created.' : 'Organization updated.', 'Dismiss', { duration: 3000 }); void this.router.navigateByUrl(this.isCreating ? '/dashboard' : '/organization'); }, error: e => this.errorMessage.set(getApiErrorMessage(e, 'Organization could not be saved.')) }); }
  changeStatus(active: boolean): void { this.isSubmitting.set(true); const request = active ? this.service.activateOrganization() : this.service.deactivateOrganization(); request.pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.isSubmitting.set(false))).subscribe({ next: () => { const org = this.organization(); if (org) this.organization.set({ ...org, isActive: active }); this.snack.open(`Organization ${active ? 'activated' : 'deactivated'}.`, 'Dismiss', { duration: 3000 }); }, error: e => this.errorMessage.set(getApiErrorMessage(e, 'Organization status could not be changed.')) }); }
}
