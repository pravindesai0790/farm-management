import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PagePlaceholderComponent } from '../../shared/components/page-placeholder/page-placeholder.component';
@Component({ selector: 'app-activities-page', standalone: true, imports: [PagePlaceholderComponent], template: '<app-page-placeholder title="Activities" description="Planned and completed farm work will be tracked here." icon="event_note" />', changeDetection: ChangeDetectionStrategy.OnPush })
export class ActivitiesPageComponent {}
