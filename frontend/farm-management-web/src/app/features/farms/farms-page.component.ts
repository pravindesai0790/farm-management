import { ChangeDetectionStrategy, Component } from '@angular/core';

import { PagePlaceholderComponent } from '../../shared/components/page-placeholder/page-placeholder.component';

@Component({
  selector: 'app-farms-page',
  standalone: true,
  imports: [PagePlaceholderComponent],
  template: '<app-page-placeholder title="Farms" description="Your farms and growing areas will have a home here." icon="landscape" />',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FarmsPageComponent {}
