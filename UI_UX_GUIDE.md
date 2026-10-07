# UI/UX Design System & Implementation Guide

This guide establishes the mandatory UI and UX standards for the **Farm Management Application**. All developers and AI agents implementing new features, pages, components, dialogs, or refactoring existing screens **MUST** adhere to these rules to ensure a modern, clean, compact, consistent, and desktop-efficient enterprise SaaS experience.

---

## 1. Core Design Philosophy

### 1.1 The Golden Principle
> **Modern + Compact + Clean + Consistent + Readable**

The Farm Management application is an enterprise operational tool used daily by farm owners, agronomists, and supervisors. It must prioritize **information density, visual clarity, and fast task execution** over oversized, overly spacious, or decorative aesthetics.

### 1.2 Core Rules
1. **Compact but not cramped**: Remove unnecessary whitespace, empty padding, and oversized controls while preserving readable line-heights, comfortable click/touch targets, and accessible contrast.
2. **Desktop-First Enterprise Density**: Maximize useful screen area so users can review tabular data, KPI metrics, and operational cards without excessive scrolling.
3. **Medium-Sized Controls**: Standardize all inputs, selects, pickers, and buttons to medium enterprise dimensions. Never use default tall (56px) fields in toolbars, filters, or dialogs.
4. **Unified Visual Language**: Every page (Farm, Farm Area, Plantation, Crop Cycle, Inventory, Labor, etc.) must look and feel like part of a single, coherent application.
5. **Zero Business Logic Regressions**: UI/UX work must never modify or compromise existing validations, permissions (`permissionService.has(...)`), reactive form rules, API contracts, routes, or backend workflows.

---

## 2. Typography System

### 2.1 Font Family
Always use the standardized modern system sans-serif font stack. **Never use serif fonts (e.g. Georgia) anywhere in the application.**

```scss
font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", "Inter", Roboto, Helvetica, Arial, sans-serif;
```

### 2.2 Typographic Hierarchy & Scale

| Element | Font Size | Font Weight | Line Height | Color Token / Description |
| :--- | :--- | :--- | :--- | :--- |
| **Page Title** (`h2`) | `1.35rem` (21–22px) | `700` (Bold) | `1.2` | `var(--app-text, #1e293b)` |
| **Modal / Dialog Title** | `1.15rem` (18–19px) | `600` (Semi-bold) | `1.2` | Contextual (Primary, Red for Warn) |
| **Section Title** (`h3`) | `0.95rem` (15–16px) | `600` (Semi-bold) | `1.3` | `var(--app-text, #1e293b)` |
| **Form Section Heading** | `0.85rem` (13–14px) | `600` (Semi-bold) | `1.2` | `var(--app-text, #1e293b)` with icon |
| **Eyebrow / Category Tag** | `0.725rem`–`0.75rem` | `600` (Semi-bold) | `1.0` | Uppercase, `letter-spacing: 0.05em` |
| **Form Labels** | `0.85rem` (13–14px) | `500` (Medium) | `1.2` | Angular Material default (`#64748b`) |
| **Body / Input Text** | `0.85rem`–`0.875rem` | `400` / `500` | `1.4` | `var(--app-text, #1e293b)` |
| **Table Header** (`th`) | `0.75rem` (12px) | `600` (Semi-bold) | `1.0` | Uppercase, `letter-spacing: 0.05em`, `#64748b` |
| **Table Cell** (`td`) | `0.85rem` (13–14px) | `400` / `500` | `1.3` | `#1e293b` (primary) / `#64748b` (meta) |
| **Status Badge Text** | `0.725rem` (11.5px) | `600` (Semi-bold) | `1.0` | Color matched with badge state |
| **Supporting / Muted Text**| `0.75rem`–`0.8rem` | `400` (Regular) | `1.3` | `var(--app-muted, #64748b)` |

---

## 3. Color Palette & Semantic Tokens

### 3.1 CSS Variables (`src/styles.scss`)
Use CSS custom properties for theming consistency:

```scss
:root {
  --app-primary: #15803d;         // Farm green (Primary CTA, links, active highlights)
  --app-primary-hover: #166534;   // Darker green
  --app-primary-light: #ecfdf5;   // Soft mint tint for active badges/tags
  --app-text: #1e293b;            // Dark slate text
  --app-muted: #64748b;           // Secondary/muted text
  --app-border: #e2e8f0;          // Standard subtle card/table border
  --app-border-light: #f1f5f9;    // Ultra-light row/section dividers
  --app-surface: #ffffff;         // Card & table background
  --app-surface-variant: #f8fafc; // Table headers, light tags, subtle panels
  --app-surface-hover: rgba(0, 0, 0, 0.02); // Table row hover
  --app-radius-sm: 6px;           // Standard border radius (buttons, badges, inputs)
  --app-radius-md: 8px;           // Card & container border radius
}
```

### 3.2 Semantic Status Palette
Used for badges, status pills, alerts, and state indicators:

| State | Background | Text Color | Dot / Accent Color | Standard Usage |
| :--- | :--- | :--- | :--- | :--- |
| **Active / In Progress** | `#ecfdf5` | `#047857` | `#10b981` | Active cycles, active plantations, workers on-duty |
| **Planned / Scheduled** | `#e0f2fe` | `#0369a1` | `#0284c7` | Planned cycles, scheduled stages, drafts |
| **Completed** | `#f0fdf4` | `#15803d` | `#22c55e` | Completed cycles, finished stages, reconciled items |
| **Harvested / Warning** | `#fef3c7` | `#b45309` | `#f59e0b` | Harvested stage, pending reviews, allocation warnings |
| **Terminated / Inactive** | `#f1f5f9` | `#475569` | `#94a3b8` | Terminated plantations, inactive plots, retired items |
| **Cancelled / Error** | `#fef2f2` | `#b91c1c` | `#ef4444` | Cancelled cycles, error alerts, destructive confirmations |

---

## 4. Spacing, Shell & Layout Guidelines

### 4.1 Global Application Shell (`main-layout`)
- **Topbar**: Fixed height `54px`.
- **Main Container Shell**: Padding `20px 28px 36px` (tightened from legacy 72px/56px paddings).
- **Max Content Width**: `1400px` (or `100%` width with fluid layout on widescreen monitors).

### 4.2 Standard Page Header (`.section-heading` / `.page-header-strip`)
Every top-level page and detail page must use a structured, consistent header:

```html
<div class="section-heading">
  <div>
    <p class="eyebrow">Category / Module</p>
    <h2>Page Title</h2>
    <p class="subtitle">Brief 1-sentence description of the page purpose.</p>
  </div>
  @if (permissionService.has("Entity.Create")) {
    <a mat-flat-button color="primary" routerLink="/entity/new">
      <mat-icon>add</mat-icon> Add Entity
    </a>
  }
</div>
```

### 4.3 Back Navigation Bar (`.back-link-bar`)
All detail, editor, and sub-pages must include a clean back link bar above the header:

```html
<div class="back-link-bar">
  <a mat-button routerLink="/parent-route">
    <mat-icon>arrow_back</mat-icon> Back to Entities
  </a>
</div>
```

---

## 5. Form Controls & Form Layouts

### 5.1 Material Density Setting
Angular Material is configured with `density: (scale: -1)` globally in `src/styles.scss`:
```scss
@include mat.all-component-densities(-1);
```
**Never override density to 0 or positive scales locally.**

### 5.2 Dynamic Subscript Sizing (CRITICAL)
- **Always specify `subscriptSizing="dynamic"` on**:
  - Filter bars and search toolbars
  - Detail page inline selectors (e.g. status change dropdowns)
  - Modal dialog forms
- **Why**: Default Angular Material fields reserve `22px` below the control for hint/error text even when none is present. `subscriptSizing="dynamic"` collapses this space, ensuring controls align cleanly with 36px buttons.
- **NEVER use the legacy height hack**:
  ```scss
  // ANTI-PATTERN: DO NOT USE
  .clear-btn {
    height: 56px;
    margin-bottom: 22px;
  }
  ```
  Instead, use `subscriptSizing="dynamic"` on the `<mat-form-field>` and set the button height to `36px`.

### 5.3 Filter Rows (`.filters-row`)
```html
<div class="filters-row">
  <mat-form-field appearance="outline" subscriptSizing="dynamic" class="filter-item">
    <mat-label>Filter Field</mat-label>
    <mat-select [value]="filterValue()" (valueChange)="onFilterChange($event)">
      <mat-option value="">All</mat-option>
      <!-- options -->
    </mat-select>
  </mat-form-field>

  @if (hasActiveFilters()) {
    <button mat-stroked-button class="clear-filter-btn" (click)="clearFilters()">
      <mat-icon>clear</mat-icon> Clear Filters
    </button>
  }
</div>
```

### 5.4 Form Cards & Layout (`.form-card`, `.form-grid`)
- Form containers must use `.form-card` with max-width `840px–900px` centered on the page.
- Fields must be laid out in a 2-column grid (`.form-grid`) with `.col-span-2` for full-width fields (titles, textareas).
- Sub-sections must have clear section headings with icons (`.form-section-title`).

```html
<mat-card class="form-card">
  <mat-card-content>
    <form [formGroup]="form" (ngSubmit)="submit()">
      <!-- Form Sub-section -->
      <div class="form-section-title">
        <mat-icon>domain</mat-icon>
        <span>Master Information</span>
      </div>

      <div class="form-grid">
        <mat-form-field appearance="outline">
          <mat-label>Field 1</mat-label>
          <input matInput formControlName="field1" />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Field 2</mat-label>
          <input matInput formControlName="field2" />
        </mat-form-field>

        <mat-form-field appearance="outline" class="col-span-2">
          <mat-label>Description & Notes</mat-label>
          <textarea matInput rows="3" formControlName="description"></textarea>
        </mat-form-field>
      </div>

      <div class="form-actions">
        <a mat-button routerLink="/cancel-route">Cancel</a>
        <button mat-flat-button color="primary" [disabled]="isSubmitting()">
          @if (isSubmitting()) { <mat-spinner diameter="16" /> }
          Save Changes
        </button>
      </div>
    </form>
  </mat-card-content>
</mat-card>
```

---

## 6. Data Tables & Presentation

### 6.1 Dense Table Specification
All tables must use the `.table-wrap` container and `.dense-table` class:

```html
<div class="table-wrap">
  <table mat-table [dataSource]="items()" class="dense-table">
    <!-- Columns -->
    <tr mat-header-row *matHeaderRowDef="columns"></tr>
    <tr mat-row *matRowDef="let row; columns: columns" class="interactive-row"></tr>
  </table>
</div>
```

### 6.2 Table Standards
1. **Row Height**: Target `40px–44px` per row (`padding: 10px 14px`).
2. **Interactive Hover**: Rows should have `.interactive-row` for subtle hover highlight (`rgba(0,0,0,0.02)`).
3. **Primary Link**: The first primary column (e.g. Farm Name, Cycle Name, Plantation Name) must be an anchor link leading to the details view with bold text and primary green hover.
4. **Numeric Columns**:
   - MUST be right-aligned (`class="text-right"` on both `th` and `td`).
   - MUST use tabular numbers (`font-variant-numeric: tabular-nums`).
   - MUST display the measurement unit or currency symbol.
5. **Actions Column**:
   - Right-aligned.
   - Use a 30px stroked/text "View" button plus 32px icon buttons for Edit/Actions with `matTooltip`.
6. **Compact Paginator**: Always add `class="compact-paginator"` to `<mat-paginator>`.

---

## 7. Status Badges & Pills

### 7.1 Unified `.status-pill` Pattern
All status values across the application must use the unified `.status-pill` structure with live indicator dots:

```html
<span class="status-pill status-pill--active">
  <span class="status-pill__dot"></span>
  Active
</span>
```

### 7.2 Available Status Modifiers
- `status-pill--active` (Active, In Progress, On Duty)
- `status-pill--planned` (Planned, Scheduled, Draft)
- `status-pill--completed` (Completed, Reconciled, Settled)
- `status-pill--harvested` (Harvested, In Review)
- `status-pill--terminated` / `status-pill--inactive` (Terminated, Inactive, Fallow)
- `status-pill--cancelled` (Cancelled, Rejected, Voided)

---

## 8. Buttons & Actions Hierarchy

| Button Type | Angular Material Syntax | Height | Padding | Usage Rule |
| :--- | :--- | :--- | :--- | :--- |
| **Primary CTA** | `<button mat-flat-button color="primary">` | `36–38px` | `0 16px` | Single primary action per view (Save, Create, Submit, Start) |
| **Secondary Action** | `<button mat-stroked-button>` | `36px` | `0 14px` | Secondary workflows (Filter, Export, Back, Reopen) |
| **Subtle Action** | `<button mat-button>` | `32–36px` | `0 12px` | Cancel, dismiss, non-intrusive operations |
| **Table Action** | `<a mat-button class="action-btn">` | `30px` | `0 10px` | In-table View / Review action |
| **Icon Action** | `<a mat-icon-button class="action-icon-btn">` | `32px` | `0` | Edit, More options, Delete (always include `matTooltip`) |
| **Destructive Action**| `<button mat-flat-button color="warn">` | `36px` | `0 16px` | Terminate, Delete, Cancel (requires confirmation modal) |

---

## 9. Modal Dialogs

### 9.1 Dialog Rules
1. **Compact Dimensions**: Set explicit width appropriate for the task (typically `480px` to `560px`). Avoid oversized 800px+ dialogs for simple forms.
2. **Clear Dialog Header**:
   - Include a contextual icon (`warning_amber` for warnings/destructions, `info` for details, `edit_calendar` for dates).
   - Use `h2[mat-dialog-title]` with `font-size: 1.15rem; font-weight: 600;`.
3. **Form Fields**: Apply `subscriptSizing="dynamic"` on every field in dialogs.
4. **Action Footer**:
   - Right-aligned with `<mat-dialog-actions align="end">`.
   - Padding `12px 24px 18px`.
   - Clear Cancel / Confirm buttons with 36px height.

---

## 10. Detail Pages & 360 Views

### 10.1 KPI & Metric Cards (`.metrics-grid`, `.metric-card`)
- Layout: 3 to 5 columns on desktop (`grid-template-columns: repeat(auto-fit, minmax(200px, 1fr))`), collapsing gracefully on tablet/mobile.
- Card padding: `12px 14px` or `14px 16px`.
- Layout structure:
  - Header row: Uppercase label (`0.75rem`) + contextual 16px icon wrapper.
  - Value row: Large prominent number (`1.25rem–1.45rem`, bold, tabular numbers) + unit symbol.
  - Subtext row: Subtle supporting text (`0.725rem`, `#64748b`).

### 10.2 Tabs (`mat-tab-group`)
- **Global Styling**: Built globally in `styles.scss` via `.mat-mdc-tab-group` so all modules maintain unified visual behavior.
- **Tab Dimensions**: `42px` height per tab (`.mat-mdc-tab { height: 42px; min-height: 42px; }`). Never constrain `.mat-mdc-tab-header` with fixed height directly, as it clips the MDC indicator underline.
- **Active Tab Highlight**:
  - Text: Primary green (`var(--app-primary, #2d6e4b)`), `font-weight: 600`.
  - Icon: Primary green (`var(--app-primary, #2d6e4b)`), `18px` size.
  - Ink Indicator: Prominent `3px` solid underline in `var(--app-primary, #2d6e4b)` with `border-radius: 3px 3px 0 0`.
- **Inactive Tabs**:
  - Text: Muted (`var(--app-muted, #5e7064)`), `font-weight: 500`.
  - Icon: Muted (`var(--app-muted, #5e7064)`).
  - Hover: Text and icon transition smoothly to `--app-ink` (`#19261f`).
- **Tab Content**: `padding-top: 14px;` (or `tab-content-container` spacing).

---

## 11. Angular Implementation & Quality Rules

### 11.1 Angular 20 Standalone Component Imports
When using Angular Material components or common pipes, **always verify that the module is imported in that standalone component's `imports: [...]` array**:
- `<mat-icon>` $\rightarrow$ `MatIconModule` (from `@angular/material/icon`)
- `matTooltip="..."` $\rightarrow$ `MatTooltipModule` (from `@angular/material/tooltip`)
- `| number` pipe $\rightarrow$ `DecimalPipe` (from `@angular/common`)
- `| date` pipe $\rightarrow$ `DatePipe` (from `@angular/common`)
- `<mat-form-field>` $\rightarrow$ `MatFormFieldModule`
- `<mat-select>` $\rightarrow$ `MatSelectModule`
- `<input matInput>` $\rightarrow$ `MatInputModule`

### 11.2 Style Budget Management
Angular CLI enforces an `8.00 kB` component style budget.
- **Do NOT duplicate generic CSS** (e.g. status pills, table styles, button sizes) in individual component `.scss` files.
- **Rely on shared utility classes** defined in `src/styles.scss` (`.section-heading`, `.table-wrap`, `.dense-table`, `.status-pill`, `.form-card`, `.form-grid`, etc.).
- Keep per-component `.scss` concise, focused only on feature-unique layout requirements.

### 11.3 Verification Commands
Before submitting any frontend changes, always execute:
```powershell
# From frontend/farm-management-web
npm run build

# From backend/
dotnet test FarmManagement.sln
```
Ensure **0 compilation errors** and **100% passing test suite**.

---

## 12. Pre-Merge UI/UX Checklist

Use this checklist before finalizing any UI feature:

- [ ] **Typography**: No serif fonts; page titles $\le$ `1.35rem`; section titles $\le$ `1.15rem`.
- [ ] **Control Dimensions**: All inputs and selects are medium-sized; no 56px height hacks.
- [ ] **Subscript Sizing**: `subscriptSizing="dynamic"` applied on all filter fields, search bars, and dialogs.
- [ ] **Whitespace & Density**: Shell padding is `20px 28px 36px`; no excessive empty gaps.
- [ ] **Multi-Column Forms**: Form fields arranged in logical 2-column grid (`.form-grid`); max-width $\le$ 900px.
- [ ] **Tables**: Dense row height (`40–44px`); tabular right-aligned numeric data; hover highlights.
- [ ] **Status Badges**: Standard `.status-pill` with dot indicator and appropriate semantic color.
- [ ] **Action Buttons**: Standardized heights (36px for primary/secondary, 30–32px for table rows); tooltips on icon buttons.
- [ ] **Dialogs**: Clean header with icon; compact field spacing; explicit max-width (480–560px).
- [ ] **Responsiveness**: Forms, tables, and metric strips adapt cleanly on tablet and mobile widths.
- [ ] **Permissions & Business Logic**: All `permissionService.has(...)` checks, validations, and routes preserved.
- [ ] **Build Verification**: `npm run build` succeeds with 0 errors.
