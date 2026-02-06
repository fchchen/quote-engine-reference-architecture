import { test, expect } from '@playwright/test';
import { loginDemo } from './helpers';

test.describe('Settings (Branding)', () => {
  test.beforeEach(async ({ page }) => {
    await loginDemo(page);
  });

  test('settings is accessible via user menu', async ({ page }) => {
    // Open user menu
    await page.locator('button', { has: page.locator('mat-icon:text("account_circle")') }).click();
    await page.getByRole('menuitem', { name: 'Settings' }).click();

    await page.waitForURL(/\/settings/);
    await expect(page.locator('h1')).toContainText('Settings');
  });

  test('shows branding form with color pickers', async ({ page }) => {
    await page.goto('/settings');

    await expect(page.getByText('Branding', { exact: true })).toBeVisible();
    await expect(page.getByRole('textbox', { name: 'Primary' })).toBeVisible();
    await expect(page.getByRole('textbox', { name: 'Secondary' })).toBeVisible();
    await expect(page.getByRole('textbox', { name: 'Tag Line' })).toBeVisible();
  });

  test('tag line field contains current branding value', async ({ page }) => {
    await page.goto('/settings');

    const tagLine = page.getByRole('textbox', { name: 'Tag Line' });
    await expect(tagLine).toHaveValue(/./); // non-empty
  });

  test('has font family selector', async ({ page }) => {
    await page.goto('/settings');

    await expect(page.getByText('Font Family')).toBeVisible();
    // Font Family is a combobox/select
    await expect(page.locator('mat-select')).toBeVisible();
  });

  test('has Save Branding and Reset Preview buttons', async ({ page }) => {
    await page.goto('/settings');

    await expect(page.getByRole('button', { name: 'Save Branding' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Reset Preview' })).toBeVisible();
  });
});
