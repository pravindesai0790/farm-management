import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';
import { SystemService } from './system.service';

describe('SystemService', () => {
  let service: SystemService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [SystemService, provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(SystemService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('gets the system ping from the configured API endpoint', () => {
    const response = {
      message: 'Farm Management API is running',
      timestamp: '2026-09-01T00:00:00Z'
    };

    service.getPing().subscribe((ping) => {
      expect(ping).toEqual(response);
    });

    const request = httpTesting.expectOne(`${environment.apiUrl}/system/ping`);
    expect(request.request.method).toBe('GET');
    request.flush(response);
  });
});
