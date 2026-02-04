import { Injectable, signal, computed } from '@angular/core';

/**
 * Global loading state service.
 *
 * INTERVIEW TALKING POINTS:
 * - Centralized loading state management
 * - Uses signals for reactive state
 * - Tracks multiple concurrent operations
 */
@Injectable({ providedIn: 'root' })
export class LoadingService {
  // Track loading operations by key
  private loadingOperations = signal<Set<string>>(new Set());

  // Global loading state
  isLoading = computed(() => this.loadingOperations().size > 0);

  // Specific loading states
  isQuoteLoading = computed(() => this.loadingOperations().has('quote'));
  isSearchLoading = computed(() => this.loadingOperations().has('search'));
  isReferenceDataLoading = computed(() => this.loadingOperations().has('referenceData'));

  /**
   * Start a loading operation.
   */
  startLoading(key: string = 'default'): void {
    this.loadingOperations.update(ops => {
      const newOps = new Set(ops);
      newOps.add(key);
      return newOps;
    });
  }

  /**
   * Stop a loading operation.
   */
  stopLoading(key: string = 'default'): void {
    this.loadingOperations.update(ops => {
      const newOps = new Set(ops);
      newOps.delete(key);
      return newOps;
    });
  }

  /**
   * Check if a specific operation is loading.
   */
  isOperationLoading(key: string): boolean {
    return this.loadingOperations().has(key);
  }

  /**
   * Clear all loading states.
   */
  clearAll(): void {
    this.loadingOperations.set(new Set());
  }
}
