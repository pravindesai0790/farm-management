import { TestBed } from "@angular/core/testing";

import { AppComponent } from "./app.component";
import { appConfig } from "./app.config";

describe("AppComponent", () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [...(appConfig.providers ?? [])],
    }).compileComponents();
  });

  it("creates the application shell", () => {
    const fixture = TestBed.createComponent(AppComponent);

    expect(fixture.componentInstance).toBeTruthy();
  });
});
