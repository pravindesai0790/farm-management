import { ChangeDetectionStrategy, Component } from '@angular/core';

import { PagePlaceholderComponent } from '../../shared/components/page-placeholder/page-placeholder.component';

@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [PagePlaceholderComponent],
  template: '<app-page-placeholder title="Settings" description="Platform preferences and configuration will be available here." icon="settings" />',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsPageComponent {}
