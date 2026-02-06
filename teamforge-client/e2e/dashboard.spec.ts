import { test, expect } from '@playwright/test';
import { loginDemo } from './helpers';

test.describe('Dashboard', () => {
  test.beforeEach(async ({ page }) => {
    await loginDemo(page);
  });

  test('shows tenant name as heading', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Acme Corp');
  });

  test('displays stats for projects, teams, and members', async ({ page }) => {
    // Use .stat-label class to target only the stat counters, not nav links
    const stats = page.locator('.stat-label');
    await expect(stats.getByText('Projects')).toBeVisible();
    await expect(stats.getByText('Teams')).toBeVisible();
    await expect(stats.getByText('Members')).toBeVisible();
  });

  test('shows recent projects section', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'Recent Projects' })).toBeVisible();
    // Should list at least one project
    await expect(page.getByText('Solar Initiative')).toBeVisible();
  });

  test('toolbar has navigation links', async ({ page }) => {
    await expect(page.getByRole('link', { name: 'Dashboard' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Projects' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Teams' })).toBeVisible();
  });

  test('user menu shows user name and tenant', async ({ page }) => {
    await page.locator('button', { has: page.locator('mat-icon:text("account_circle")') }).click();
    const menu = page.locator('[role="menu"]');
    await expect(menu.getByText('Alice Johnson')).toBeVisible();
    await expect(menu.getByText('Acme Corp')).toBeVisible();
  });
});
