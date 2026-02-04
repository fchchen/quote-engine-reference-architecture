import { Component, inject, signal, output, DestroyRef, input, effect } from '@angular/core';
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
  template: `
    <mat-form-field appearance="outline" class="full-width">
      <mat-label>Search Business</mat-label>
      <input
        matInput
        [value]="searchTerm()"
        (input)="onSearchInput($event)"
        [matAutocomplete]="auto"
        placeholder="Enter business name or Tax ID">
      <mat-icon matSuffix>search</mat-icon>

      @if (isSearching()) {
        <mat-spinner matSuffix diameter="20"></mat-spinner>
      }

      <mat-autocomplete
        #auto="matAutocomplete"
        (optionSelected)="onSelect($event)"
        [displayWith]="displayFn">
        @for (business of searchResults(); track business.id) {
          <mat-option [value]="business">
            <div class="business-option">
              <span class="business-name">{{ business.businessName }}</span>
              <span class="business-details">
                {{ business.stateCode }} | {{ business.taxId }}
              </span>
            </div>
          </mat-option>
        }

        @if (searchResults().length === 0 && searchTerm().length >= 2 && !isSearching()) {
          <mat-option disabled>
            No businesses found
          </mat-option>
        }
      </mat-autocomplete>

      <mat-hint>Start typing to search by name or Tax ID</mat-hint>
    </mat-form-field>
  `,
  styles: [`
    .full-width {
      width: 100%;
    }

    .business-option {
      display: flex;
      flex-direction: column;
      line-height: 1.2;
    }

    .business-name {
      font-weight: 500;
    }

    .business-details {
      font-size: 0.75rem;
      color: rgba(0, 0, 0, 0.54);
    }

    mat-spinner {
      margin-right: 8px;
    }
  `]
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
    // Effect to clear search when reset is triggered
    effect(() => {
      const trigger = this.resetTrigger();
      if (trigger > 0) {
        this.clear();
      }
    });

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
  }
}
