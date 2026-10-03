import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

import { PurchaseInvoiceService } from '../../../../core/expenses/purchase-invoice.service';
import { InventoryService } from '../../../../core/inventory/inventory.service';
import { StorageLocation } from '../../../../core/inventory/inventory.models';
import {
  PurchaseInvoiceRemainingLineResponse,
  PurchaseInvoiceResponse,
  ReceivePurchaseInvoiceItemLineRequest,
  ReceivePurchaseInvoiceItemsRequest,
} from '../../../../core/expenses/purchase-invoice.models';

export interface PurchaseInvoiceReceiveDialogData {
  invoice: PurchaseInvoiceResponse;
}

@Component({
  selector: 'app-purchase-invoice-receive-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatTableModule,
    MatProgressSpinnerModule,
    MatIconModule,
    MatSnackBarModule,
  ],
  templateUrl: './purchase-invoice-receive-dialog.component.html',
  styleUrls: ['./purchase-invoice-receive-dialog.component.scss'],
})
export class PurchaseInvoiceReceiveDialogComponent implements OnInit {
  readonly data: PurchaseInvoiceReceiveDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<PurchaseInvoiceReceiveDialogComponent>);
  private readonly fb = inject(FormBuilder);
  private readonly invoiceService = inject(PurchaseInvoiceService);
  private readonly inventoryService = inject(InventoryService);
  private readonly snackBar = inject(MatSnackBar);

  readonly loading = signal<boolean>(true);
  readonly submitting = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly storageLocations = signal<StorageLocation[]>([]);
  readonly remainingLines = signal<PurchaseInvoiceRemainingLineResponse[]>([]);

  readonly idempotencyKey = 'rcv-' + Date.now() + '-' + Math.random().toString(36).substring(2, 9);
  readonly today = new Date();

  form: FormGroup = this.fb.group({
    storageLocationId: ['', Validators.required],
    movementDate: [this.today, Validators.required],
    referenceNumber: [''],
    notes: [''],
    lines: this.fb.array([]),
  });

  get linesFormArray(): FormArray {
    return this.form.get('lines') as FormArray;
  }

  readonly displayedColumns: string[] = [
    'item',
    'sku',
    'unit',
    'invoiced',
    'received',
    'remaining',
    'receiveQuantity',
  ];

  ngOnInit(): void {
    this.loadData();
  }

  private loadData(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    // Load active storage locations for farm
    this.inventoryService.listLocations(1, 100, this.data.invoice.farmId, true).subscribe({
      next: (locationsRes) => {
        this.storageLocations.set([...locationsRes.items]);
        if (locationsRes.items.length === 1) {
          this.form.patchValue({ storageLocationId: locationsRes.items[0].id });
        }
      },
      error: (err) => {
        console.error('Failed to load storage locations', err);
        this.errorMessage.set('Failed to load storage locations for farm.');
      },
    });

    // Load remaining eligible lines
    this.invoiceService.getRemainingToReceive(this.data.invoice.id).subscribe({
      next: (lines) => {
        this.remainingLines.set(lines);
        this.buildLinesForm(lines);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load remaining invoice lines', err);
        this.errorMessage.set('Failed to load remaining invoice lines.');
        this.loading.set(false);
      },
    });
  }

  private buildLinesForm(lines: PurchaseInvoiceRemainingLineResponse[]): void {
    this.linesFormArray.clear();
    lines.forEach((line) => {
      this.linesFormArray.push(
        this.fb.group({
          purchaseInvoiceLineId: [line.purchaseInvoiceLineId],
          quantity: [
            line.remainingQuantity,
            [
              Validators.required,
              Validators.min(0),
              Validators.max(line.remainingQuantity),
            ],
          ],
        })
      );
    });
  }

  getLineFormGroup(index: number): FormGroup {
    return this.linesFormArray.at(index) as FormGroup;
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const rawValues = this.form.value;
    const selectedLines: ReceivePurchaseInvoiceItemLineRequest[] = [];

    this.linesFormArray.controls.forEach((control) => {
      const lineVal = control.value;
      const qty = Number(lineVal.quantity);
      if (qty > 0) {
        selectedLines.push({
          purchaseInvoiceLineId: lineVal.purchaseInvoiceLineId,
          quantity: qty,
        });
      }
    });

    if (selectedLines.length === 0) {
      this.snackBar.open('Please specify a quantity greater than zero for at least one item.', 'Close', {
        duration: 4000,
      });
      return;
    }

    const movementDateStr =
      rawValues.movementDate instanceof Date
        ? rawValues.movementDate.toISOString().split('T')[0]
        : rawValues.movementDate;

    const payload: ReceivePurchaseInvoiceItemsRequest = {
      storageLocationId: rawValues.storageLocationId,
      movementDate: movementDateStr,
      lines: selectedLines,
      referenceNumber: rawValues.referenceNumber ? rawValues.referenceNumber.trim() : null,
      notes: rawValues.notes ? rawValues.notes.trim() : null,
      idempotencyKey: this.idempotencyKey,
    };

    this.submitting.set(true);
    this.invoiceService.receiveItems(this.data.invoice.id, payload).subscribe({
      next: (res) => {
        this.submitting.set(false);
        this.snackBar.open('Items received into inventory successfully.', 'Close', {
          duration: 4000,
        });
        this.dialogRef.close(res);
      },
      error: (err) => {
        this.submitting.set(false);
        const msg = err.error?.message || 'Failed to receive items into stock.';
        this.errorMessage.set(msg);
        this.snackBar.open(msg, 'Close', { duration: 5000 });
      },
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
