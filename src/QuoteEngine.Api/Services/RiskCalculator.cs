using QuoteEngine.Api.Models;

namespace QuoteEngine.Api.Services;

/// <summary>
/// Risk calculation service for premium computation.
///
/// INTERVIEW TALKING POINTS:
/// - Stateless service - safe to use as Singleton in DI
/// - Pure functions for calculation logic - easily testable
/// - Demonstrates domain-specific business logic
/// - Shows separation of concerns from QuoteService
/// </summary>
public class RiskCalculator : IRiskCalculator
{
    private readonly ILogger<RiskCalculator> _logger;

    // Risk factor weights (in production, these would be configurable)
    private const int YearsInBusinessWeight = 20;
    private const int EmployeeCountWeight = 15;
    private const int IndustryWeight = 25;
    private const int ClaimsHistoryWeight = 25;
    private const int RevenueSizeWeight = 15;

    public RiskCalculator(ILogger<RiskCalculator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public RiskAssessment CalculateRisk(QuoteRequest request)
    {
        _logger.LogDebug("Calculating risk for {BusinessType} in {State}", request.BusinessType, request.StateCode);

        var factorScores = new List<RiskFactorScore>();

        // Factor 1: Years in Business (more years = lower risk)
        var yearsScore = CalculateYearsInBusinessScore(request.YearsInBusiness);
        factorScores.Add(new RiskFactorScore
        {
            FactorName = "Years in Business",
            Score = yearsScore,
            Weight = YearsInBusinessWeight,
            Impact = yearsScore <= 30 ? "Favorable" : yearsScore >= 60 ? "Unfavorable" : "Neutral"
        });

        // Factor 2: Employee Count relative to industry
        var employeeScore = CalculateEmployeeCountScore(request.EmployeeCount, request.BusinessType);
        factorScores.Add(new RiskFactorScore
        {
            FactorName = "Employee Count",
            Score = employeeScore,
            Weight = EmployeeCountWeight,
            Impact = employeeScore <= 30 ? "Favorable" : employeeScore >= 60 ? "Unfavorable" : "Neutral"
        });

        // Factor 3: Industry/Business Type
        var industryScore = CalculateIndustryScore(request.BusinessType);
        factorScores.Add(new RiskFactorScore
        {
            FactorName = "Industry Risk",
            Score = industryScore,
            Weight = IndustryWeight,
            Impact = industryScore <= 30 ? "Favorable" : industryScore >= 60 ? "Unfavorable" : "Neutral"
        });

        // Factor 4: Claims History (from risk factors if provided)
        var claimsScore = CalculateClaimsScore(request.RiskFactors);
        factorScores.Add(new RiskFactorScore
        {
            FactorName = "Claims History",
            Score = claimsScore,
            Weight = ClaimsHistoryWeight,
            Impact = claimsScore <= 30 ? "Favorable" : claimsScore >= 60 ? "Unfavorable" : "Neutral"
        });

        // Factor 5: Revenue Size
        var revenueScore = CalculateRevenueSizeScore(request.AnnualRevenue);
        factorScores.Add(new RiskFactorScore
        {
            FactorName = "Revenue Size",
            Score = revenueScore,
            Weight = RevenueSizeWeight,
            Impact = revenueScore <= 30 ? "Favorable" : revenueScore >= 60 ? "Unfavorable" : "Neutral"
        });

        // Calculate weighted overall score
        var weightedSum = factorScores.Sum(f => f.Score * f.Weight);
        var totalWeight = factorScores.Sum(f => f.Weight);
        var overallScore = (int)Math.Round(weightedSum / (double)totalWeight);

        // Determine risk tier based on score
        var riskTier = DetermineRiskTier(overallScore);

        var notes = GenerateUnderwritingNotes(request, overallScore, riskTier);

        _logger.LogInformation(
            "Risk assessment complete: Score={Score}, Tier={Tier}",
            overallScore,
            riskTier);

        return new RiskAssessment
        {
            RiskScore = overallScore,
            RiskTier = riskTier,
            FactorScores = factorScores,
            Notes = notes
        };
    }

    /// <inheritdoc />
    public PremiumBreakdown CalculatePremium(QuoteRequest request, RiskAssessment riskAssessment, RateTableEntry rateEntry)
    {
        _logger.LogDebug(
            "Calculating premium. BaseRate: {BaseRate}, RiskTier: {Tier}",
            rateEntry.BaseRate,
            riskAssessment.RiskTier);

        // Step 1: Calculate base premium
        // For Workers' Comp: (Payroll / 100) * Rate
        // For GL/BOP: Based on revenue and classification
        var basePremium = CalculateBasePremium(request, rateEntry);

        // Step 2: Apply risk-based adjustments
        var adjustments = CalculateAdjustments(request, riskAssessment, basePremium);
        var totalAdjustments = adjustments.Sum(a => a.Amount);

        // Step 3: Calculate subtotal
        var subtotal = basePremium + totalAdjustments;

        // Step 4: Apply minimum premium
        var minPremium = rateEntry.MinPremium;
        if (subtotal < minPremium)
        {
            adjustments.Add(new PremiumAdjustment
            {
                Code = "MIN",
                Description = "Minimum Premium Adjustment",
                Type = AdjustmentType.Surcharge,
                Factor = 0,
                Amount = minPremium - subtotal
            });
            subtotal = minPremium;
        }

        // Step 5: Apply deductible credit
        var deductibleCredit = CalculateDeductibleCredit(request.Deductible, subtotal);
        if (deductibleCredit != 0)
        {
            adjustments.Add(new PremiumAdjustment
            {
                Code = "DED",
                Description = "Deductible Credit",
                Type = AdjustmentType.Credit,
                Factor = deductibleCredit / subtotal,
                Amount = -deductibleCredit
            });
            subtotal -= deductibleCredit;
        }

        // Step 6: Calculate taxes and fees
        var stateTax = Math.Round(subtotal * rateEntry.StateTaxRate, 2);
        var policyFee = CalculatePolicyFee(request.ProductType);

        // Step 7: Calculate final annual premium
        var annualPremium = subtotal + stateTax + policyFee;
        var monthlyPremium = Math.Round(annualPremium / 12, 2);

        return new PremiumBreakdown
        {
            BasePremium = basePremium,
            Adjustments = adjustments,
            TotalAdjustments = adjustments.Sum(a => a.Amount),
            Subtotal = subtotal,
            StateTax = stateTax,
            PolicyFee = policyFee,
            AnnualPremium = annualPremium,
            MonthlyPremium = monthlyPremium,
            MinimumPremium = minPremium
        };
    }

    #region Private Calculation Methods

    private int CalculateYearsInBusinessScore(int yearsInBusiness)
    {
        // Lower score = lower risk
        return yearsInBusiness switch
        {
            >= 10 => 15,
            >= 5 => 25,
            >= 3 => 40,
            >= 2 => 55,
            1 => 70,
            _ => 90
        };
    }

    private int CalculateEmployeeCountScore(int employeeCount, BusinessType businessType)
    {
        // Score based on employee count relative to business type
        // Construction and Manufacturing have higher risk with more employees
        var baseScore = employeeCount switch
        {
            <= 5 => 20,
            <= 25 => 35,
            <= 100 => 50,
            <= 500 => 65,
            _ => 80
        };

        // Adjust for high-risk industries
        if (businessType is BusinessType.Construction or BusinessType.Manufacturing)
        {
            baseScore = Math.Min(100, baseScore + 15);
        }

        return baseScore;
    }

    private int CalculateIndustryScore(BusinessType businessType)
    {
        return businessType switch
        {
            BusinessType.Technology => 20,
            BusinessType.ProfessionalServices => 25,
            BusinessType.Office => 25,
            BusinessType.Retail => 40,
            BusinessType.RealEstate => 45,
            BusinessType.Healthcare => 55,
            BusinessType.Restaurant => 60,
            BusinessType.Transportation => 65,
            BusinessType.Manufacturing => 70,
            BusinessType.Construction => 80,
            _ => 50
        };
    }

    private int CalculateClaimsScore(List<RiskFactor>? riskFactors)
    {
        if (riskFactors == null || riskFactors.Count == 0)
        {
            return 40; // Neutral if no claims data
        }

        var claimsFactor = riskFactors.FirstOrDefault(f => f.FactorType == RiskFactorType.Claims);
        if (claimsFactor == null)
        {
            return 40;
        }

        // Factor value should be 0-100 where higher = more claims
        return (int)Math.Clamp(claimsFactor.FactorValue, 0, 100);
    }

    private int CalculateRevenueSizeScore(decimal annualRevenue)
    {
        return annualRevenue switch
        {
            < 100_000m => 30,
            < 500_000m => 35,
            < 1_000_000m => 40,
            < 5_000_000m => 50,
            < 10_000_000m => 60,
            < 25_000_000m => 70,
            _ => 80
        };
    }

    private RiskTier DetermineRiskTier(int overallScore)
    {
        return overallScore switch
        {
            <= 35 => RiskTier.Preferred,
            <= 55 => RiskTier.Standard,
            <= 75 => RiskTier.NonStandard,
            _ => RiskTier.Decline
        };
    }

    private List<string> GenerateUnderwritingNotes(QuoteRequest request, int score, RiskTier tier)
    {
        var notes = new List<string>();

        if (tier == RiskTier.Preferred)
        {
            notes.Add("Account qualifies for preferred rates based on favorable risk profile");
        }

        if (request.YearsInBusiness >= 10)
        {
            notes.Add("Established business history - positive indicator");
        }
        else if (request.YearsInBusiness < 3)
        {
            notes.Add("New business - limited experience data available");
        }

        if (request.BusinessType is BusinessType.Construction or BusinessType.Manufacturing)
        {
            notes.Add("High-risk industry classification - verify safety programs in place");
        }

        if (request.AnnualPayroll > 5_000_000m)
        {
            notes.Add("Large payroll exposure - review workers' compensation loss history");
        }

        if (tier == RiskTier.NonStandard)
        {
            notes.Add("Non-standard tier - premium surcharge applied");
        }

        return notes;
    }

    private decimal CalculateBasePremium(QuoteRequest request, RateTableEntry rateEntry)
    {
        var premium = request.ProductType switch
        {
            ProductType.WorkersCompensation =>
                // Workers' Comp: (Payroll / 100) * Rate per $100 of payroll
                Math.Round(request.AnnualPayroll / 100m * rateEntry.BaseRate, 2),

            ProductType.GeneralLiability =>
                // GL: Based on revenue or area
                Math.Round(request.AnnualRevenue / 1000m * rateEntry.BaseRate, 2),

            ProductType.BusinessOwnersPolicy =>
                // BOP: Combined property + liability
                Math.Round((request.AnnualRevenue / 1000m * rateEntry.BaseRate) * 1.25m, 2),

            ProductType.CommercialAuto =>
                // Auto: Per vehicle (simplified - would normally use vehicle count)
                Math.Round(request.EmployeeCount * rateEntry.BaseRate * 0.5m, 2),

            ProductType.ProfessionalLiability =>
                // E&O: Revenue-based
                Math.Round(request.AnnualRevenue / 1000m * rateEntry.BaseRate * 0.8m, 2),

            ProductType.CyberLiability =>
                // Cyber: Revenue-based with employee factor
                Math.Round((request.AnnualRevenue / 1000m * rateEntry.BaseRate * 0.3m) + (request.EmployeeCount * 50), 2),

            _ => Math.Round(request.AnnualRevenue / 1000m * rateEntry.BaseRate, 2)
        };

        return premium;
    }

    private List<PremiumAdjustment> CalculateAdjustments(
        QuoteRequest request,
        RiskAssessment riskAssessment,
        decimal basePremium)
    {
        var adjustments = new List<PremiumAdjustment>();

        // Risk tier adjustment
        var tierFactor = riskAssessment.RiskTier switch
        {
            RiskTier.Preferred => -0.15m,  // 15% discount
            RiskTier.Standard => 0m,
            RiskTier.NonStandard => 0.25m, // 25% surcharge
            _ => 0m
        };

        if (tierFactor != 0)
        {
            adjustments.Add(new PremiumAdjustment
            {
                Code = "TIER",
                Description = $"{riskAssessment.RiskTier} Tier Adjustment",
                Type = tierFactor < 0 ? AdjustmentType.Discount : AdjustmentType.Surcharge,
                Factor = tierFactor,
                Amount = Math.Round(basePremium * tierFactor, 2)
            });
        }

        // Experience modification (for Workers' Comp)
        if (request.ProductType == ProductType.WorkersCompensation)
        {
            var expMod = CalculateExperienceMod(request.YearsInBusiness, request.RiskFactors);
            if (expMod != 1.0m)
            {
                adjustments.Add(new PremiumAdjustment
                {
                    Code = "EXPMOD",
                    Description = "Experience Modification",
                    Type = expMod < 1 ? AdjustmentType.Credit : AdjustmentType.Debit,
                    Factor = expMod - 1,
                    Amount = Math.Round(basePremium * (expMod - 1), 2)
                });
            }
        }

        // Multi-year discount (if applicable)
        if (request.YearsInBusiness >= 5)
        {
            adjustments.Add(new PremiumAdjustment
            {
                Code = "LOYAL",
                Description = "Established Business Discount",
                Type = AdjustmentType.Discount,
                Factor = -0.05m,
                Amount = Math.Round(basePremium * -0.05m, 2)
            });
        }

        // Safety program credit (check risk factors)
        var safetyFactor = request.RiskFactors?.FirstOrDefault(f => f.FactorType == RiskFactorType.Safety);
        if (safetyFactor?.FactorValue >= 80)
        {
            adjustments.Add(new PremiumAdjustment
            {
                Code = "SAFETY",
                Description = "Safety Program Credit",
                Type = AdjustmentType.Credit,
                Factor = -0.10m,
                Amount = Math.Round(basePremium * -0.10m, 2)
            });
        }

        return adjustments;
    }

    private decimal CalculateExperienceMod(int yearsInBusiness, List<RiskFactor>? riskFactors)
    {
        // Simplified experience mod calculation
        // In production, this would use actual loss history
        if (yearsInBusiness < 3)
        {
            return 1.0m; // No experience mod for new businesses
        }

        var claimsFactor = riskFactors?.FirstOrDefault(f => f.FactorType == RiskFactorType.Claims);
        if (claimsFactor == null)
        {
            return 1.0m;
        }

        // Convert claims score to experience mod
        // 0-30 = 0.75-0.90, 30-50 = 0.90-1.00, 50-70 = 1.00-1.20, 70+ = 1.20+
        return claimsFactor.FactorValue switch
        {
            <= 20 => 0.75m,
            <= 30 => 0.85m,
            <= 40 => 0.95m,
            <= 50 => 1.0m,
            <= 60 => 1.10m,
            <= 70 => 1.20m,
            <= 80 => 1.30m,
            _ => 1.50m
        };
    }

    private decimal CalculateDeductibleCredit(decimal deductible, decimal subtotal)
    {
        // Higher deductible = larger credit
        var creditPercentage = deductible switch
        {
            >= 25000m => 0.15m,
            >= 10000m => 0.10m,
            >= 5000m => 0.07m,
            >= 2500m => 0.05m,
            >= 1000m => 0.02m,
            _ => 0m
        };

        return Math.Round(subtotal * creditPercentage, 2);
    }

    private decimal CalculatePolicyFee(ProductType productType)
    {
        return productType switch
        {
            ProductType.WorkersCompensation => 250m,
            ProductType.GeneralLiability => 150m,
            ProductType.BusinessOwnersPolicy => 200m,
            ProductType.CommercialAuto => 175m,
            ProductType.ProfessionalLiability => 200m,
            ProductType.CyberLiability => 125m,
            _ => 150m
        };
    }

    #endregion
}
