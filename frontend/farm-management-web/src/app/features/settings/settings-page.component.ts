import { ChangeDetectionStrategy, Component } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { RouterLink, RouterOutlet } from "@angular/router";

@Component({
  selector: "app-settings-page",
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    RouterLink,
    RouterOutlet,
  ],
  templateUrl: "./settings-page.component.html",
  styleUrl: "./settings-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SettingsPageComponent {}
