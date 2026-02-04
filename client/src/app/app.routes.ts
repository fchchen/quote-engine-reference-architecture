import { Routes } from '@angular/router';
import { rateTableResolver } from './resolvers/rate-table.resolver';
import { quoteIncompleteGuard } from './guards/quote-incomplete.guard';

/**
 * Application routes with guards and resolvers.
 *
 * INTERVIEW TALKING POINTS:
 * - Lazy loading for code splitting
 * - Route guards for UX protection
 * - Resolvers for pre-fetching data
 */
export const routes: Routes = [
  {
    path: '',
    redirectTo: '/quote',
    pathMatch: 'full'
  },
  {
    path: 'quote',
    loadComponent: () =>
      import('./components/quote-form/quote-form.component')
        .then(m => m.QuoteFormComponent),
    resolve: {
      referenceData: rateTableResolver
    },
    canDeactivate: [quoteIncompleteGuard]
  },
  {
    path: 'history',
    loadComponent: () =>
      import('./pages/history/history.page')
        .then(m => m.HistoryPage)
  },
  {
    path: 'about',
    loadComponent: () =>
      import('./pages/about/about.page')
        .then(m => m.AboutPage)
  },
  {
    path: '**',
    redirectTo: '/quote'
  }
];
