import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { retry, catchError, tap } from 'rxjs/operators';
import { of } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  QuoteRequest,
  QuoteResponse,
  PremiumEstimateRequest,
  PremiumEstimateResponse
} from '../models/quote.model';

/**
 * Quote service with Signals-first state management.
 *
 * INTERVIEW TALKING POINTS:
 * - Uses Angular Signals for all state management
 * - RxJS only where necessary (HTTP calls, retry logic)
 * - computed() for derived state
 * - Thread-safe state updates with signal.update()
 */
@Injectable({ providedIn: 'root' })
export class QuoteService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  // ========================================
  // STATE - All as Signals
  // ========================================

  /** Loading state for quote operations */
  isLoading = signal(false);

  /** Error message from last operation */
  error = signal<string | null>(null);

  /** Quote history for the current session */
  quoteHistory = signal<QuoteResponse[]>([]);

  /** Currently active quote being worked on */
  currentQuote = signal<QuoteResponse | null>(null);

  /** Premium estimate for real-time updates */
  premiumEstimate = signal<PremiumEstimateResponse | null>(null);

  // ========================================
  // COMPUTED VALUES - Derived from Signals
  // ========================================

  /** Most recent quote in history */
  latestQuote = computed(() => this.quoteHistory().at(-1) ?? null);

  /** Total number of quotes requested this session */
  totalQuotesRequested = computed(() => this.quoteHistory().length);

  /** Whether we have any quotes */
  hasQuotes = computed(() => this.quoteHistory().length > 0);

  /** Summary statistics */
  quoteStats = computed(() => {
    const history = this.quoteHistory();
    const quoted = history.filter(q => q.status === 'Quoted').length;
    const declined = history.filter(q => q.status === 'Declined').length;
    const totalPremium = history
      .filter(q => q.status === 'Quoted')
      .reduce((sum, q) => sum + q.premium.annualPremium, 0);

    return {
      total: history.length,
      quoted,
      declined,
      totalPremium,
      averagePremium: quoted > 0 ? totalPremium / quoted : 0
    };
  });

  // ========================================
  // METHODS
  // ========================================

  /**
   * Calculate a full quote.
   * Uses RxJS for HTTP call with retry logic.
   */
  calculateQuote(request: QuoteRequest): void {
    this.isLoading.set(true);
    this.error.set(null);

    // RxJS only for HTTP call with retry
    this.http.post<QuoteResponse>(`${this.apiUrl}/quote`, request).pipe(
      retry({ count: 3, delay: 1000 }),
      tap(result => {
        // Update state with Signals
        this.quoteHistory.update(history => [...history, result]);
        this.currentQuote.set(result);
        this.isLoading.set(false);
      }),
      catchError(err => {
        this.error.set(err.error?.detail || err.message || 'Failed to calculate quote');
        this.isLoading.set(false);
        return of(null);
      })
    ).subscribe();
  }

  /**
   * Get a previously calculated quote.
   */
  getQuote(quoteNumber: string): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.http.get<QuoteResponse>(`${this.apiUrl}/quote/${quoteNumber}`).pipe(
      tap(result => {
        this.currentQuote.set(result);
        this.isLoading.set(false);
      }),
      catchError(err => {
        this.error.set(err.error?.detail || 'Quote not found');
        this.isLoading.set(false);
        return of(null);
      })
    ).subscribe();
  }

  /**
   * Get premium estimate for real-time updates.
   * Called frequently during form input.
   */
  getPremiumEstimate(request: PremiumEstimateRequest): void {
    // Don't show loading for estimates (too fast)
    this.http.post<PremiumEstimateResponse>(
      `${this.apiUrl}/premium/estimate`,
      request
    ).pipe(
      tap(result => this.premiumEstimate.set(result)),
      catchError(() => {
        this.premiumEstimate.set(null);
        return of(null);
      })
    ).subscribe();
  }

  /**
   * Clear current quote and error state.
   */
  clearCurrentQuote(): void {
    this.currentQuote.set(null);
    this.error.set(null);
    this.premiumEstimate.set(null);
  }

  /**
   * Clear all state.
   */
  reset(): void {
    this.isLoading.set(false);
    this.error.set(null);
    this.quoteHistory.set([]);
    this.currentQuote.set(null);
    this.premiumEstimate.set(null);
  }
}
