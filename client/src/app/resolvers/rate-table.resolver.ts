import { inject } from '@angular/core';
import { ResolveFn } from '@angular/router';
import { forkJoin, map } from 'rxjs';
import { RateTableService } from '../services/rate-table.service';
import { StateInfo, ProductInfo, BusinessTypeInfo } from '../models/business.model';

/**
 * Reference data for quote form.
 */
export interface ReferenceData {
  states: StateInfo[];
  products: ProductInfo[];
  businessTypes: BusinessTypeInfo[];
}

/**
 * Route resolver to pre-fetch rate table data before component loads.
 *
 * INTERVIEW TALKING POINTS:
 * - Functional resolver (Angular 15+)
 * - Pre-fetches data to avoid loading states in components
 * - Uses forkJoin to load multiple resources in parallel
 */
export const rateTableResolver: ResolveFn<ReferenceData> = () => {
  const rateTableService = inject(RateTableService);

  return forkJoin({
    states: rateTableService.getStates(),
    products: rateTableService.getProducts(),
    businessTypes: rateTableService.getBusinessTypes()
  }).pipe(
    map(data => ({
      states: data.states,
      products: data.products,
      businessTypes: data.businessTypes
    }))
  );
};
