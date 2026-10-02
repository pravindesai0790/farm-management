import { PagedResponse } from "../models/paged-response.model";

export interface Supplier {
  readonly id: string;
  readonly organizationId: string;
  readonly name: string;
  readonly contactPerson?: string | null;
  readonly phone?: string | null;
  readonly email?: string | null;
  readonly address?: string | null;
  readonly registrationIdentifier?: string | null;
  readonly notes?: string | null;
  readonly isActive: boolean;
  readonly createdAt: string;
  readonly createdBy: string;
  readonly updatedAt?: string | null;
  readonly updatedBy?: string | null;
}

export type SupplierList = PagedResponse<Supplier>;

export interface CreateSupplierRequest {
  readonly name: string;
  readonly contactPerson?: string | null;
  readonly phone?: string | null;
  readonly email?: string | null;
  readonly address?: string | null;
  readonly registrationIdentifier?: string | null;
  readonly notes?: string | null;
}

export interface UpdateSupplierRequest {
  readonly name: string;
  readonly contactPerson?: string | null;
  readonly phone?: string | null;
  readonly email?: string | null;
  readonly address?: string | null;
  readonly registrationIdentifier?: string | null;
  readonly notes?: string | null;
}

export interface UpdateSupplierStatusRequest {
  readonly isActive: boolean;
}
