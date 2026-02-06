import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { authGuard } from './auth.guard';
import { AuthService } from '../services/auth.service';

describe('authGuard', () => {
  let router: Router;
  let authServiceMock: { isAuthenticated: jasmine.Spy<() => boolean> };

  beforeEach(() => {
    authServiceMock = {
      isAuthenticated: jasmine.createSpy('isAuthenticated')
    };

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceMock }
      ]
    });

    router = TestBed.inject(Router);
  });

  function runGuard(url: string): boolean | UrlTree {
    const route = {} as ActivatedRouteSnapshot;
    const state = { url } as RouterStateSnapshot;
    const result = TestBed.runInInjectionContext(() => authGuard(route, state));

    if (typeof result === 'boolean' || result instanceof UrlTree) {
      return result;
    }

    throw new Error('Expected authGuard to return a synchronous value in this app');
  }

  it('allows navigation when authenticated', () => {
    authServiceMock.isAuthenticated.and.returnValue(true);

    const result = runGuard('/quote');

    expect(result).toBeTrue();
  });

  it('redirects to login with returnUrl when unauthenticated', () => {
    authServiceMock.isAuthenticated.and.returnValue(false);

    const result = runGuard('/quote?step=review');

    expect(result instanceof UrlTree).toBeTrue();
    expect(router.serializeUrl(result as UrlTree)).toBe('/login?returnUrl=%2Fquote%3Fstep%3Dreview');
  });
});
