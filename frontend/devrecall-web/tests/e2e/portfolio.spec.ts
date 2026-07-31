import path from 'node:path'
import { expect, test } from '@playwright/test'

test('capture the seeded Today workspace', async ({ page }) => {
  const email = process.env.DEVRECALL_DEMO_EMAIL
  const password = process.env.DEVRECALL_DEMO_PASSWORD
  test.skip(!email || !password, 'Demo credentials are required for portfolio capture.')

  await page.goto('/login')
  await page.getByRole('textbox', { name: 'Email*' }).fill(email!)
  await page.getByRole('textbox', { name: 'Password*' }).fill(password!)
  await page.getByRole('button', { name: 'Sign in' }).click()
  await expect(page.getByRole('heading', { name: 'Continue study session' })).toBeVisible()
  await page.screenshot({
    path: path.resolve('../../docs/images/devrecall-today-dashboard.png'),
    fullPage: true,
  })
})
