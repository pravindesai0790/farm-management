import { ChangeDetectionStrategy, Component, input } from "@angular/core";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";

@Component({
  selector: "app-page-placeholder",
  standalone: true,
  imports: [MatCardModule, MatIconModule],
  templateUrl: "./page-placeholder.component.html",
  styleUrl: "./page-placeholder.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PagePlaceholderComponent {
  readonly title = input.required<string>();
  readonly description = input.required<string>();
  readonly icon = input.required<string>();
}
