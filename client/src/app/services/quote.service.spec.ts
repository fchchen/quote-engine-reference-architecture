import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { QuoteService } from './quote.service';
import {
  BusinessType,
  ProductType,
  QuoteRequest,
  QuoteResponse,
  QuoteStatus,
  RiskTier
} from '../models/quote.model';

describe('QuoteService', () => {
  let service: QuoteService;
  let httpMock: HttpTestingController;

  const quoteUrl = 'http://localhost:5210/api/v1/quote';

  const request: QuoteRequest = {
    businessName: 'Acme LLC',
    taxId: '12-3456789',
    businessType: BusinessType.Technology,
    stateCode: 'CA',
    classificationCode: '41650',
    productType: ProductType.GeneralLiability,
    annualPayroll: 300000,
    annualRevenue: 1000000,
    employeeCount: 10,
    yearsInBusiness: 5,
    coverageLimit: 1000000,
    deductible: 1000
  };

  const response: QuoteResponse = {
    quoteNumber: 'QT-20260206-ABC12345',
    quoteDate: new Date('2026-02-06T00:00:00Z'),
    expirationDate: new Date('2026-03-08T00:00:00Z'),
    status: QuoteStatus.Quoted,
    businessName: request.businessName,
    businessType: request.businessType,
    productType: request.productType,
    stateCode: request.stateCode,
    coverageLimit: request.coverageLimit,
    deductible: request.deductible,
    effectiveDate: new Date('2026-02-07T00:00:00Z'),
    policyExpirationDate: new Date('2027-02-07T00:00:00Z'),
    premium: {
      basePremium: 5500,
      adjustments: [],
      totalAdjustments: 0,
      subtotal: 5500,
      stateTax: 180.4,
      policyFee: 150,
      annualPremium: 5830.4,
      monthlyPremium: 485.87,
      minimumPremium: 500
    },
    riskAssessment: {
      riskScore: 45,
      riskTier: RiskTier.Standard,
      factorScores: [],
      notes: []
    },
    isEligible: true,
    eligibilityMessages: [],
    processingTimeMs: '25ms',
    apiVersion: '1.0'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [QuoteService, provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(QuoteService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('posts a quote request and updates state on success', () => {
    service.calculateQuote(request);

    expect(service.isLoading()).toBeTrue();

    const req = httpMock.expectOne(quoteUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(response);

    expect(service.isLoading()).toBeFalse();
    expect(service.error()).toBeNull();
    expect(service.currentQuote()?.quoteNumber).toBe(response.quoteNumber);
    expect(service.quoteHistory().length).toBe(1);
  });

  it('sets error and clears loading state on failure', () => {
    service.calculateQuote(request);

    const req = httpMock.expectOne(quoteUrl);
    req.flush(
      { detail: 'Calculation failed' },
      { status: 500, statusText: 'Server Error' }
    );

    expect(service.isLoading()).toBeFalse();
    expect(service.error()).toBe('Calculation failed');
  });

  it('does not retry failed quote POST requests', fakeAsync(() => {
    service.calculateQuote(request);

    const req = httpMock.expectOne(quoteUrl);
    req.flush(
      { detail: 'Calculation failed' },
      { status: 500, statusText: 'Server Error' }
    );

    tick(4000);
    httpMock.expectNone(quoteUrl);
    expect(service.error()).toBe('Calculation failed');
  }));
});
