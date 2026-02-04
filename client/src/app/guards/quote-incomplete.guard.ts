import { inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';
import { QuoteFormComponent } from '../components/quote-form/quote-form.component';

/**
 * Route guard to prevent navigation away from incomplete quote.
 *
 * INTERVIEW TALKING POINTS:
 * - Functional guard (Angular 15+)
 * - Uses component's signal state to check form status
 * - Confirms navigation with user
 */
export const quoteIncompleteGuard: CanDeactivateFn<QuoteFormComponent> = (component) => {
  // Check if form has unsaved changes using component's signal
  if (component.hasUnsavedChanges()) {
    return confirm(
      'You have an incomplete quote. Are you sure you want to leave? Your progress will be lost.'
    );
  }
  return true;
};

/**
 * Type for components that can use the quote incomplete guard.
 */
export interface HasUnsavedChanges {
  hasUnsavedChanges(): boolean;
}
