import { z } from 'zod'

export const loginSchema = z.object({
  email: z.string().trim().min(1, 'Email is required.').email('Enter a valid email address.'),
  password: z.string().min(1, 'Password is required.'),
})

export const registerSchema = z.object({
  displayName: z.string().trim().min(2, 'Display name must contain at least 2 characters.').max(100, 'Display name cannot exceed 100 characters.'),
  email: z.string().trim().min(1, 'Email is required.').email('Enter a valid email address.'),
  password: z.string().min(8, 'Password must contain at least 8 characters.').max(128, 'Password cannot exceed 128 characters.'),
  confirmPassword: z.string().min(1, 'Confirm your password.'),
}).refine(value => value.password === value.confirmPassword, {
  path: ['confirmPassword'],
  message: 'Passwords do not match.',
})

export type LoginFormState = z.infer<typeof loginSchema>
export type RegisterFormState = z.infer<typeof registerSchema>
