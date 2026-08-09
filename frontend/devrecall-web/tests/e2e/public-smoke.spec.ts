import { expect, test } from '@playwright/test'

test('landing exposes consistent registration and sign-in conversion links', async ({ page }) => {
  await page.goto('/')
  await expect(
    page.getByRole('heading', { name: 'Remember what you learn. Know what to study next.' }),
  ).toBeVisible()
  await expect(page.getByRole('heading', { name: 'One loop for developer learning' })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Start learning' }).first()).toHaveAttribute(
    'href',
    '/register',
  )
  await expect(page.getByRole('link', { name: 'Sign in' }).first()).toHaveAttribute('href', '/login')
})

test('features page uses the same primary registration route', async ({ page }) => {
  await page.goto('/features')
  await expect(
    page.getByRole('heading', { name: 'A connected workspace for developer learning' }),
  ).toBeVisible()
  await expect(page.getByRole('link', { name: 'Start learning' }).first()).toHaveAttribute(
    'href',
    '/register',
  )
})
