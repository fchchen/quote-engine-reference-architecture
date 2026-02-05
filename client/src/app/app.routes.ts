import { Routes } from '@angular/router';
import { rateTableResolver } from './resolvers/rate-table.resolver';
import { quoteIncompleteGuard } from './guards/quote-incomplete.guard';
import { authGuard } from './guards/auth.guard';

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
    path: 'login',
    loadComponent: () =>
      import('./pages/login/login.page')
        .then(m => m.LoginPage)
  },
  {
    path: 'quote',
    loadComponent: () =>
      import('./components/quote-form/quote-form.component')
        .then(m => m.QuoteFormComponent),
    resolve: {
      referenceData: rateTableResolver
    },
    canActivate: [authGuard],
    canDeactivate: [quoteIncompleteGuard]
  },
  {
    path: 'history',
    loadComponent: () =>
      import('./pages/history/history.page')
        .then(m => m.HistoryPage),
    canActivate: [authGuard]
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
