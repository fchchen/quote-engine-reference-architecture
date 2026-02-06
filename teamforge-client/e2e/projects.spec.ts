import { test, expect } from '@playwright/test';
import { loginDemo } from './helpers';

test.describe('Projects', () => {
  test.beforeEach(async ({ page }) => {
    await loginDemo(page);
    await page.getByRole('link', { name: 'Projects' }).click();
    await page.waitForURL(/\/projects/);
  });

  test('lists existing projects from seed data', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Projects');
    // Should have at least one project card
    const cards = page.locator('.card-grid mat-card');
    await expect(cards.first()).toBeVisible();
    // Verify a known seed project
    await expect(page.getByText('Solar Initiative')).toBeVisible();
  });

  test('create project shows new card', async ({ page }) => {
    // Open create form
    await page.getByRole('button', { name: /New Project/ }).click();

    // Fill out the form
    await page.getByRole('textbox', { name: 'Project Name' }).fill('E2E Test Project');
    await page.getByRole('textbox', { name: 'Description' }).fill('Created by Playwright');
    await page.getByRole('textbox', { name: 'Category' }).fill('Testing');

    // Submit
    await page.getByRole('button', { name: 'Create' }).click();

    // New card should appear
    await expect(page.getByText('E2E Test Project')).toBeVisible();
  });

  test('create form can be cancelled', async ({ page }) => {
    await page.getByRole('button', { name: /New Project/ }).click();
    await expect(page.getByRole('textbox', { name: 'Project Name' })).toBeVisible();

    await page.getByRole('button', { name: 'Cancel' }).click();

    // Form should be gone
    await expect(page.getByRole('textbox', { name: 'Project Name' })).toBeHidden();
  });

  test('delete project removes card', async ({ page }) => {
    // Create a project to delete
    await page.getByRole('button', { name: /New Project/ }).click();
    await page.getByRole('textbox', { name: 'Project Name' }).fill('To Be Deleted');
    await page.getByRole('button', { name: 'Create' }).click();
    await expect(page.getByText('To Be Deleted')).toBeVisible();

    // Find the card and click its delete button
    const card = page.locator('mat-card', { hasText: 'To Be Deleted' });
    await card.locator('button', { has: page.locator('mat-icon:text("delete")') }).click();

    // Should be gone
    await expect(page.getByText('To Be Deleted')).toBeHidden();
  });

  test('each project card shows category and status', async ({ page }) => {
    // Check a known seed project has its metadata
    const firstCard = page.locator('.card-grid mat-card').first();
    await expect(firstCard.locator('mat-card-subtitle')).toBeVisible();
    // Subtitle format is "Category · Status"
    await expect(firstCard.locator('mat-card-subtitle')).toContainText('·');
  });
});
