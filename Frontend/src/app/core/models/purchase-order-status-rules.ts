import { PurchaseOrderStatus } from './purchase-order';

/**
 * Which status changes make sense. Mirrors PurchaseOrderStatusRules.cs in the backend, which
 * remains the source of truth (it rejects invalid changes with the same messages).
 *
 *   Pending ──► Confirmed ──► Fulfilled
 *      │            │
 *      └────────────┴──► Cancelled        (Fulfilled and Cancelled are final)
 */
export const NEXT_STATUSES: Readonly<Record<PurchaseOrderStatus, readonly PurchaseOrderStatus[]>> = {
  Pending: ['Confirmed', 'Cancelled'],
  Confirmed: ['Fulfilled', 'Cancelled'],
  Fulfilled: [],
  Cancelled: [],
};

export function canChangeStatus(from: PurchaseOrderStatus, to: PurchaseOrderStatus): boolean {
  return from === to || NEXT_STATUSES[from].includes(to);
}

/** Returns null when the change is allowed, otherwise a message explaining why it isn't. */
export function statusChangeProblem(from: PurchaseOrderStatus, to: PurchaseOrderStatus): string | null {
  if (canChangeStatus(from, to)) {
    return null;
  }

  if (from === 'Pending' && to === 'Fulfilled') {
    return "A pending order can't be fulfilled yet. Confirm it first, then mark it as Fulfilled.";
  }
  if (from === 'Confirmed' && to === 'Pending') {
    return "A confirmed order can't go back to Pending. Cancel it instead if it shouldn't go ahead.";
  }
  if (from === 'Fulfilled' && to === 'Cancelled') {
    return "A fulfilled order can't be cancelled because it has already been delivered.";
  }
  if (from === 'Fulfilled') {
    return `This order has already been fulfilled, so it can't move back to ${to}.`;
  }
  if (from === 'Cancelled') {
    return "A cancelled order can't be reopened. Create a new purchase order instead.";
  }
  return `An order can't change from ${from} to ${to}.`;
}

/** Short guidance shown under the status field. */
export function statusHint(current: PurchaseOrderStatus): string {
  const next = NEXT_STATUSES[current];
  if (next.length === 0) {
    return `${current} is a final status, so it can't be changed.`;
  }
  return `From ${current} this order can move to ${next.join(' or ')}.`;
}
