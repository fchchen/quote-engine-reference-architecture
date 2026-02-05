import { test, expect } from '@playwright/test';

test.describe('Authentication', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to a page first so we can clear localStorage
    await page.goto('/about');
    await page.evaluate(() => localStorage.clear());
  });

  test('redirects to login when unauthenticated', async ({ page }) => {
    await page.goto('/quote');

    // Should redirect to login
    await expect(page).toHaveURL(/\/login/);
    await expect(page.locator('mat-card-title')).toContainText('Quote Engine');
  });

  test('demo button authenticates and redirects to /quote', async ({ page }) => {
    await page.goto('/login');

    // Click Try Demo button
    await page.getByRole('button', { name: /Try Demo/ }).click();

    // Should redirect to quote page
    await page.waitForURL(/\/quote/);
    await expect(page.locator('h1')).toContainText('Get a Quote');
  });

  test('username/password login works', async ({ page }) => {
    await page.goto('/login');

    // Fill in credentials
    await page.fill('input[name="username"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');

    // Click Sign In
    await page.getByRole('button', { name: 'Sign In' }).click();

    // Should redirect to quote page
    await page.waitForURL(/\/quote/);
    await expect(page.locator('h1')).toContainText('Get a Quote');
  });

  test('invalid credentials show error', async ({ page }) => {
    await page.goto('/login');

    // Fill in wrong credentials
    await page.fill('input[name="username"]', 'admin');
    await page.fill('input[name="password"]', 'wrong');

    // Click Sign In
    await page.getByRole('button', { name: 'Sign In' }).click();

    // Should show error
    await expect(page.locator('.error-text')).toBeVisible();
    await expect(page).toHaveURL(/\/login/);
  });

  test('logout redirects to login', async ({ page }) => {
    // First login
    await page.goto('/login');
    await page.getByRole('button', { name: /Try Demo/ }).click();
    await page.waitForURL(/\/quote/);

    // Open sidenav and click logout
    await page.locator('button', { hasText: 'menu' }).first().click();
    await page.getByRole('button', { name: 'Logout' }).click();

    // Should redirect to login
    await expect(page).toHaveURL(/\/login/);
  });

  test('/about accessible without auth', async ({ page }) => {
    // Navigate to about directly (localStorage already cleared in beforeEach)
    await page.goto('/about');

    // Should be on about page (not redirected to login)
    await expect(page.locator('h1')).toContainText('About Quote Engine');
  });
});
