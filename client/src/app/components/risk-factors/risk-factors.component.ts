import { Component, input, output, computed, signal, effect, untracked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatSliderModule } from '@angular/material/slider';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RiskFactor, RiskFactorType } from '../../models/quote.model';

/**
 * Risk factors input component.
 *
 * INTERVIEW TALKING POINTS:
 * - Collects additional risk information
 * - Uses signals for local state
 * - Emits changes via output()
 */
@Component({
  selector: 'app-risk-factors',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatSliderModule,
    MatSelectModule,
    MatFormFieldModule,
    MatCheckboxModule,
    MatTooltipModule
  ],
  templateUrl: './risk-factors.component.html',
  styleUrl: './risk-factors.component.scss'
})
export class RiskFactorsComponent {
  // Input for initial values
  initialFactors = input<RiskFactor[]>([]);

  // Input to trigger reset from parent
  resetTrigger = input(0);

  // Output for changes
  factorsChange = output<RiskFactor[]>();

  // Local state signals
  claimsScore = signal(40);
  hasSafetyProgram = signal(false);
  hasWrittenPolicies = signal(false);
  hasSafetyCommittee = signal(false);
  experienceMod = signal(1.0);

  constructor() {
    // Effect to load initial factors or reset when business changes.
    // Only tracks resetTrigger. All other signal reads (initialFactors,
    // and transitively via reset/emitFactors) are untracked so that
    // user-driven changes to checkboxes/slider don't re-trigger this effect.
    effect(() => {
      const trigger = this.resetTrigger();
      if (trigger > 0) {
        untracked(() => {
          const factors = this.initialFactors();
          if (factors && factors.length > 0) {
            this.loadFromFactors(factors);
          } else {
            this.reset();
          }
        });
      }
    }, { allowSignalWrites: true });
  }

  private loadFromFactors(factors: RiskFactor[]): void {
    const claims = factors.find(f => f.factorCode === 'CLAIMS');
    const safety = factors.find(f => f.factorCode === 'SAFETY');
    const expMod = factors.find(f => f.factorCode === 'EXPMOD');

    if (claims) {
      this.claimsScore.set(claims.factorValue);
    }
    if (safety) {
      // Reverse-calculate checkboxes from safety score
      // Base is 40, each checkbox adds 20
      const safetyValue = safety.factorValue;
      this.hasSafetyProgram.set(safetyValue >= 60);
      this.hasWrittenPolicies.set(safetyValue >= 80);
      this.hasSafetyCommittee.set(safetyValue >= 100);
    }
    if (expMod) {
      this.experienceMod.set(expMod.factorValue / 100);
    }
  }

  reset(): void {
    this.claimsScore.set(40);
    this.hasSafetyProgram.set(false);
    this.hasWrittenPolicies.set(false);
    this.hasSafetyCommittee.set(false);
    this.experienceMod.set(1.0);
    // Emit the reset values to parent
    this.emitFactors();
  }

  // Computed safety score
  safetyScore = computed(() => {
    let score = 40; // Base score
    if (this.hasSafetyProgram()) score += 20;
    if (this.hasWrittenPolicies()) score += 20;
    if (this.hasSafetyCommittee()) score += 20;
    return score;
  });

  updateClaimsScore(value: number | null): void {
    if (value !== null) {
      this.claimsScore.set(value);
      this.emitFactors();
    }
  }

  updateSafetyProgram(checked: boolean): void {
    this.hasSafetyProgram.set(checked);
    this.emitFactors();
  }

  updateWrittenPolicies(checked: boolean): void {
    this.hasWrittenPolicies.set(checked);
    this.emitFactors();
  }

  updateSafetyCommittee(checked: boolean): void {
    this.hasSafetyCommittee.set(checked);
    this.emitFactors();
  }

  updateExperienceMod(value: number): void {
    this.experienceMod.set(value);
    this.emitFactors();
  }

  getClaimsLabel(): string {
    const score = this.claimsScore();
    if (score <= 20) return 'Excellent - No/minimal claims';
    if (score <= 40) return 'Good - Few minor claims';
    if (score <= 60) return 'Average - Typical claims history';
    if (score <= 80) return 'Below Average - Multiple claims';
    return 'Poor - Significant claims history';
  }

  private emitFactors(): void {
    const factors: RiskFactor[] = [
      {
        factorCode: 'CLAIMS',
        factorName: 'Claims History',
        factorValue: this.claimsScore(),
        factorType: RiskFactorType.Claims
      },
      {
        factorCode: 'SAFETY',
        factorName: 'Safety Programs',
        factorValue: this.safetyScore(),
        factorType: RiskFactorType.Safety
      },
      {
        factorCode: 'EXPMOD',
        factorName: 'Experience Modification',
        factorValue: this.experienceMod() * 100,
        factorType: RiskFactorType.Experience
      }
    ];

    this.factorsChange.emit(factors);
  }
}
