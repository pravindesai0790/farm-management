import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  FormArray,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ExpenseCategory } from '../../../../core/expenses/expense-category.models';
import { ExpenseCategoryService } from '../../../../core/expenses/expense-category.service';
import { ExpenseService } from '../../../../core/expenses/expense.service';
import {
  CreatePurchaseInvoiceLineRequest,
  CreatePurchaseInvoiceRequest,
  PurchaseInvoiceLineType,
  PurchaseInvoiceResponse,
  UpdatePurchaseInvoiceRequest,
} from '../../../../core/expenses/purchase-invoice.models';
import { PurchaseInvoiceService } from '../../../../core/expenses/purchase-invoice.service';
import { Supplier } from '../../../../core/expenses/supplier.models';
import { SupplierService } from '../../../../core/expenses/supplier.service';
import {
  CropCycle,
  CropCycleStage,
  Farm,
  FarmArea,
  Plantation,
} from '../../../../core/farm-management/farm-management.models';
import { FarmManagementService } from '../../../../core/farm-management/farm-management.service';
import { InventoryItem } from '../../../../core/inventory/inventory.models';
import { InventoryService } from '../../../../core/inventory/inventory.service';
import { CurrencyItem } from '../../../../core/labor/labor.models';
import { getApiErrorMessage } from '../../../../core/models/api-error.model';
import { formatDateOnly, parseDateOnly } from '../../../../core/utils/date.utils';
import { ExpensesSubNavComponent } from '../../components/expenses-sub-nav/expenses-sub-nav.component';

export interface LineCascadingLookups {
  areas: readonly FarmArea[];
  plantations: readonly Plantation[];
  cycles: readonly CropCycle[];
  stages: readonly CropCycleStage[];
}

@Component({
  selector: 'app-purchase-invoice-editor-page',
  standalone: true,
  providers: [provideNativeDateAdapter()],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatRadioModule,
    MatSelectModule,
    ExpensesSubNavComponent,
  ],
  templateUrl: './purchase-invoice-editor-page.component.html',
  styleUrl: './purchase-invoice-editor-page.component.scss',
})
export class PurchaseInvoiceEditorPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  private readonly invoiceService = inject(PurchaseInvoiceService);
  private readonly farmService = inject(FarmManagementService);
  private readonly supplierService = inject(SupplierService);
  private readonly expenseCategoryService = inject(ExpenseCategoryService);
  private readonly inventoryService = inject(InventoryService);
  private readonly expenseService = inject(ExpenseService);

  readonly isEdit = signal(false);
  readonly invoiceId = signal<string | null>(null);
  readonly isLoading = signal(false);
  readonly isSubmitting = signal(false);

  readonly suppliers = signal<readonly Supplier[]>([]);
  readonly farms = signal<readonly Farm[]>([]);
  readonly currencies = signal<readonly CurrencyItem[]>([]);
  readonly inventoryItems = signal<readonly InventoryItem[]>([]);
  readonly expenseCategories = signal<readonly ExpenseCategory[]>([]);

  // Array of cascading lookups for each line in lines FormArray
  readonly lineLookups = signal<LineCascadingLookups[]>([]);

  readonly form = this.fb.group({
    supplierId: ['', Validators.required],
    farmId: ['', Validators.required],
    supplierInvoiceNumber: ['', [Validators.required, Validators.maxLength(100)]],
    invoiceDate: [new Date() as Date | string | null, Validators.required],
    dueDate: [null as Date | string | null],
    currencyId: ['', Validators.required],
    paymentTerms: ['', Validators.maxLength(100)],
    taxAmount: [0, [Validators.min(0)]],
    otherCharges: [0, [Validators.min(0)]],
    discountAmount: [0, [Validators.min(0)]],
    notes: ['', Validators.maxLength(1000)],
    attachmentReference: ['', Validators.maxLength(500)],
    lines: this.fb.array([]),
  });

  get linesArray(): FormArray {
    return this.form.get('lines') as FormArray;
  }

  get lineFormGroups(): FormGroup[] {
    return this.linesArray.controls as FormGroup[];
  }

  get subtotal(): number {
    let sum = 0;
    for (const group of this.lineFormGroups) {
      sum += this.calculateLineAmount(group);
    }
    return sum;
  }

  get totalAmount(): number {
    const tax = Number(this.form.value.taxAmount) || 0;
    const charges = Number(this.form.value.otherCharges) || 0;
    const discount = Number(this.form.value.discountAmount) || 0;
    return Math.max(0, this.subtotal + tax + charges - discount);
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit.set(true);
      this.invoiceId.set(id);
    }

    this.loadLookups();
  }

  loadLookups(): void {
    this.isLoading.set(true);

    this.farmService.listFarms(1, 100, '', true).subscribe({
      next: (res) => this.farms.set(res.items),
    });

    this.supplierService.list(1, 100, '', true).subscribe({
      next: (res) => this.suppliers.set(res.items),
    });

    this.expenseCategoryService.list(1, 100, '', true).subscribe({
      next: (res) => this.expenseCategories.set(res.items),
    });

    this.inventoryService.listItems(1, 200, null, null, true).subscribe({
      next: (res) => this.inventoryItems.set(res.items),
    });

    this.expenseService.listCurrencies().subscribe({
      next: (res) => {
        this.currencies.set(res);
        // Auto-select INR if creating a new invoice and currency not yet selected
        if (!this.isEdit() && !this.form.value.currencyId) {
          const inr = res.find((c) => c.code.toUpperCase() === 'INR');
          if (inr) {
            this.form.patchValue({ currencyId: inr.id });
          }
        }
      },
    });

    // Listen to farmId changes -> reload farm areas for all lines
    this.form.controls.farmId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((farmId) => {
        this.onFarmChanged(farmId);
      });

    if (this.isEdit() && this.invoiceId()) {
      this.loadInvoiceForEdit(this.invoiceId()!);
    } else {
      this.isLoading.set(false);
      this.addLine(); // Add default line
    }
  }

  loadInvoiceForEdit(id: string): void {
    this.invoiceService
      .get(id)
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (invoice) => {
          if (invoice.status !== 'Draft') {
            this.snack.open('Only draft invoices can be edited.', 'Close', { duration: 5000 });
            this.router.navigate(['/expenses/invoices', id]);
            return;
          }

          this.form.patchValue({
            supplierId: invoice.supplierId,
            farmId: invoice.farmId,
            supplierInvoiceNumber: invoice.supplierInvoiceNumber,
            invoiceDate: parseDateOnly(invoice.invoiceDate),
            dueDate: parseDateOnly(invoice.dueDate),
            currencyId: invoice.currencyId,
            paymentTerms: invoice.paymentTerms || '',
            taxAmount: invoice.taxAmount,
            otherCharges: invoice.otherCharges,
            discountAmount: invoice.discountAmount,
            notes: invoice.notes || '',
            attachmentReference: invoice.attachmentReference || '',
          });

          // Clear existing lines & lookups
          this.linesArray.clear();
          this.lineLookups.set([]);

          // Populate lines
          if (invoice.lines && invoice.lines.length > 0) {
            invoice.lines.forEach((line) => {
              this.addLineFromResponse(line, invoice.farmId);
            });
          } else {
            this.addLine();
          }
        },
        error: (err) => {
          this.snack.open(getApiErrorMessage(err, 'Failed to load invoice.'), 'Close', { duration: 5000 });
          this.router.navigate(['/expenses/invoices']);
        },
      });
  }

  onFarmChanged(farmId: string | null): void {
    const currentLookups = [...this.lineLookups()];
    this.lineFormGroups.forEach((group, index) => {
      // Clear line operational linkages when farm changes
      group.patchValue({
        farmAreaId: '',
        plantationId: '',
        cropCycleId: '',
        cropCycleStageId: '',
      });
      group.get('farmAreaId')?.disable();
      group.get('plantationId')?.disable();
      group.get('cropCycleId')?.disable();
      group.get('cropCycleStageId')?.disable();

      currentLookups[index] = { areas: [], plantations: [], cycles: [], stages: [] };

      if (farmId) {
        group.get('farmAreaId')?.enable();
        this.loadAreasForLine(index, farmId);
      }
    });
    this.lineLookups.set(currentLookups);
  }

  addLine(): void {
    const index = this.linesArray.length;
    const farmId = this.form.value.farmId;

    const group = this.fb.group({
      lineType: ['InventoryItem' as PurchaseInvoiceLineType, Validators.required],
      inventoryItemId: [''],
      stockUnitId: [''],
      stockUnitCode: [''],
      stockUnitName: [''],
      quantity: [1],
      unitPrice: [0],
      expenseCategoryId: [''],
      amount: [0],
      description: ['', Validators.maxLength(500)],
      farmAreaId: [{ value: '', disabled: true }],
      plantationId: [{ value: '', disabled: true }],
      cropCycleId: [{ value: '', disabled: true }],
      cropCycleStageId: [{ value: '', disabled: true }],
    });

    // Add validator based on lineType
    this.setupLineTypeValidators(group);

    this.linesArray.push(group);

    const newLookups = [...this.lineLookups(), { areas: [], plantations: [], cycles: [], stages: [] }];
    this.lineLookups.set(newLookups);

    if (farmId) {
      group.get('farmAreaId')?.enable();
      this.loadAreasForLine(index, farmId);
    }
  }

  addLineFromResponse(line: any, farmId: string): void {
    const index = this.linesArray.length;

    const group = this.fb.group({
      lineType: [line.lineType as PurchaseInvoiceLineType, Validators.required],
      inventoryItemId: [line.inventoryItemId || ''],
      stockUnitId: [line.stockUnitId || ''],
      stockUnitCode: [line.stockUnitCode || ''],
      stockUnitName: [line.stockUnitName || ''],
      quantity: [line.quantity ?? 1],
      unitPrice: [line.unitPrice ?? 0],
      expenseCategoryId: [line.expenseCategoryId || ''],
      amount: [line.lineAmount ?? 0],
      description: [line.description || '', Validators.maxLength(500)],
      farmAreaId: [{ value: line.farmAreaId || '', disabled: !farmId }],
      plantationId: [{ value: line.plantationId || '', disabled: !line.farmAreaId }],
      cropCycleId: [{ value: line.cropCycleId || '', disabled: !line.plantationId }],
      cropCycleStageId: [{ value: line.cropCycleStageId || '', disabled: !line.cropCycleId }],
    });

    this.setupLineTypeValidators(group);
    this.linesArray.push(group);

    const newLookups = [...this.lineLookups(), { areas: [], plantations: [], cycles: [], stages: [] }];
    this.lineLookups.set(newLookups);

    if (farmId) {
      this.loadAreasForLine(index, farmId);
      if (line.farmAreaId) {
        this.loadPlantationsForLine(index, farmId, line.farmAreaId);
        if (line.plantationId) {
          this.loadCyclesForLine(index, farmId, line.farmAreaId, line.plantationId);
          if (line.cropCycleId) {
            this.loadStagesForLine(index, line.cropCycleId);
          }
        }
      }
    }
  }

  removeLine(index: number): void {
    if (this.linesArray.length <= 1) {
      this.snack.open('Invoice must have at least one line.', 'Close', { duration: 3000 });
      return;
    }
    this.linesArray.removeAt(index);
    const lookups = [...this.lineLookups()];
    lookups.splice(index, 1);
    this.lineLookups.set(lookups);
  }

  setupLineTypeValidators(group: FormGroup): void {
    const applyValidators = (type: string | null | undefined) => {
      const isInventory = type === 'InventoryItem';

      const inventoryItemIdCtrl = group.get('inventoryItemId');
      const quantityCtrl = group.get('quantity');
      const unitPriceCtrl = group.get('unitPrice');
      const expenseCategoryIdCtrl = group.get('expenseCategoryId');
      const amountCtrl = group.get('amount');

      if (isInventory) {
        inventoryItemIdCtrl?.setValidators([Validators.required]);
        quantityCtrl?.setValidators([Validators.required, Validators.min(0.0001)]);
        unitPriceCtrl?.setValidators([Validators.required, Validators.min(0)]);

        expenseCategoryIdCtrl?.clearValidators();
        amountCtrl?.clearValidators();
      } else {
        expenseCategoryIdCtrl?.setValidators([Validators.required]);
        amountCtrl?.setValidators([Validators.required, Validators.min(0.01)]);

        inventoryItemIdCtrl?.clearValidators();
        quantityCtrl?.clearValidators();
        unitPriceCtrl?.clearValidators();
      }

      inventoryItemIdCtrl?.updateValueAndValidity({ emitEvent: false });
      quantityCtrl?.updateValueAndValidity({ emitEvent: false });
      unitPriceCtrl?.updateValueAndValidity({ emitEvent: false });
      expenseCategoryIdCtrl?.updateValueAndValidity({ emitEvent: false });
      amountCtrl?.updateValueAndValidity({ emitEvent: false });
      group.updateValueAndValidity({ emitEvent: false });
    };

    group.get('lineType')?.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((type) => {
        applyValidators(type);
      });

    // Run for initial state
    applyValidators(group.get('lineType')?.value);
  }

  onInventoryItemSelect(index: number, itemId: string): void {
    const group = this.lineFormGroups[index];
    const item = this.inventoryItems().find((i) => i.id === itemId);
    if (item) {
      group.patchValue({
        stockUnitId: item.stockUnitId,
        stockUnitCode: item.stockUnitCode,
        stockUnitName: item.stockUnitName,
      });
    } else {
      group.patchValue({
        stockUnitId: '',
        stockUnitCode: '',
        stockUnitName: '',
      });
    }
  }

  // --- CASCADING OPERATIONAL LINKAGE HANDLERS ---
  onLineAreaChanged(index: number, areaId: string): void {
    const group = this.lineFormGroups[index];
    group.patchValue({
      plantationId: '',
      cropCycleId: '',
      cropCycleStageId: '',
    });
    group.get('plantationId')?.disable();
    group.get('cropCycleId')?.disable();
    group.get('cropCycleStageId')?.disable();

    this.updateLineLookup(index, { plantations: [], cycles: [], stages: [] });

    const farmId = this.form.value.farmId;
    if (areaId && farmId) {
      group.get('plantationId')?.enable();
      this.loadPlantationsForLine(index, farmId, areaId);
    }
  }

  onLinePlantationChanged(index: number, plantationId: string): void {
    const group = this.lineFormGroups[index];
    group.patchValue({
      cropCycleId: '',
      cropCycleStageId: '',
    });
    group.get('cropCycleId')?.disable();
    group.get('cropCycleStageId')?.disable();

    this.updateLineLookup(index, { cycles: [], stages: [] });

    const farmId = this.form.value.farmId;
    const areaId = group.get('farmAreaId')?.value;
    if (plantationId && farmId && areaId) {
      group.get('cropCycleId')?.enable();
      this.loadCyclesForLine(index, farmId, areaId, plantationId);
    }
  }

  onLineCropCycleChanged(index: number, cycleId: string): void {
    const group = this.lineFormGroups[index];
    group.patchValue({
      cropCycleStageId: '',
    });
    group.get('cropCycleStageId')?.disable();

    this.updateLineLookup(index, { stages: [] });

    if (cycleId) {
      group.get('cropCycleStageId')?.enable();
      this.loadStagesForLine(index, cycleId);
    }
  }

  private loadAreasForLine(index: number, farmId: string): void {
    this.farmService.listAreas(farmId, true).subscribe({
      next: (areas) => this.updateLineLookup(index, { areas: [...areas] }),
    });
  }

  private loadPlantationsForLine(index: number, farmId: string, areaId: string): void {
    this.farmService.listPlantations(1, 100, farmId, areaId).subscribe({
      next: (res) => this.updateLineLookup(index, { plantations: res.items }),
    });
  }

  private loadCyclesForLine(index: number, farmId: string, areaId: string, plantationId: string): void {
    this.farmService.listCycles(1, 100, farmId, areaId, plantationId).subscribe({
      next: (res) => this.updateLineLookup(index, { cycles: res.items }),
    });
  }

  private loadStagesForLine(index: number, cycleId: string): void {
    this.farmService.getCycleLifecycle(cycleId).subscribe({
      next: (res) => this.updateLineLookup(index, { stages: res.stages || [] }),
    });
  }

  private updateLineLookup(index: number, patch: Partial<LineCascadingLookups>): void {
    const lookups = [...this.lineLookups()];
    if (lookups[index]) {
      lookups[index] = { ...lookups[index], ...patch };
      this.lineLookups.set(lookups);
    }
  }

  calculateLineAmount(group: FormGroup): number {
    const type = group.get('lineType')?.value as PurchaseInvoiceLineType;
    if (type === 'InventoryItem') {
      const qty = Number(group.get('quantity')?.value) || 0;
      const price = Number(group.get('unitPrice')?.value) || 0;
      return qty * price;
    } else {
      return Number(group.get('amount')?.value) || 0;
    }
  }

  onSubmit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      this.snack.open('Please review missing or invalid form fields.', 'Close', { duration: 4000 });
      return;
    }

    this.isSubmitting.set(true);
    const val = this.form.getRawValue();

    const lines: CreatePurchaseInvoiceLineRequest[] = val.lines.map((l: any, idx: number) => ({
      lineType: l.lineType,
      inventoryItemId: l.lineType === 'InventoryItem' ? l.inventoryItemId || null : null,
      stockUnitId: l.lineType === 'InventoryItem' ? l.stockUnitId || null : null,
      quantity: l.lineType === 'InventoryItem' ? Number(l.quantity) || 0 : null,
      unitPrice: l.lineType === 'InventoryItem' ? Number(l.unitPrice) || 0 : 0,
      expenseCategoryId: l.lineType === 'NonInventoryExpense' ? l.expenseCategoryId || null : null,
      amount: l.lineType === 'NonInventoryExpense' ? Number(l.amount) || 0 : null,
      description: l.description ? l.description.trim() : null,
      farmAreaId: l.farmAreaId || null,
      plantationId: l.plantationId || null,
      cropCycleId: l.cropCycleId || null,
      cropCycleStageId: l.cropCycleStageId || null,
      sortOrder: idx,
    }));

    if (this.isEdit() && this.invoiceId()) {
      const updateReq: UpdatePurchaseInvoiceRequest = {
        supplierId: val.supplierId!,
        farmId: val.farmId!,
        supplierInvoiceNumber: val.supplierInvoiceNumber!.trim(),
        invoiceDate: formatDateOnly(val.invoiceDate)!,
        dueDate: formatDateOnly(val.dueDate),
        currencyId: val.currencyId!,
        paymentTerms: val.paymentTerms?.trim() || null,
        taxAmount: Number(val.taxAmount) || 0,
        otherCharges: Number(val.otherCharges) || 0,
        discountAmount: Number(val.discountAmount) || 0,
        notes: val.notes?.trim() || null,
        attachmentReference: val.attachmentReference?.trim() || null,
        lines,
      };

      this.invoiceService
        .updateDraft(this.invoiceId()!, updateReq)
        .pipe(
          finalize(() => this.isSubmitting.set(false)),
          takeUntilDestroyed(this.destroyRef),
        )
        .subscribe({
          next: (res) => {
            this.snack.open('Supplier invoice draft updated successfully.', 'Close', { duration: 3000 });
            this.router.navigate(['/expenses/invoices', res.id]);
          },
          error: (err) => {
            this.snack.open(getApiErrorMessage(err, 'Failed to update supplier invoice.'), 'Close', { duration: 5000 });
          },
        });
    } else {
      const createReq: CreatePurchaseInvoiceRequest = {
        supplierId: val.supplierId!,
        farmId: val.farmId!,
        supplierInvoiceNumber: val.supplierInvoiceNumber!.trim(),
        invoiceDate: formatDateOnly(val.invoiceDate)!,
        dueDate: formatDateOnly(val.dueDate),
        currencyId: val.currencyId!,
        paymentTerms: val.paymentTerms?.trim() || null,
        taxAmount: Number(val.taxAmount) || 0,
        otherCharges: Number(val.otherCharges) || 0,
        discountAmount: Number(val.discountAmount) || 0,
        notes: val.notes?.trim() || null,
        attachmentReference: val.attachmentReference?.trim() || null,
        lines,
      };

      this.invoiceService
        .createDraft(createReq)
        .pipe(
          finalize(() => this.isSubmitting.set(false)),
          takeUntilDestroyed(this.destroyRef),
        )
        .subscribe({
          next: (res) => {
            this.snack.open('Supplier invoice draft created successfully.', 'Close', { duration: 3000 });
            this.router.navigate(['/expenses/invoices', res.id]);
          },
          error: (err) => {
            this.snack.open(getApiErrorMessage(err, 'Failed to create supplier invoice.'), 'Close', { duration: 5000 });
          },
        });
    }
  }

  getCurrencySymbol(): string {
    const cid = this.form.value.currencyId;
    const c = this.currencies().find((x) => x.id === cid);
    return c ? c.symbol : '₹';
  }
}
