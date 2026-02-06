import { test, expect } from '@playwright/test';

test.describe('Authentication', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
  });

  test('redirects to /login when unauthenticated', async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/\/login/);
  });

  test('login page shows TeamForge title', async ({ page }) => {
    await page.goto('/login');
    await expect(page.locator('mat-card-title')).toContainText('TeamForge');
    await expect(page.locator('mat-card-subtitle')).toContainText('Sign in to your workspace');
  });

  test('Acme Corp demo login navigates to /dashboard', async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.getByRole('button', { name: 'Acme Corp' }).click();
    await page.waitForURL(/\/dashboard/);
    await expect(page.locator('h1')).toContainText('Acme Corp');
  });

  test('logout redirects to /login', async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.getByRole('button', { name: 'Acme Corp' }).click();
    await page.waitForURL(/\/dashboard/);

    // Open user menu and click Logout
    await page.locator('button', { has: page.locator('mat-icon:text("account_circle")') }).click();
    await page.getByRole('menuitem', { name: 'Logout' }).click();

    await expect(page).toHaveURL(/\/login/);
  });

  test('Create a new workspace link goes to /register', async ({ page }) => {
    await page.goto('/login');
    await page.getByRole('link', { name: /Create a new workspace/ }).click();
    await expect(page).toHaveURL(/\/register/);
    await expect(page.locator('mat-card-title')).toContainText('Create Your Workspace');
  });
});
