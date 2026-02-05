import { Component, inject, signal, output, DestroyRef, input, effect, untracked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, filter, switchMap, tap } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BusinessLookupService } from '../../services/business-lookup.service';
import { Business } from '../../models/business.model';

/**
 * Business search component with autocomplete.
 *
 * INTERVIEW TALKING POINTS:
 * - RxJS ONLY for debouncing (signals can't debounce)
 * - Signals for all state
 * - switchMap cancels previous requests
 * - takeUntilDestroyed for cleanup
 */
@Component({
  selector: 'app-business-search',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatAutocompleteModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './business-search.component.html',
  styleUrl: './business-search.component.scss'
})
export class BusinessSearchComponent {
  private lookupService = inject(BusinessLookupService);
  private destroyRef = inject(DestroyRef);

  // Input to trigger reset from parent
  resetTrigger = input(0);

  // Signals for state
  searchTerm = signal('');
  searchResults = signal<Business[]>([]);
  isSearching = signal(false);

  // Output for parent component
  businessSelected = output<Business>();

  // RxJS Subject ONLY for debouncing (signals cannot debounce)
  private searchSubject = new Subject<string>();

  constructor() {
    // Effect to clear search when reset is triggered.
    // Uses allowSignalWrites because clear() writes to signals.
    effect(() => {
      const trigger = this.resetTrigger();
      if (trigger > 0) {
        untracked(() => this.clear());
      }
    }, { allowSignalWrites: true });

    // RxJS pipeline ONLY because we need debounceTime
    // Signals don't have built-in debounce capability
    this.searchSubject.pipe(
      debounceTime(300),               // Wait 300ms after typing stops
      distinctUntilChanged(),          // Only emit if value changed
      filter(term => term.length >= 2), // Minimum 2 characters
      tap(() => this.isSearching.set(true)),
      switchMap(term =>                // Cancel previous request
        this.lookupService.search({ searchTerm: term, pageSize: 10 })
      ),
      takeUntilDestroyed(this.destroyRef) // Auto-cleanup on destroy
    ).subscribe(response => {
      this.searchResults.set(response.businesses);
      this.isSearching.set(false);
    });
  }

  /**
   * Handle input events - update signal and feed to RxJS for debouncing.
   */
  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchTerm.set(value);
    this.searchSubject.next(value); // Feed to RxJS for debouncing
  }

  /**
   * Handle autocomplete selection.
   */
  onSelect(event: MatAutocompleteSelectedEvent): void {
    const business = event.option.value as Business;
    this.businessSelected.emit(business);
    this.searchResults.set([]); // Clear results
  }

  /**
   * Display function for autocomplete.
   */
  displayFn(business: Business): string {
    return business?.businessName ?? '';
  }

  /**
   * Clear search state.
   */
  clear(): void {
    this.searchTerm.set('');
    this.searchResults.set([]);
    // Push empty string to reset distinctUntilChanged state,
    // so re-searching the same term works after reset
    this.searchSubject.next('');
  }
}
