import { expect, test } from '@playwright/test'

const apiBaseUrl = process.env.PLAYWRIGHT_API_BASE_URL

test.describe('staging deployment smoke checks', () => {
  test.skip(!apiBaseUrl, 'PLAYWRIGHT_API_BASE_URL must point at the deployed staging API')

  test('staging API is ready and serves the Rust curriculum', async ({ request }) => {
    const [health, ready, catalog, rust] = await Promise.all([
      request.get(`${apiBaseUrl}/health`),
      request.get(`${apiBaseUrl}/ready`),
      request.get(`${apiBaseUrl}/api/courses`),
      request.get(`${apiBaseUrl}/api/courses/rust-systems`),
    ])

    await expect(health).toBeOK()
    await expect(health.json()).resolves.toMatchObject({ status: 'ok' })
    await expect(ready).toBeOK()
    await expect(catalog.json()).resolves.toEqual(expect.arrayContaining([
      expect.objectContaining({ id: 'rust-systems', available: true }),
    ]))
    const course = await rust.json() as { modules: { lessons: { order: number }[] }[] }
    const orders = course.modules.flatMap(module => module.lessons).map(lesson => lesson.order).sort((a, b) => a - b)
    expect(orders.length).toBeGreaterThan(0)
    expect(orders).toEqual(orders.map((_, index) => index + 1))
  })
})
