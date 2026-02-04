import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, catchError, of, shareReplay } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  StateInfo,
  ProductInfo,
  BusinessTypeInfo,
  ClassificationCode
} from '../models/business.model';
import { FormFieldConfig } from '../models/form-field.model';
import { ProductType } from '../models/quote.model';

/**
 * Rate table service with caching.
 *
 * INTERVIEW TALKING POINTS:
 * - Caches reference data using shareReplay
 * - Signals for state
 * - Provides dynamic form field configuration
 */
@Injectable({ providedIn: 'root' })
export class RateTableService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  // Cached observables for reference data
  private states$?: Observable<StateInfo[]>;
  private products$?: Observable<ProductInfo[]>;
  private businessTypes$?: Observable<BusinessTypeInfo[]>;
  private classificationCache = new Map<string, Observable<ClassificationCode[]>>();

  // State signals
  states = signal<StateInfo[]>([]);
  products = signal<ProductInfo[]>([]);
  businessTypes = signal<BusinessTypeInfo[]>([]);
  isLoading = signal(false);
  error = signal<string | null>(null);

  // Computed values
  statesLoaded = computed(() => this.states().length > 0);
  productsLoaded = computed(() => this.products().length > 0);

  /**
   * Get available states (cached).
   */
  getStates(): Observable<StateInfo[]> {
    if (!this.states$) {
      this.isLoading.set(true);
      this.states$ = this.http.get<StateInfo[]>(`${this.apiUrl}/ratetable/states`).pipe(
        tap(data => {
          this.states.set(data);
          this.isLoading.set(false);
        }),
        shareReplay(1),
        catchError(err => {
          this.error.set(err.message);
          this.isLoading.set(false);
          return of([]);
        })
      );
    }
    return this.states$;
  }

  /**
   * Get available products (cached).
   */
  getProducts(): Observable<ProductInfo[]> {
    if (!this.products$) {
      this.isLoading.set(true);
      this.products$ = this.http.get<ProductInfo[]>(`${this.apiUrl}/ratetable/products`).pipe(
        tap(data => {
          this.products.set(data);
          this.isLoading.set(false);
        }),
        shareReplay(1),
        catchError(err => {
          this.error.set(err.message);
          this.isLoading.set(false);
          return of([]);
        })
      );
    }
    return this.products$;
  }

  /**
   * Get business types (cached).
   */
  getBusinessTypes(): Observable<BusinessTypeInfo[]> {
    if (!this.businessTypes$) {
      this.isLoading.set(true);
      this.businessTypes$ = this.http.get<BusinessTypeInfo[]>(
        `${this.apiUrl}/ratetable/business-types`
      ).pipe(
        tap(data => {
          this.businessTypes.set(data);
          this.isLoading.set(false);
        }),
        shareReplay(1),
        catchError(err => {
          this.error.set(err.message);
          this.isLoading.set(false);
          return of([]);
        })
      );
    }
    return this.businessTypes$;
  }

  /**
   * Get classification codes for a product type (cached per product).
   */
  getClassificationCodes(productType: ProductType): Observable<ClassificationCode[]> {
    const cacheKey = productType;

    if (!this.classificationCache.has(cacheKey)) {
      const obs$ = this.http.get<ClassificationCode[]>(
        `${this.apiUrl}/ratetable/classifications/${productType}`
      ).pipe(
        shareReplay(1),
        catchError(() => of([]))
      );
      this.classificationCache.set(cacheKey, obs$);
    }

    return this.classificationCache.get(cacheKey)!;
  }

  /**
   * Get dynamic form fields for a product type.
   */
  getFieldsForType(productType: ProductType): Observable<FormFieldConfig[]> {
    // Return product-specific field configurations
    const fields = this.getProductFields(productType);
    return of(fields);
  }

  /**
   * Load all reference data.
   */
  loadAll(): void {
    this.getStates().subscribe();
    this.getProducts().subscribe();
    this.getBusinessTypes().subscribe();
  }

  private getProductFields(productType: ProductType): FormFieldConfig[] {
    switch (productType) {
      case ProductType.WorkersCompensation:
        return [
          {
            key: 'annualPayroll',
            label: 'Annual Payroll',
            type: 'currency',
            required: true,
            min: 10000,
            max: 100000000,
            hint: 'Total annual payroll for all employees'
          },
          {
            key: 'employeeCount',
            label: 'Number of Employees',
            type: 'number',
            required: true,
            min: 1,
            max: 10000
          }
        ];

      case ProductType.GeneralLiability:
        return [
          {
            key: 'annualRevenue',
            label: 'Annual Revenue',
            type: 'currency',
            required: true,
            min: 10000,
            max: 50000000
          },
          {
            key: 'squareFootage',
            label: 'Square Footage',
            type: 'number',
            required: false,
            min: 100,
            max: 1000000,
            hint: 'Business premises square footage'
          }
        ];

      case ProductType.BusinessOwnersPolicy:
        return [
          {
            key: 'annualRevenue',
            label: 'Annual Revenue',
            type: 'currency',
            required: true
          },
          {
            key: 'buildingValue',
            label: 'Building Value',
            type: 'currency',
            required: false,
            hint: 'If you own the building'
          },
          {
            key: 'contentsValue',
            label: 'Contents Value',
            type: 'currency',
            required: true,
            hint: 'Business personal property value'
          }
        ];

      case ProductType.CommercialAuto:
        return [
          {
            key: 'vehicleCount',
            label: 'Number of Vehicles',
            type: 'number',
            required: true,
            min: 1,
            max: 500
          },
          {
            key: 'radius',
            label: 'Operating Radius (miles)',
            type: 'select',
            required: true,
            options: [
              { value: 50, label: 'Local (0-50 miles)' },
              { value: 200, label: 'Regional (51-200 miles)' },
              { value: 500, label: 'Intermediate (201-500 miles)' },
              { value: 1000, label: 'Long Haul (500+ miles)' }
            ]
          }
        ];

      case ProductType.CyberLiability:
        return [
          {
            key: 'annualRevenue',
            label: 'Annual Revenue',
            type: 'currency',
            required: true
          },
          {
            key: 'recordCount',
            label: 'Number of Customer Records',
            type: 'number',
            required: true,
            hint: 'Approximate number of customer/patient records'
          },
          {
            key: 'pciCompliant',
            label: 'PCI Compliant',
            type: 'checkbox',
            required: false
          }
        ];

      default:
        return [
          {
            key: 'annualRevenue',
            label: 'Annual Revenue',
            type: 'currency',
            required: true
          }
        ];
    }
  }
}
