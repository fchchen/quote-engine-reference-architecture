import { Page } from '@playwright/test';

/**
 * Log in via the Acme Corp demo button and wait for dashboard.
 */
export async function loginDemo(page: Page): Promise<void> {
  await page.goto('/login');
  await page.getByRole('button', { name: 'Acme Corp' }).click();
  await page.waitForURL(/\/dashboard/);
}
