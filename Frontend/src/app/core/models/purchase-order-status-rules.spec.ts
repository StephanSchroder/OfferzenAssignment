import { PURCHASE_ORDER_STATUSES } from './purchase-order';
import { canChangeStatus, statusChangeProblem, statusHint } from './purchase-order-status-rules';

describe('purchase order status rules', () => {
  it('allows keeping the current status', () => {
    for (const status of PURCHASE_ORDER_STATUSES) {
      expect(statusChangeProblem(status, status)).toBeNull();
    }
  });

  it('allows the normal flow', () => {
    expect(canChangeStatus('Pending', 'Confirmed')).toBe(true);
    expect(canChangeStatus('Confirmed', 'Fulfilled')).toBe(true);
    expect(canChangeStatus('Pending', 'Cancelled')).toBe(true);
    expect(canChangeStatus('Confirmed', 'Cancelled')).toBe(true);
  });

  it('explains changes that do not make sense', () => {
    expect(statusChangeProblem('Pending', 'Fulfilled')).toContain('Confirm it first');
    expect(statusChangeProblem('Confirmed', 'Pending')).toContain("can't go back to Pending");
    expect(statusChangeProblem('Fulfilled', 'Cancelled')).toContain('already been delivered');
    expect(statusChangeProblem('Fulfilled', 'Pending')).toContain('already been fulfilled');
    expect(statusChangeProblem('Cancelled', 'Confirmed')).toContain("can't be reopened");
  });

  it('describes the next steps', () => {
    expect(statusHint('Pending')).toBe('From Pending this order can move to Confirmed or Cancelled.');
    expect(statusHint('Cancelled')).toBe("Cancelled is a final status, so it can't be changed.");
  });
});
