import { expect, test } from '@playwright/test'

test('landing links into the authenticated application', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Know what to learn next.' })).toBeVisible()
  await page.getByRole('link', { name: 'Sign in' }).click()
  await expect(page.getByRole('heading', { name: 'Welcome back' })).toBeVisible()
})
