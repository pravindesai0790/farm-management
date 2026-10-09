import { ComponentFixture, TestBed, fakeAsync, tick } from "@angular/core/testing";
import { MatDialog } from "@angular/material/dialog";
import { MatSnackBar } from "@angular/material/snack-bar";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { ActivatedRoute } from "@angular/router";
import { of } from "rxjs";
import { PermissionService } from "../../../core/auth/permission.service";
import {
  ApplicationMethodItem,
  ProductTypeItem,
  TargetItem,
} from "../../../core/sprays/spray-master-data.models";
import { SprayMasterDataService } from "../../../core/sprays/spray-master-data.service";
import { SprayMasterDataPageComponent } from "./spray-master-data-page.component";

describe("SprayMasterDataPageComponent", () => {
  let component: SprayMasterDataPageComponent;
  let fixture: ComponentFixture<SprayMasterDataPageComponent>;
  let mockMasterService: jasmine.SpyObj<SprayMasterDataService>;
  let mockDialog: jasmine.SpyObj<MatDialog>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;
  let mockPermissionService: jasmine.SpyObj<PermissionService>;

  const sampleProductTypes: ProductTypeItem[] = [
    {
      id: "pt-1",
      code: "FUNG",
      name: "Fungicide",
      description: "Controls fungi",
      displayOrder: 1,
      isSystem: true,
      isActive: true,
    },
    {
      id: "pt-2",
      code: "CUSTOM_BIO",
      name: "Custom Bio",
      description: null,
      displayOrder: 2,
      isSystem: false,
      isActive: false,
    },
  ];

  const sampleTargets: TargetItem[] = [
    {
      id: "tg-1",
      code: "MILDEW",
      name: "Powdery Mildew",
      targetType: "Disease",
      description: "White powdery spots",
      displayOrder: 1,
      isSystem: true,
      isActive: true,
    },
    {
      id: "tg-2",
      code: "APHIDS",
      name: "Green Aphids",
      targetType: "Insect",
      description: null,
      displayOrder: 2,
      isSystem: false,
      isActive: true,
    },
  ];

  const sampleMethods: ApplicationMethodItem[] = [
    {
      id: "am-1",
      code: "KNAPSACK",
      name: "Knapsack Sprayer",
      description: "Manual backpack",
      displayOrder: 1,
      isSystem: true,
      isActive: true,
    },
  ];

  beforeEach(async () => {
    mockMasterService = jasmine.createSpyObj("SprayMasterDataService", [
      "listProductTypes",
      "listTargets",
      "listApplicationMethods",
      "activateProductType",
      "deactivateProductType",
      "activateTarget",
      "deactivateTarget",
      "activateApplicationMethod",
      "deactivateApplicationMethod",
    ]);

    mockDialog = jasmine.createSpyObj("MatDialog", ["open"]);
    mockSnackBar = jasmine.createSpyObj("MatSnackBar", ["open"]);
    mockPermissionService = jasmine.createSpyObj("PermissionService", ["has"]);

    mockPermissionService.has.and.returnValue(true);
    mockMasterService.listProductTypes.and.returnValue(of(sampleProductTypes));
    mockMasterService.listTargets.and.returnValue(of(sampleTargets));
    mockMasterService.listApplicationMethods.and.returnValue(of(sampleMethods));

    await TestBed.configureTestingModule({
      imports: [NoopAnimationsModule, SprayMasterDataPageComponent],
      providers: [
        { provide: SprayMasterDataService, useValue: mockMasterService },
        { provide: MatDialog, useValue: mockDialog },
        { provide: MatSnackBar, useValue: mockSnackBar },
        { provide: PermissionService, useValue: mockPermissionService },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: new Map() } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SprayMasterDataPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should initialize and load all master data", () => {
    expect(mockMasterService.listProductTypes).toHaveBeenCalledWith(true);
    expect(mockMasterService.listTargets).toHaveBeenCalledWith(null, true);
    expect(mockMasterService.listApplicationMethods).toHaveBeenCalledWith(true);

    expect(component.productTypes().length).toBe(2);
    expect(component.targets().length).toBe(2);
    expect(component.applicationMethods().length).toBe(1);
    expect(component.totalCount()).toBe(2); // Tab 0 Product Types
    expect(component.systemCount()).toBe(1);
    expect(component.customCount()).toBe(1);
  });

  it("should switch tabs and update metrics", () => {
    component.onTabChanged(1); // Targets tab
    expect(component.activeTab()).toBe(1);
    expect(component.currentKind()).toBe("target");
    expect(component.totalCount()).toBe(2);

    component.onTabChanged(2); // Application Methods tab
    expect(component.activeTab()).toBe(2);
    expect(component.currentKind()).toBe("application-method");
    expect(component.totalCount()).toBe(1);
  });

  it("should filter product types by search term", () => {
    component.searchFilter.set("Bio");
    expect(component.filteredProductTypes().length).toBe(1);
    expect(component.filteredProductTypes()[0].name).toBe("Custom Bio");
  });

  it("should filter product types by status", () => {
    component.statusFilter.set("INACTIVE");
    expect(component.filteredProductTypes().length).toBe(1);
    expect(component.filteredProductTypes()[0].code).toBe("CUSTOM_BIO");
  });

  it("should filter targets by target type category", () => {
    component.onTabChanged(1);
    component.targetTypeFilter.set("Insect");
    expect(component.filteredTargets().length).toBe(1);
    expect(component.filteredTargets()[0].code).toBe("APHIDS");
  });

  it("should open create dialog and reload on save", () => {
    const dialogRefSpy = jasmine.createSpyObj({ afterClosed: of(true) });
    mockDialog.open.and.returnValue(dialogRefSpy);

    component.openCreateDialog();

    expect(mockDialog.open).toHaveBeenCalled();
    expect(mockMasterService.listProductTypes).toHaveBeenCalledTimes(2);
  });

  it("should not open edit dialog for system items", () => {
    component.openEditDialog(sampleProductTypes[0]); // isSystem = true
    expect(mockDialog.open).not.toHaveBeenCalled();
  });

  it("should open edit dialog for custom items", () => {
    const dialogRefSpy = jasmine.createSpyObj({ afterClosed: of(false) });
    mockDialog.open.and.returnValue(dialogRefSpy);

    component.openEditDialog(sampleProductTypes[1]); // isSystem = false
    expect(mockDialog.open).toHaveBeenCalled();
  });

  it("should activate custom item without confirm dialog", () => {
    mockMasterService.activateProductType.and.returnValue(of(undefined));

    component.toggleStatus(sampleProductTypes[1]); // Custom item that is currently inactive

    expect(mockDialog.open).not.toHaveBeenCalled();
    expect(mockMasterService.activateProductType).toHaveBeenCalledWith("pt-2");
  });

  it("should prompt confirmation when deactivating custom item", fakeAsync(() => {
    const activeCustom: ProductTypeItem = {
      ...sampleProductTypes[1],
      isActive: true,
    };

    const confirmRefSpy = jasmine.createSpyObj({ afterClosed: of(true) });
    mockDialog.open.and.returnValue(confirmRefSpy);
    mockMasterService.deactivateProductType.and.returnValue(of(undefined));

    component.toggleStatus(activeCustom);
    tick();

    expect(mockDialog.open).toHaveBeenCalled();
    expect(mockMasterService.deactivateProductType).toHaveBeenCalledWith("pt-2");
  }));
});
