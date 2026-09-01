import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { SystemPingResponse } from '../models/system-ping-response.model';

@Injectable({ providedIn: 'root' })
export class SystemService {
  private readonly http = inject(HttpClient);
  private readonly endpoint = `${environment.apiUrl}/system/ping`;

  getPing(): Observable<SystemPingResponse> {
    return this.http.get<SystemPingResponse>(this.endpoint);
  }
}
