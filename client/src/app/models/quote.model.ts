/**
 * Quote request model for API calls.
 */
export interface QuoteRequest {
  businessName: string;
  taxId: string;
  businessType: BusinessType;
  stateCode: string;
  classificationCode: string;
  productType: ProductType;
  annualPayroll: number;
  annualRevenue: number;
  employeeCount: number;
  yearsInBusiness: number;
  coverageLimit: number;
  deductible: number;
  contactEmail?: string;
  effectiveDate?: Date;
  riskFactors?: RiskFactor[];
}

/**
 * Quote response from API.
 */
export interface QuoteResponse {
  quoteNumber: string;
  quoteDate: Date;
  expirationDate: Date;
  status: QuoteStatus;
  businessName: string;
  businessType: BusinessType;
  productType: ProductType;
  stateCode: string;
  coverageLimit: number;
  deductible: number;
  effectiveDate: Date;
  policyExpirationDate: Date;
  premium: PremiumBreakdown;
  riskAssessment: RiskAssessment;
  isEligible: boolean;
  eligibilityMessages: string[];
  processingTimeMs: string;
  apiVersion: string;
}

/**
 * Premium breakdown details.
 */
export interface PremiumBreakdown {
  basePremium: number;
  adjustments: PremiumAdjustment[];
  totalAdjustments: number;
  subtotal: number;
  stateTax: number;
  policyFee: number;
  annualPremium: number;
  monthlyPremium: number;
  minimumPremium: number;
}

export interface PremiumAdjustment {
  code: string;
  description: string;
  type: AdjustmentType;
  factor: number;
  amount: number;
}

/**
 * Risk assessment results.
 */
export interface RiskAssessment {
  riskScore: number;
  riskTier: RiskTier;
  factorScores: RiskFactorScore[];
  notes: string[];
}

export interface RiskFactorScore {
  factorName: string;
  score: number;
  weight: number;
  impact: string;
}

export interface RiskFactor {
  factorCode: string;
  factorName: string;
  factorValue: number;
  factorType: RiskFactorType;
}

/**
 * Enums matching backend.
 */
export enum BusinessType {
  Retail = 'Retail',
  Restaurant = 'Restaurant',
  Office = 'Office',
  Manufacturing = 'Manufacturing',
  Construction = 'Construction',
  Technology = 'Technology',
  Healthcare = 'Healthcare',
  Transportation = 'Transportation',
  RealEstate = 'RealEstate',
  ProfessionalServices = 'ProfessionalServices'
}

export enum ProductType {
  WorkersCompensation = 'WorkersCompensation',
  GeneralLiability = 'GeneralLiability',
  BusinessOwnersPolicy = 'BusinessOwnersPolicy',
  CommercialAuto = 'CommercialAuto',
  ProfessionalLiability = 'ProfessionalLiability',
  CyberLiability = 'CyberLiability'
}

export enum QuoteStatus {
  Draft = 'Draft',
  Quoted = 'Quoted',
  Referred = 'Referred',
  Declined = 'Declined',
  Expired = 'Expired',
  Bound = 'Bound'
}

export enum RiskTier {
  Preferred = 'Preferred',
  Standard = 'Standard',
  NonStandard = 'NonStandard',
  Decline = 'Decline'
}

export enum AdjustmentType {
  Discount = 'Discount',
  Surcharge = 'Surcharge',
  Credit = 'Credit',
  Debit = 'Debit'
}

export enum RiskFactorType {
  Credit = 'Credit',
  Claims = 'Claims',
  Safety = 'Safety',
  Experience = 'Experience',
  Industry = 'Industry'
}

/**
 * Premium estimate request (for real-time calculation).
 */
export interface PremiumEstimateRequest {
  productType: ProductType;
  stateCode: string;
  classificationCode?: string;
  annualPayroll: number;
  annualRevenue: number;
  employeeCount: number;
  coverageLimit: number;
  deductible: number;
}

export interface PremiumEstimateResponse {
  estimatedAnnualPremium: number;
  estimatedMonthlyPremium: number;
  basePremium: number;
  minimumPremium: number;
  stateTaxRate: number;
  note: string;
}
