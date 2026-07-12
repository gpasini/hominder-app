import { expect, test } from 'vitest'
import { statusLabel } from './taskStatus'

test('statusLabel maps to French labels', () => {
  expect(statusLabel('Overdue')).toBe('En retard')
  expect(statusLabel('Due')).toBe('À faire')
  expect(statusLabel('Upcoming')).toBe('À venir')
  expect(statusLabel('Done')).toBe('Fait')
})
