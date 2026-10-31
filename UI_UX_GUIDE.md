# Farm Management UI/UX Design System

This file is the single source of truth for the Angular application's visual rules. Shared design tokens and generic component styles belong in `frontend/farm-management-web/src/styles.scss`. Feature styles may define workflow-specific layout, but must not redefine generic colors, typography, controls, cards, tables, status pills, or dialog shells. Preserve routes, permissions, validation, API contracts, and business behavior during visual work.

## Design principles

- Build a compact, calm, readable operational interface. Keep information density high without making controls difficult to use.
- Use the forest-green brand accent sparingly for primary actions, active navigation, and links. Use semantic colors only to communicate state.
- Prefer shared classes and CSS custom properties over literal values. Exceptions require a feature-specific reason.
- Support desktop, tablet, and phone layouts. Never rely on color alone to convey meaning; preserve visible keyboard focus and accessible names.

## Tokens

Define these once under `:root` in `styles.scss`; components should consume the variables. Existing palette values are canonical.

| Token | Value | Purpose |
|---|---|---|
| `--app-background` | `#f4f6f4` | App canvas |
| `--app-surface` | `#ffffff` | Cards, tables, dialogs |
| `--app-surface-subtle` | `#eef3ef` | Soft panels and table headers |
| `--app-ink` | `#19261f` | Headings and strong text |
| `--app-text` | `#243329` | Body text |
| `--app-muted` | `#5e7064` | Supporting text |
| `--app-border` | `#d8e2da` | Component borders |
| `--app-border-light` | `#e7ede8` | Dividers |
| `--app-primary` | `#2d6e4b` | Primary actions and links |
| `--app-primary-hover` | `#24583c` | Primary hover/pressed |
| `--app-primary-light` | `#e8f3ed` | Brand tint |
| `--app-success` / `-bg` | `#176b3a` / `#e8f3ed` | Success foreground/background |
| `--app-info` / `-bg` | `#1769aa` / `#eaf3fb` | In-progress information |
| `--app-warning` / `-bg` | `#805600` / `#fff5dc` | Planned and caution |
| `--app-danger` / `-bg` | `#a52b2b` / `#fceeee` | Errors and destructive state |
| `--app-neutral` / `-bg` | `#526158` / `#eef1ef` | Draft, inactive, archived |
| `--app-harvest` / `-bg` | `#813d70` / `#f7edf5` | Harvested state |
| `--app-radius-sm` | `6px` | Controls and badges |
| `--app-radius` | `10px` | Cards and containers |
| `--app-radius-lg` | `12px` | Dialogs and prominent cards |

Use a 4px spacing scale: `4, 8, 12, 16, 20, 24, 32px`. Use the system sans-serif stack already configured in the Material theme. Typography: page title 22–24px/700; section heading 16–18px/600; body and controls 14px; supporting text 12–13px; table headers 12px/600; line-height 1.3–1.5. Numeric values use tabular figures.

## Layout and components

### Shell and page headers

- Keep the top navigation compact and stable. Page content uses responsive gutters (24px desktop, 16px tablet, 12px phone) and a maximum readable width of 1440px; wide operational tables may fill available width.
- Use `.section-heading` / `.page-header-strip` with title, optional short description, and a right-aligned action group. Stack actions below the title on narrow screens.
- Back navigation is a quiet text link above detail/editor titles. Avoid duplicate back controls.

### Buttons and actions

- One primary CTA per view. Use flat primary for commit/create/start, outlined for secondary workflows, text for cancel/back, and warn only for destructive actions after confirmation.
- Standard heights: page actions 40px, form/dialog actions 36px, table icon controls 32px (with accessible label and tooltip). On touch layouts, targets should be at least 40px.
- Keep labels concise; include a visible loading state and disable duplicate submission while pending. Show visible focus; do not use color alone to indicate disabled or destructive state.

### Forms

- Use Angular Material density `-1` and outlined fields. Add `subscriptSizing="dynamic"` to all filter/search toolbars, inline selectors, and dialog forms. Use it on other forms when layout permits.
- Use `.form-grid` with two equal columns and `.wide` for full-width controls; collapse to one column below 640px. Keep field gaps at 12–16px.
- Use `.form-card` for standalone editors, with a readable max width around 880px. Group long forms with concise section headings. Keep labels, hints, required indicators, validation, and error text consistent.
- Do not create page-specific height/margin hacks to compensate for Material subscript space.

### Cards and metrics

- Standard cards use a white surface, 1px `--app-border`, `--app-radius`, and a subtle shadow. Default content padding is 16–20px; form cards use 20–24px; compact KPI cards use 12–16px.
- KPI cards share one hierarchy: muted label, prominent 20–24px tabular value, then supporting text. Use a consistent icon size and restrained semantic accent.

### Tables

- Use `.table-wrap` and `table.mat-mdc-table.dense-table`. Header height is about 40px and body rows 40–44px. Use 12px uppercase headers, 14px cells, subtle row separators, and a restrained hover/focus background.
- Right-align numeric columns and use tabular figures; display units/currency. Keep action columns right-aligned with labeled icon buttons. Allow horizontal scrolling on narrow screens rather than collapsing important data.
- Use compact paginator styling and provide clear loading, empty, and error states.

### Statuses

Use `.status-pill` with text and a dot or other shape cue. Use one mapping everywhere: active entity and completed = success; in progress = info; planned/scheduled = warning; draft = neutral; cancelled/terminated/error = danger; inactive/archived = neutral; harvested = harvest. Skipped is neutral unless the workflow explicitly treats it as a warning. Do not map “draft” and “scheduled” to the same state. Maintain readable contrast.

### Dialogs

- Use shared small (about 480px), medium (about 640px), and large (up to 920px) dialog variants, each constrained to `min(92vw, variant width)` and a usable viewport height. Large is for dense workflows such as invoice receiving, not simple editors.
- Standardize title, body, and footer spacing; right-align actions, put Cancel before the commit action, and use warn styling for destructive confirmation. Dialog forms use dynamic subscript sizing. Keep focus management and Escape/backdrop behavior consistent with the workflow.

### Accessibility and states

- Preserve semantic headings and labels, keyboard operation, visible focus, sufficient contrast, and accessible names for icon-only controls. Pair status colors with text/icons.
- Every data view must define loading, empty, error, and populated states. Errors must explain recovery where possible; loading should not cause large layout shifts.

## Angular and styling rules

- Keep generic styles in `styles.scss`; feature SCSS is for feature-specific structure only. Do not copy shared button, badge, table, card, dialog, spacing, or palette declarations into feature styles.
- Use standalone component imports for every Material component, directive, and pipe referenced in a template. Keep permission checks and form behavior intact.
- Avoid introducing new aliases for existing tokens or raw hex colors for shared elements. Add/adjust tokens here and in the global stylesheet together.

## Migration and acceptance checklist

- [ ] Use the canonical tokens, typography, spacing, and shared component patterns above.
- [ ] Filters, inline selectors, and dialog fields use dynamic subscript sizing.
- [ ] Tables use the shared wrapper and dense-table pattern; numeric and action columns follow the rules.
- [ ] Dialogs use a size variant and shared header/body/footer treatment.
- [ ] Responsive layout, keyboard focus, labels, validation, and loading/empty/error states are checked.
- [ ] Permissions, routes, validations, API contracts, and business workflows are unchanged.
- [ ] Run `npm run build` from `frontend/farm-management-web` and review representative screens at desktop and narrow widths.
