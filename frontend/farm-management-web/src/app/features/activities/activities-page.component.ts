import { ChangeDetectionStrategy, Component } from "@angular/core";
import { PagePlaceholderComponent } from "../../shared/components/page-placeholder/page-placeholder.component";
@Component({
  selector: "app-activities-page",
  standalone: true,
  imports: [PagePlaceholderComponent],
  templateUrl: "./activities-page.component.html",
  styleUrl: "./activities-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActivitiesPageComponent {}
