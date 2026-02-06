import { test, expect } from '@playwright/test';
import { loginDemo } from './helpers';

test.describe('Teams', () => {
  test.beforeEach(async ({ page }) => {
    await loginDemo(page);
    await page.getByRole('link', { name: 'Teams' }).click();
    await page.waitForURL(/\/teams/);
  });

  test('lists existing teams from seed data', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Teams');
    const cards = page.locator('.card-grid mat-card');
    await expect(cards.first()).toBeVisible();
    // Verify known seed teams
    await expect(page.getByText('Engineering')).toBeVisible();
    await expect(page.getByText('Frontend')).toBeVisible();
  });

  test('create new team', async ({ page }) => {
    await page.getByRole('button', { name: /New Team/ }).click();

    await page.getByRole('textbox', { name: 'Team Name' }).fill('E2E Team');
    await page.getByRole('textbox', { name: 'Description' }).fill('Created by Playwright');

    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page.getByText('E2E Team')).toBeVisible();
  });

  test('create form can be cancelled', async ({ page }) => {
    await page.getByRole('button', { name: /New Team/ }).click();
    await expect(page.getByRole('textbox', { name: 'Team Name' })).toBeVisible();

    await page.getByRole('button', { name: 'Cancel' }).click();

    await expect(page.getByRole('textbox', { name: 'Team Name' })).toBeHidden();
  });

  test('each team card shows member count', async ({ page }) => {
    const firstCard = page.locator('.card-grid mat-card').first();
    await expect(firstCard.locator('mat-card-subtitle')).toContainText('member');
  });

  test('team cards display member names as chips', async ({ page }) => {
    // The Engineering team should have Alice Johnson as a member
    const engCard = page.locator('mat-card', { hasText: 'Engineering' });
    await expect(engCard.getByText('Alice Johnson')).toBeVisible();
  });
});
