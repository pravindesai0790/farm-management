import { ChangeDetectionStrategy, Component } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { RouterLink } from "@angular/router";

@Component({
  selector: "app-forbidden-page",
  standalone: true,
  imports: [MatButtonModule, MatCardModule, MatIconModule, RouterLink],
  templateUrl: "./forbidden-page.component.html",
  styleUrl: "./forbidden-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ForbiddenPageComponent {}
