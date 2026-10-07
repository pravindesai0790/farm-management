import { ComponentFixture, TestBed } from "@angular/core/testing";
import { provideRouter } from "@angular/router";
import { By } from "@angular/platform-browser";
import { PermissionService } from "../../core/auth/permission.service";
import { ExpenseCard, ExpensesPageComponent } from "./expenses-page.component";

describe("ExpensesPageComponent", () => {
  let component: ExpensesPageComponent;
  let fixture: ComponentFixture<ExpensesPageComponent>;
  let mockPermissionService: jasmine.SpyObj<PermissionService>;

  beforeEach(async () => {
    mockPermissionService = jasmine.createSpyObj("PermissionService", ["has"]);
    mockPermissionService.has.and.returnValue(true);

    await TestBed.configureTestingModule({
      imports: [ExpensesPageComponent],
      providers: [
        provideRouter([]),
        { provide: PermissionService, useValue: mockPermissionService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ExpensesPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create the component", () => {
    expect(component).toBeTruthy();
  });

  it("should define all 7 expense sub-link cards", () => {
    expect(component.cards.length).toBe(7);
    const cardIds = component.cards.map((c) => c.id);
    expect(cardIds).toContain("direct-expenses");
    expect(cardIds).toContain("supplier-invoices");
    expect(cardIds).toContain("supplier-payments");
    expect(cardIds).toContain("supplier-balances");
    expect(cardIds).toContain("expense-reports");
    expect(cardIds).toContain("suppliers");
    expect(cardIds).toContain("expense-categories");
  });

  it("should allow access when card has no required permission", () => {
    const card: ExpenseCard = {
      id: "test",
      title: "Test",
      category: "Test",
      description: "Test",
      icon: "test",
      status: "AVAILABLE",
      highlights: [],
    };
    expect(component.canAccess(card)).toBeTrue();
  });

  it("should delegate to permissionService.has when card has required permission", () => {
    const card: ExpenseCard = {
      id: "test",
      title: "Test",
      category: "Test",
      description: "Test",
      icon: "test",
      status: "AVAILABLE",
      highlights: [],
      requiredPermission: "Expense.View",
    };
    mockPermissionService.has.and.callFake((perm) => perm === "Expense.View");
    expect(component.canAccess(card)).toBeTrue();
    expect(mockPermissionService.has).toHaveBeenCalledWith("Expense.View");

    mockPermissionService.has.and.returnValue(false);
    expect(component.canAccess(card)).toBeFalse();
  });

  it("should render 7 mat-card elements in the grid", () => {
    const cards = fixture.debugElement.queryAll(By.css("mat-card.activity-card"));
    expect(cards.length).toBe(7);
  });

  it("should render Open buttons for accessible available cards", () => {
    const openButtons = fixture.debugElement.queryAll(
      By.css(".action-buttons a[mat-flat-button]")
    );
    expect(openButtons.length).toBe(7);
  });

  it("should render Add buttons for cards with createRoute when permission granted", () => {
    mockPermissionService.has.and.returnValue(true);
    fixture.detectChanges();

    const addButtons = fixture.debugElement.queryAll(
      By.css(".action-buttons a[mat-stroked-button]")
    );
    // Supplier Invoices and Supplier Payments have createRoute
    expect(addButtons.length).toBe(2);
  });

  it("should hide Add button when user lacks createPermission", () => {
    mockPermissionService.has.and.callFake((perm) => !perm.includes("Create"));
    const localFixture = TestBed.createComponent(ExpensesPageComponent);
    localFixture.detectChanges();

    const addButtons = localFixture.debugElement.queryAll(
      By.css(".action-buttons a[mat-stroked-button]")
    );
    expect(addButtons.length).toBe(0);
  });
});
