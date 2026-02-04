import { BusinessType } from './quote.model';

/**
 * Business information for lookup/autocomplete.
 */
export interface Business {
  id: number;
  businessName: string;
  taxId: string;
  dbaName?: string;
  businessType: BusinessType;
  stateCode: string;
  classificationCode?: string;
  classificationDescription?: string;
  address?: string;
  city?: string;
  zipCode?: string;
  phone?: string;
  email?: string;
  dateEstablished?: Date;
  employeeCount?: number;
  annualRevenue?: number;
  annualPayroll?: number;
  isActive: boolean;
  createdDate: Date;
  modifiedDate?: Date;
}

/**
 * Business search request.
 */
export interface BusinessSearchRequest {
  searchTerm?: string;
  taxId?: string;
  stateCode?: string;
  businessType?: BusinessType;
  pageNumber?: number;
  pageSize?: number;
}

/**
 * Paginated business search response.
 */
export interface BusinessSearchResponse {
  businesses: Business[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

/**
 * State information for dropdowns.
 */
export interface StateInfo {
  code: string;
  name: string;
}

/**
 * Product information for dropdowns.
 */
export interface ProductInfo {
  type: string;
  name: string;
  description: string;
}

/**
 * Business type information for dropdowns.
 */
export interface BusinessTypeInfo {
  type: string;
  name: string;
  description: string;
}

/**
 * Classification code for product type.
 */
export interface ClassificationCode {
  code: string;
  description: string;
  productType?: string;
  baseRate?: number;
  hazardGroup?: string;
}
