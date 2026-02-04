import { test, expect, Page } from '@playwright/test';

/**
 * Helper to search for and select a business from the autocomplete.
 */
async function selectBusiness(page: Page, searchTerm: string, businessName: string) {
  const searchInput = page.locator('input[placeholder*="business name"]').first();
  await searchInput.fill(searchTerm);

  // Wait for autocomplete results to appear
  const option = page.locator('mat-option', { hasText: businessName });
  await option.waitFor({ state: 'visible', timeout: 5000 });
  await option.click();

  // Wait for form to populate
  await page.waitForTimeout(300);
}

/**
 * Navigate to a stepper step by clicking its header.
 * Works in linear mode because all forms have valid defaults.
 */
async function goToStep(page: Page, stepLabel: string) {
  // Step headers have aria-disabled in linear mode, but Angular handles
  // navigation internally. Use force to bypass Playwright's disabled check.
  await page.locator('.mat-step-header', { hasText: stepLabel }).click({ force: true });
  await page.waitForTimeout(500);
}

test.describe('Quote Form', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should load the quote form page', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Get a Quote');
    await expect(page.locator('mat-stepper')).toBeVisible();
  });

  test('should search and select Pacific Coast Brewing', async ({ page }) => {
    await selectBusiness(page, 'Pacific', 'Pacific Coast Brewing');

    // Verify business name is populated
    const businessNameInput = page.locator('input[formControlName="businessName"]');
    await expect(businessNameInput).toHaveValue('Pacific Coast Brewing Co');

    // Verify state is populated
    const stateSelect = page.locator('mat-select[formControlName="stateCode"]');
    await expect(stateSelect).toContainText('Washington');
  });

  test('should navigate through all tabs', async ({ page }) => {
    await selectBusiness(page, 'Pacific', 'Pacific Coast Brewing');

    // Navigate to Coverage
    await goToStep(page, 'Coverage');
    await expect(page.getByRole('heading', { name: 'Coverage Options' })).toBeVisible();

    // Navigate to Risk Factors
    await goToStep(page, 'Risk Factors');
    await expect(page.getByRole('heading', { name: 'Additional Risk Information' })).toBeVisible();

    // Navigate to Review
    await goToStep(page, 'Review');
    await expect(page.getByRole('heading', { name: 'Review & Submit' })).toBeVisible();
  });
});

test.describe('Business Data Isolation', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('Coverage tab should show different values for different businesses', async ({ page }) => {
    test.setTimeout(60000);

    // Step 1: Select Pacific Coast Brewing
    await selectBusiness(page, 'Pacific', 'Pacific Coast Brewing');

    // Step 2: Go to Coverage tab
    await goToStep(page, 'Coverage');
    await expect(page.getByRole('heading', { name: 'Coverage Options' })).toBeVisible();

    // Step 3: Change to Workers' Compensation
    await page.locator('mat-select[formControlName="productType"]').click();
    await page.locator('mat-option', { hasText: /Workers/ }).click();
    await page.waitForTimeout(500);

    // Step 4: Get current Annual Payroll value for Pacific
    const annualPayrollField = page.locator('app-dynamic-field').filter({ hasText: 'Annual Payroll' }).locator('input');
    const pacificPayroll = await annualPayrollField.inputValue();
    console.log(`Pacific Annual Payroll: ${pacificPayroll}`);

    // Step 5: Modify the Annual Payroll for Pacific
    await annualPayrollField.fill('999999');
    await page.waitForTimeout(300);

    // Verify the value was set
    const modifiedPacificPayroll = await annualPayrollField.inputValue();
    expect(modifiedPacificPayroll).toBe('999999');

    // Step 6: Go back to Business Info
    await goToStep(page, 'Business Info');
    await expect(page.getByRole('heading', { name: 'Business Information' })).toBeVisible();

    // Step 7: Select Lone Star Auto Repair
    await selectBusiness(page, 'Lone', 'Lone Star Auto Repair');

    // Step 8: Go to Coverage tab
    await goToStep(page, 'Coverage');
    await expect(page.getByRole('heading', { name: 'Coverage Options' })).toBeVisible();

    // Step 8b: Switch Lone Star to Workers' Comp too (coverage form resets per business)
    await page.locator('mat-select[formControlName="productType"]').click();
    await page.locator('mat-option', { hasText: /Workers/ }).click();
    await page.waitForTimeout(500);

    // Step 9: Get Annual Payroll value for Lone Star
    const loneStarPayrollField = page.locator('app-dynamic-field').filter({ hasText: 'Annual Payroll' }).locator('input');
    const loneStarPayroll = await loneStarPayrollField.inputValue();
    console.log(`Lone Star Annual Payroll: ${loneStarPayroll}`);

    // Step 10: Verify Lone Star does NOT have Pacific's modified value
    expect(loneStarPayroll).not.toBe('999999');
    console.log(`SUCCESS: Lone Star (${loneStarPayroll}) != Pacific modified (999999)`);
  });

  test('Coverage Limit should not leak between businesses', async ({ page }) => {
    test.setTimeout(60000);

    // Select Pacific and change coverage limit to $2,000,000
    await selectBusiness(page, 'Pacific', 'Pacific Coast Brewing');
    await goToStep(page, 'Coverage');

    await page.locator('mat-select[formControlName="coverageLimit"]').click();
    await page.locator('mat-option', { hasText: '$2,000,000' }).click();
    await page.waitForTimeout(300);

    // Verify Pacific has $2,000,000
    await expect(page.locator('mat-select[formControlName="coverageLimit"]')).toContainText('$2,000,000');

    // Switch to Lone Star
    await goToStep(page, 'Business Info');
    await selectBusiness(page, 'Lone', 'Lone Star Auto Repair');
    await goToStep(page, 'Coverage');

    // Lone Star should have default coverage limit ($1,000,000), NOT Pacific's $2,000,000
    await expect(page.locator('mat-select[formControlName="coverageLimit"]')).toContainText('$1,000,000');
  });

  test('Deductible should not leak between businesses', async ({ page }) => {
    test.setTimeout(60000);

    // Select Pacific and change deductible to $10,000
    await selectBusiness(page, 'Pacific', 'Pacific Coast Brewing');
    await goToStep(page, 'Coverage');

    await page.locator('mat-select[formControlName="deductible"]').click();
    await page.locator('mat-option', { hasText: '$10,000' }).click();
    await page.waitForTimeout(300);

    // Verify Pacific has $10,000
    await expect(page.locator('mat-select[formControlName="deductible"]')).toContainText('$10,000');

    // Switch to Lone Star
    await goToStep(page, 'Business Info');
    await selectBusiness(page, 'Lone', 'Lone Star Auto Repair');
    await goToStep(page, 'Coverage');

    // Lone Star should have default deductible ($1,000), NOT Pacific's $10,000
    await expect(page.locator('mat-select[formControlName="deductible"]')).toContainText('$1,000');
  });

  test('Coverage changes should persist when switching back to original business', async ({ page }) => {
    test.setTimeout(60000);

    // Select Pacific and change coverage limit and deductible
    await selectBusiness(page, 'Pacific', 'Pacific Coast Brewing');
    await goToStep(page, 'Coverage');

    await page.locator('mat-select[formControlName="coverageLimit"]').click();
    await page.locator('mat-option', { hasText: '$2,000,000' }).click();
    await page.locator('mat-select[formControlName="deductible"]').click();
    await page.locator('mat-option', { hasText: '$5,000' }).click();
    await page.waitForTimeout(300);

    // Switch to Lone Star (should get defaults)
    await goToStep(page, 'Business Info');
    await selectBusiness(page, 'Lone', 'Lone Star Auto Repair');
    await goToStep(page, 'Coverage');
    await expect(page.locator('mat-select[formControlName="coverageLimit"]')).toContainText('$1,000,000');

    // Switch back to Pacific — should restore Pacific's saved values
    await goToStep(page, 'Business Info');
    await selectBusiness(page, 'Pacific', 'Pacific Coast Brewing');
    await goToStep(page, 'Coverage');

    await expect(page.locator('mat-select[formControlName="coverageLimit"]')).toContainText('$2,000,000');
    await expect(page.locator('mat-select[formControlName="deductible"]')).toContainText('$5,000');
  });

  test('Risk Factors should reset when switching businesses', async ({ page }) => {
    test.setTimeout(60000);

    // Step 1: Select Pacific Coast Brewing
    await selectBusiness(page, 'Pacific', 'Pacific Coast Brewing');

    // Step 2: Navigate to Risk Factors (must go through Coverage first in linear stepper)
    await goToStep(page, 'Coverage');
    await goToStep(page, 'Risk Factors');
    await expect(page.getByRole('heading', { name: 'Additional Risk Information' })).toBeVisible();

    // Step 3: Check Safety Training Program checkbox (click label to avoid tooltip overlay)
    const safetyCheckbox = page.locator('mat-checkbox', { hasText: 'Safety Training Program' });
    await safetyCheckbox.locator('label').click();
    await page.waitForTimeout(300);

    // Verify it's checked
    await expect(safetyCheckbox.locator('input')).toBeChecked();

    // Step 4: Go back to Business Info
    await goToStep(page, 'Business Info');

    // Step 5: Select Lone Star
    await selectBusiness(page, 'Lone', 'Lone Star Auto Repair');

    // Step 6: Navigate to Risk Factors (step by step)
    await goToStep(page, 'Coverage');
    await goToStep(page, 'Risk Factors');

    // Step 7: Verify Safety Training Program is NOT checked for Lone Star
    const loneStarSafetyCheckbox = page.locator('mat-checkbox', { hasText: 'Safety Training Program' });
    await expect(loneStarSafetyCheckbox.locator('input')).not.toBeChecked();
  });
});

test.describe('Coverage Tab Field Persistence', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('Coverage field values should persist when navigating between tabs', async ({ page }) => {
    await selectBusiness(page, 'Pacific', 'Pacific Coast Brewing');

    // Go to Coverage
    await goToStep(page, 'Coverage');

    // Change Coverage Limit to $2,000,000
    await page.locator('mat-select[formControlName="coverageLimit"]').click();
    await page.locator('mat-option', { hasText: '$2,000,000' }).click();

    // Change Deductible to $5,000
    await page.locator('mat-select[formControlName="deductible"]').click();
    await page.locator('mat-option', { hasText: '$5,000' }).click();

    // Go to Risk Factors then back to Coverage
    await goToStep(page, 'Risk Factors');
    await goToStep(page, 'Coverage');

    // Verify values persisted
    await expect(page.locator('mat-select[formControlName="coverageLimit"]')).toContainText('$2,000,000');
    await expect(page.locator('mat-select[formControlName="deductible"]')).toContainText('$5,000');
  });

  test('Dynamic field values should persist when navigating between tabs', async ({ page }) => {
    test.setTimeout(60000);

    await selectBusiness(page, 'Pacific', 'Pacific Coast Brewing');

    // Go to Coverage
    await goToStep(page, 'Coverage');

    // Change to Workers' Compensation to get Annual Payroll field
    await page.locator('mat-select[formControlName="productType"]').click();
    await page.locator('mat-option', { hasText: /Workers/ }).click();
    await page.waitForTimeout(500);

    // Modify Annual Payroll
    const annualPayrollField = page.locator('app-dynamic-field').filter({ hasText: 'Annual Payroll' }).locator('input');
    await annualPayrollField.fill('123456');

    // Go to Risk Factors then back to Coverage
    await goToStep(page, 'Risk Factors');
    await goToStep(page, 'Coverage');
    await page.waitForTimeout(300);

    // Verify value persisted
    const persistedValue = await page.locator('app-dynamic-field').filter({ hasText: 'Annual Payroll' }).locator('input').inputValue();
    expect(persistedValue).toBe('123456');
  });
});

test.describe('Quote Submission', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should submit a quote and see the result', async ({ page }) => {
    test.setTimeout(60000);

    await selectBusiness(page, 'Pacific', 'Pacific Coast Brewing');

    // Go to Review step (must navigate through each step in linear stepper)
    await goToStep(page, 'Coverage');
    await goToStep(page, 'Risk Factors');
    await goToStep(page, 'Review');

    // Submit quote
    await page.locator('button:has-text("Get Final Quote")').click();

    // Wait for quote result
    await page.waitForTimeout(3000);

    // Verify quote result is shown
    await expect(page.locator('app-quote-result')).toBeVisible();
  });
});
