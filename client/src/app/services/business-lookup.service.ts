import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { catchError, tap } from 'rxjs/operators';
import { of, Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  Business,
  BusinessSearchRequest,
  BusinessSearchResponse
} from '../models/business.model';

/**
 * Business lookup service for search functionality.
 *
 * INTERVIEW TALKING POINTS:
 * - Signals for state management
 * - Returns Observable for use with RxJS debounce in components
 * - Supports pagination
 */
@Injectable({ providedIn: 'root' })
export class BusinessLookupService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  // State
  isSearching = signal(false);
  searchResults = signal<Business[]>([]);
  searchError = signal<string | null>(null);
  totalCount = signal(0);

  /**
   * Search for businesses.
   * Returns Observable for use with RxJS operators (debounce, switchMap).
   */
  search(request: BusinessSearchRequest): Observable<BusinessSearchResponse> {
    let params = new HttpParams();

    if (request.searchTerm) {
      params = params.set('searchTerm', request.searchTerm);
    }
    if (request.stateCode) {
      params = params.set('stateCode', request.stateCode);
    }
    if (request.businessType) {
      params = params.set('businessType', request.businessType);
    }
    if (request.pageNumber) {
      params = params.set('pageNumber', request.pageNumber.toString());
    }
    if (request.pageSize) {
      params = params.set('pageSize', request.pageSize.toString());
    }

    this.isSearching.set(true);

    return this.http.get<BusinessSearchResponse>(
      `${this.apiUrl}/business/search`,
      { params }
    ).pipe(
      tap(response => {
        this.searchResults.set(response.businesses);
        this.totalCount.set(response.totalCount);
        this.isSearching.set(false);
        this.searchError.set(null);
      }),
      catchError(err => {
        this.searchError.set(err.message);
        this.isSearching.set(false);
        return of({
          businesses: [],
          totalCount: 0,
          pageNumber: 1,
          pageSize: 10,
          totalPages: 0,
          hasPreviousPage: false,
          hasNextPage: false
        });
      })
    );
  }

  /**
   * Get business by ID.
   */
  getById(id: number): Observable<Business | null> {
    return this.http.get<Business>(`${this.apiUrl}/business/${id}`).pipe(
      catchError(() => of(null))
    );
  }

  /**
   * Get business by Tax ID.
   */
  getByTaxId(taxId: string): Observable<Business | null> {
    return this.http.get<Business>(`${this.apiUrl}/business/taxid/${taxId}`).pipe(
      catchError(() => of(null))
    );
  }

  /**
   * Clear search results.
   */
  clearResults(): void {
    this.searchResults.set([]);
    this.totalCount.set(0);
    this.searchError.set(null);
  }
}
