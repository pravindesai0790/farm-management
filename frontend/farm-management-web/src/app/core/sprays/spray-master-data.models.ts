export type TargetType =
  | "Disease"
  | "Insect"
  | "Mite"
  | "Weed"
  | "Other";

export interface ProductTypeItem {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly description: string | null;
  readonly displayOrder: number;
  readonly isSystem: boolean;
  readonly isActive: boolean;
}

export interface TargetItem {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly targetType: TargetType | string;
  readonly description: string | null;
  readonly displayOrder: number;
  readonly isSystem: boolean;
  readonly isActive: boolean;
}

export interface ApplicationMethodItem {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly description: string | null;
  readonly displayOrder: number;
  readonly isSystem: boolean;
  readonly isActive: boolean;
}

export interface CreateProductTypeRequest {
  readonly code: string;
  readonly name: string;
  readonly description?: string | null;
  readonly displayOrder?: number;
}

export interface UpdateProductTypeRequest {
  readonly name: string;
  readonly description?: string | null;
  readonly displayOrder?: number;
}

export interface CreateTargetRequest {
  readonly code: string;
  readonly name: string;
  readonly targetType: string;
  readonly description?: string | null;
  readonly displayOrder?: number;
}

export interface UpdateTargetRequest {
  readonly name: string;
  readonly targetType: string;
  readonly description?: string | null;
  readonly displayOrder?: number;
}

export interface CreateApplicationMethodRequest {
  readonly code: string;
  readonly name: string;
  readonly description?: string | null;
  readonly displayOrder?: number;
}

export interface UpdateApplicationMethodRequest {
  readonly name: string;
  readonly description?: string | null;
  readonly displayOrder?: number;
}

export type MasterTypeKind = "product-type" | "target" | "application-method";
