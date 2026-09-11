import { ChangeDetectionStrategy, Component } from "@angular/core";
import { PagePlaceholderComponent } from "../../../shared/components/page-placeholder/page-placeholder.component";

@Component({
  selector: "app-contractors-page",
  standalone: true,
  imports: [PagePlaceholderComponent],
  template: `
    <app-page-placeholder
      title="Contractors"
      description="Manage third-party labor contractors, contact details, and contracted farm workforce. Coming in Phase 3.1."
      icon="business_center"
    />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ContractorsPageComponent {}
