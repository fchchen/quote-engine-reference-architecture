using QuoteEngine.Functions.Models;

namespace QuoteEngine.Functions.Services;

/// <summary>
/// Mock data service for Azure Functions free tier deployment.
/// Uses JSON-based data instead of SQL Server to avoid costs.
///
/// INTERVIEW TALKING POINTS:
/// - Demonstrates strategy pattern - same interface, different implementation
/// - Allows free tier deployment while maintaining functionality
/// - JSON data embedded for zero-cost storage
/// </summary>
public class MockDataService
{
    private static readonly Dictionary<string, decimal> BaseRates = new()
    {
        // Workers' Comp rates (per $100 of payroll)
        { "WC-CA-8810", 2.50m },
        { "WC-TX-8810", 1.85m },
        { "WC-DEFAULT", 2.00m },

        // General Liability rates (per $1000 of revenue)
        { "GL-CA-41677", 5.50m },
        { "GL-TX-41677", 4.80m },
        { "GL-DEFAULT", 5.00m },

        // BOP rates
        { "BOP-DEFAULT", 9.00m },

        // Commercial Auto rates
        { "AUTO-DEFAULT", 1200m },

        // Professional Liability rates
        { "PL-DEFAULT", 4.25m },

        // Cyber Liability rates
        { "CYBER-DEFAULT", 2.50m }
    };

    private static readonly Dictionary<string, decimal> StateTaxRates = new()
    {
        { "CA", 0.0328m },
        { "TX", 0.018m },
        { "NY", 0.035m },
        { "FL", 0.015m },
        { "DEFAULT", 0.02m }
    };

    private static readonly Dictionary<string, decimal> MinPremiums = new()
    {
        { "WorkersCompensation", 1000m },
        { "GeneralLiability", 500m },
        { "BusinessOwnersPolicy", 750m },
        { "CommercialAuto", 1500m },
        { "ProfessionalLiability", 1000m },
        { "CyberLiability", 500m }
    };

    /// <summary>
    /// Get rate entry for the given criteria.
    /// </summary>
    public RateEntry GetRate(string stateCode, string classificationCode, string productType)
    {
        var productPrefix = GetProductPrefix(productType);

        // Try specific state/class rate
        var key = $"{productPrefix}-{stateCode}-{classificationCode}";
        if (!BaseRates.TryGetValue(key, out var baseRate))
        {
            // Fall back to default
            key = $"{productPrefix}-DEFAULT";
            baseRate = BaseRates.GetValueOrDefault(key, 2.0m);
        }

        return new RateEntry
        {
            StateCode = stateCode,
            ClassificationCode = classificationCode,
            ProductType = productType,
            BaseRate = baseRate,
            MinPremium = MinPremiums.GetValueOrDefault(productType, 500m),
            StateTaxRate = StateTaxRates.GetValueOrDefault(stateCode, StateTaxRates["DEFAULT"])
        };
    }

    /// <summary>
    /// Calculate risk score based on request parameters.
    /// </summary>
    public RiskAssessment CalculateRisk(QuoteRequest request)
    {
        var factorScores = new List<RiskFactorScore>();

        // Years in business factor
        var yearsScore = request.YearsInBusiness switch
        {
            >= 10 => 15,
            >= 5 => 25,
            >= 3 => 40,
            >= 2 => 55,
            1 => 70,
            _ => 90
        };
        factorScores.Add(new RiskFactorScore
        {
            FactorName = "Years in Business",
            Score = yearsScore,
            Weight = 20,
            Impact = yearsScore <= 30 ? "Favorable" : yearsScore >= 60 ? "Unfavorable" : "Neutral"
        });

        // Employee count factor
        var employeeScore = request.EmployeeCount switch
        {
            <= 5 => 20,
            <= 25 => 35,
            <= 100 => 50,
            <= 500 => 65,
            _ => 80
        };
        factorScores.Add(new RiskFactorScore
        {
            FactorName = "Employee Count",
            Score = employeeScore,
            Weight = 15,
            Impact = employeeScore <= 30 ? "Favorable" : employeeScore >= 60 ? "Unfavorable" : "Neutral"
        });

        // Industry factor
        var industryScore = request.BusinessType switch
        {
            "Technology" => 20,
            "ProfessionalServices" => 25,
            "Office" => 25,
            "Retail" => 40,
            "Restaurant" => 60,
            "Manufacturing" => 70,
            "Construction" => 80,
            _ => 50
        };
        factorScores.Add(new RiskFactorScore
        {
            FactorName = "Industry Risk",
            Score = industryScore,
            Weight = 25,
            Impact = industryScore <= 30 ? "Favorable" : industryScore >= 60 ? "Unfavorable" : "Neutral"
        });

        // Calculate overall score
        var weightedSum = factorScores.Sum(f => f.Score * f.Weight);
        var totalWeight = factorScores.Sum(f => f.Weight);
        var overallScore = (int)Math.Round(weightedSum / (double)totalWeight);

        var riskTier = overallScore switch
        {
            <= 35 => "Preferred",
            <= 55 => "Standard",
            <= 75 => "NonStandard",
            _ => "Decline"
        };

        var notes = new List<string>();
        if (riskTier == "Preferred")
            notes.Add("Account qualifies for preferred rates");
        if (request.YearsInBusiness >= 10)
            notes.Add("Established business history - positive indicator");

        return new RiskAssessment
        {
            RiskScore = overallScore,
            RiskTier = riskTier,
            FactorScores = factorScores,
            Notes = notes
        };
    }

    /// <summary>
    /// Calculate premium based on request and rate.
    /// </summary>
    public PremiumBreakdown CalculatePremium(QuoteRequest request, RateEntry rate, RiskAssessment risk)
    {
        // Calculate base premium based on product type
        var basePremium = request.ProductType switch
        {
            "WorkersCompensation" => Math.Round(request.AnnualPayroll / 100m * rate.BaseRate, 2),
            "GeneralLiability" => Math.Round(request.AnnualRevenue / 1000m * rate.BaseRate, 2),
            "BusinessOwnersPolicy" => Math.Round((request.AnnualRevenue / 1000m * rate.BaseRate) * 1.25m, 2),
            "CommercialAuto" => Math.Round(request.EmployeeCount * rate.BaseRate * 0.5m, 2),
            "ProfessionalLiability" => Math.Round(request.AnnualRevenue / 1000m * rate.BaseRate * 0.8m, 2),
            "CyberLiability" => Math.Round((request.AnnualRevenue / 1000m * rate.BaseRate * 0.3m) + (request.EmployeeCount * 50), 2),
            _ => Math.Round(request.AnnualRevenue / 1000m * rate.BaseRate, 2)
        };

        // Apply risk tier adjustment
        var adjustments = new List<PremiumAdjustment>();
        var tierFactor = risk.RiskTier switch
        {
            "Preferred" => -0.15m,
            "Standard" => 0m,
            "NonStandard" => 0.25m,
            _ => 0m
        };

        if (tierFactor != 0)
        {
            adjustments.Add(new PremiumAdjustment
            {
                Code = "TIER",
                Description = $"{risk.RiskTier} Tier Adjustment",
                Type = tierFactor < 0 ? "Discount" : "Surcharge",
                Factor = tierFactor,
                Amount = Math.Round(basePremium * tierFactor, 2)
            });
        }

        // Apply minimum premium
        var subtotal = basePremium + adjustments.Sum(a => a.Amount);
        if (subtotal < rate.MinPremium)
        {
            adjustments.Add(new PremiumAdjustment
            {
                Code = "MIN",
                Description = "Minimum Premium Adjustment",
                Type = "Surcharge",
                Factor = 0,
                Amount = rate.MinPremium - subtotal
            });
            subtotal = rate.MinPremium;
        }

        // Calculate taxes and fees
        var stateTax = Math.Round(subtotal * rate.StateTaxRate, 2);
        var policyFee = GetPolicyFee(request.ProductType);
        var annualPremium = subtotal + stateTax + policyFee;

        return new PremiumBreakdown
        {
            BasePremium = basePremium,
            Adjustments = adjustments,
            TotalAdjustments = adjustments.Sum(a => a.Amount),
            Subtotal = subtotal,
            StateTax = stateTax,
            PolicyFee = policyFee,
            AnnualPremium = annualPremium,
            MonthlyPremium = Math.Round(annualPremium / 12, 2),
            MinimumPremium = rate.MinPremium
        };
    }

    private static string GetProductPrefix(string productType)
    {
        return productType switch
        {
            "WorkersCompensation" => "WC",
            "GeneralLiability" => "GL",
            "BusinessOwnersPolicy" => "BOP",
            "CommercialAuto" => "AUTO",
            "ProfessionalLiability" => "PL",
            "CyberLiability" => "CYBER",
            _ => "GL"
        };
    }

    private static decimal GetPolicyFee(string productType)
    {
        return productType switch
        {
            "WorkersCompensation" => 250m,
            "GeneralLiability" => 150m,
            "BusinessOwnersPolicy" => 200m,
            "CommercialAuto" => 175m,
            "ProfessionalLiability" => 200m,
            "CyberLiability" => 125m,
            _ => 150m
        };
    }
}
