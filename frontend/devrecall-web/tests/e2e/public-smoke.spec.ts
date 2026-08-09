import { expect, test } from '@playwright/test'

test('landing links into the authenticated application', async ({ page }) => {
  await page.goto('/')
  await expect(
    page.getByRole('heading', { name: 'Remember what you learn. Know what to study next.' }),
  ).toBeVisible()
  await expect(page.getByRole('heading', { name: 'One loop for developer learning' })).toBeVisible()
  await page.getByRole('link', { name: 'Sign in' }).first().click()
  await expect(page.getByRole('heading', { name: 'Welcome back' })).toBeVisible()
})
