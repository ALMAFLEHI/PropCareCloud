import type { ApiEnumValue } from '../types/api'

const propertyStatuses: Record<number, string> = {
  0: 'Active',
  1: 'Inactive',
  2: 'Under maintenance',
}

const unitStatuses: Record<number, string> = {
  0: 'Available',
  1: 'Occupied',
  2: 'Under maintenance',
}

const maintenanceStatuses: Record<number, string> = {
  0: 'Submitted',
  1: 'Under review',
  2: 'Assigned',
  3: 'In progress',
  4: 'Completed',
  5: 'Cancelled',
}

const priorities: Record<number, string> = {
  0: 'Low',
  1: 'Medium',
  2: 'High',
  3: 'Emergency',
}

const categories: Record<number, string> = {
  0: 'Plumbing',
  1: 'Electrical',
  2: 'HVAC',
  3: 'Cleaning',
  4: 'Security',
  5: 'Structural',
  6: 'Other',
}

export function formatDateTime(value?: string | null): string {
  if (!value) {
    return 'Not set'
  }

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return value
  }

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(date)
}

export function formatEnumLabel(value: ApiEnumValue): string {
  if (typeof value === 'number') {
    return String(value)
  }

  return value
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .replace(/[_-]+/g, ' ')
    .trim()
    .replace(/\b\w/g, (letter) => letter.toUpperCase())
}

function getMappedLabel(value: ApiEnumValue, map: Record<number, string>) {
  if (typeof value === 'number') {
    return map[value] ?? String(value)
  }

  const numericValue = Number(value)
  if (!Number.isNaN(numericValue) && value.trim() !== '') {
    return map[numericValue] ?? value
  }

  return formatEnumLabel(value)
}

export function getPriorityLabel(value: ApiEnumValue): string {
  return getMappedLabel(value, priorities)
}

export function getStatusLabel(value: ApiEnumValue): string {
  return getMappedLabel(value, maintenanceStatuses)
}

export function getPropertyStatusLabel(value: ApiEnumValue): string {
  return getMappedLabel(value, propertyStatuses)
}

export function getUnitStatusLabel(value: ApiEnumValue): string {
  return getMappedLabel(value, unitStatuses)
}

export function getCategoryLabel(value: ApiEnumValue): string {
  return getMappedLabel(value, categories)
}
