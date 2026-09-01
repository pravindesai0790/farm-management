import { ChangeDetectionStrategy, Component } from '@angular/core';

import { PagePlaceholderComponent } from '../../shared/components/page-placeholder/page-placeholder.component';

@Component({
  selector: 'app-crops-page',
  standalone: true,
  imports: [PagePlaceholderComponent],
  template: '<app-page-placeholder title="Crops" description="Crop master data and varieties will be managed here." icon="grass" />',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CropsPageComponent {}
